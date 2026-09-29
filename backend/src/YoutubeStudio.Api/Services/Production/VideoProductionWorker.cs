using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Services.Production;

public sealed class VideoProductionWorker(IServiceScopeFactory scopeFactory, ILogger<VideoProductionWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Video production worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessNextAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unexpected error while processing a production job.");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessNextAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YoutubeStudioDbContext>();
        var research = scope.ServiceProvider.GetRequiredService<IResearchProvider>();
        var script = scope.ServiceProvider.GetRequiredService<IScriptProvider>();
        var scenePlan = scope.ServiceProvider.GetRequiredService<IScenePlanProvider>();
        var voice = scope.ServiceProvider.GetRequiredService<IVoiceProvider>();
        var visual = scope.ServiceProvider.GetRequiredService<IVisualProvider>();
        var musicSfx = scope.ServiceProvider.GetRequiredService<IMusicSfxProvider>();
        var captions = scope.ServiceProvider.GetRequiredService<ICaptionProvider>();
        var thumbnail = scope.ServiceProvider.GetRequiredService<IThumbnailProvider>();
        var metadata = scope.ServiceProvider.GetRequiredService<IMetadataProvider>();
        var render = scope.ServiceProvider.GetRequiredService<IRenderProvider>();
        var qa = scope.ServiceProvider.GetRequiredService<IQaProvider>();

        var job = await db.ProductionJobs.Include(x => x.VideoProject)
            .Where(x => x.Status == ProductionJobStatus.Queued)
            .OrderBy(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (job is null) { await Task.Delay(PollInterval, cancellationToken); return; }

        job.Status = ProductionJobStatus.Running;
        await SetStageAsync(db, job, VideoProjectStatus.Researching, cancellationToken);

        try
        {
            // Resume-safe: if an artifact for a stage already exists (e.g. a previous run
            // failed later), reuse it instead of calling the provider again. This avoids
            // duplicating paid external operations on retry (SYSTEM-ARCHITECTURE §8).
            var researchResult = await ReuseResearchOrGenerateAsync(db, job.VideoProject, research, cancellationToken);
            await MarkStageAsync(db, job, "Research", cancellationToken);

            var scriptResult = await ReuseScriptOrGenerateAsync(db, job.VideoProject, script, researchResult, cancellationToken);
            job.VideoProject.Title = scriptResult.Title;
            job.VideoProject.Script = scriptResult.Script;
            await SetStageAsync(db, job, VideoProjectStatus.Scripted, cancellationToken);
            await MarkStageAsync(db, job, "Script", cancellationToken);

            var planResult = await ReuseScenePlanOrGenerateAsync(db, job.VideoProject, scenePlan, scriptResult, cancellationToken);
            await SetStageAsync(db, job, VideoProjectStatus.Planned, cancellationToken);
            await MarkStageAsync(db, job, "ScenePlan", cancellationToken);

            await SetStageAsync(db, job, VideoProjectStatus.Producing, cancellationToken);

            var voiceResult = await voice.GenerateVoiceAsync(new VoiceRequest(scriptResult.Script, null), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Voice, voiceResult.ProviderAssetId, null,
                JsonSerializer.Serialize(new { mediaType = voiceResult.MediaType, durationSeconds = voiceResult.Duration.TotalSeconds, provenance = Provenance(voiceResult.ProviderAssetId, "tts", null) }), cancellationToken);
            await AddCostAsync(db, job.VideoProject, "Voice", voiceResult.ProviderAssetId,
                (decimal)voiceResult.Duration.TotalSeconds, CostRates.VoicePerSecondUsd, cancellationToken);

            var visualAssetIds = new List<string>();
            foreach (var scene in planResult.Scenes)
            {
                var result = await visual.GenerateVisualAsync(new VisualRequest(scene.VisualDirection, scene.DurationSeconds), cancellationToken);
                visualAssetIds.Add(result.ProviderAssetId);
                await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Visual, result.ProviderAssetId,
                    scene.VisualDirection, JsonSerializer.Serialize(new { scene = scene.Number, mediaType = result.MediaType, provenance = Provenance(result.ProviderAssetId, "image-video", scene.VisualDirection) }), cancellationToken);
                await AddCostAsync(db, job.VideoProject, "Visual", result.ProviderAssetId,
                    1m, CostRates.VisualPerSceneUsd, cancellationToken);
            }

            var musicSfxResult = await musicSfx.GenerateMusicSfxAsync(
                new MusicSfxRequest(scriptResult.Title, scriptResult.Script, planResult.Scenes.Sum(x => x.DurationSeconds)), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.MusicSfx, musicSfxResult.ProviderAssetId,
                null, JsonSerializer.Serialize(new { mediaType = musicSfxResult.MediaType, provenance = Provenance(musicSfxResult.ProviderAssetId, "music-sfx", null) }), cancellationToken);
            await AddCostAsync(db, job.VideoProject, "MusicSfx", musicSfxResult.ProviderAssetId,
                planResult.Scenes.Sum(x => x.DurationSeconds), CostRates.MusicSfxPerSecondUsd, cancellationToken);

            var captionResult = await captions.GenerateCaptionsAsync(new CaptionRequest(scriptResult.Script), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Captions, captionResult.ProviderAssetId, null, null, cancellationToken);
            await AddCostAsync(db, job.VideoProject, "Captions", captionResult.ProviderAssetId,
                1m, CostRates.CaptionsPerVideoUsd, cancellationToken);

            var thumbnailResult = await thumbnail.GenerateThumbnailsAsync(
                new ThumbnailRequest(scriptResult.Title, scriptResult.Script, 3), cancellationToken);
            var primaryThumbnail = thumbnailResult.Candidates.FirstOrDefault();
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Thumbnail,
                primaryThumbnail?.ProviderAssetId ?? "thumbnail",
                null,
                JsonSerializer.Serialize(new { candidates = thumbnailResult.Candidates, provenance = Provenance(primaryThumbnail?.ProviderAssetId ?? "thumbnail", "image", scriptResult.Title) }), cancellationToken);
            await AddCostAsync(db, job.VideoProject, "Thumbnail", primaryThumbnail?.ProviderAssetId ?? "thumbnail",
                Math.Max(1, thumbnailResult.Candidates.Count), CostRates.ThumbnailPerCandidateUsd, cancellationToken);

            var metadataResult = await metadata.GenerateMetadataAsync(
                new MetadataRequest(scriptResult.Title, scriptResult.Script, "en"), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Metadata, "metadata",
                metadataResult.Title,
                JsonSerializer.Serialize(new { title = metadataResult.Title, description = metadataResult.Description, tags = metadataResult.Tags, category = metadataResult.Category, language = metadataResult.Language }),
                cancellationToken);
            await SetStageAsync(db, job, VideoProjectStatus.Rendering, cancellationToken);

            var renderResult = await render.RenderAsync(
                new RenderRequest([voiceResult.ProviderAssetId, ..visualAssetIds, captionResult.ProviderAssetId], musicSfxResult.ProviderAssetId), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Render, renderResult.ProviderAssetId, null,
                JsonSerializer.Serialize(new { durationSeconds = renderResult.Duration.TotalSeconds }), cancellationToken);
            await AddCostAsync(db, job.VideoProject, "Render", renderResult.ProviderAssetId,
                (decimal)renderResult.Duration.TotalSeconds, CostRates.RenderPerSecondUsd, cancellationToken);
            await MarkStageAsync(db, job, "Render", cancellationToken);
            await SetStageAsync(db, job, VideoProjectStatus.Qa, cancellationToken);

            var qaResult = await qa.EvaluateAsync(new QaRequest(
                scriptResult.Title,
                scriptResult.Script,
                renderResult.ProviderAssetId,
                planResult.Scenes.Count,
                voiceResult.Duration.TotalSeconds,
                renderResult.Duration.TotalSeconds,
                HasCaptions: !string.IsNullOrWhiteSpace(captionResult.ProviderAssetId),
                HasThumbnail: primaryThumbnail is not null,
                HasMetadata: !string.IsNullOrWhiteSpace(metadataResult.Title),
                AllAssetsHaveKnownRights: true), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Qa, "qa-result", JsonSerializer.Serialize(qaResult), null, cancellationToken);
            if (!qaResult.Passed)
            {
                db.DomainEvents.Add(new DomainEvent
                {
                    Type = "quality_gate.failed",
                    WorkspaceId = job.VideoProject.WorkspaceId,
                    AggregateId = job.VideoProjectId,
                    Payload = JsonSerializer.Serialize(new { findings = qaResult.Findings })
                });
                await db.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException($"Production QA failed: {string.Join("; ", qaResult.Findings)}");
            }

            // The automated pipeline stops at the human quality gate. Final export
            // to Completed only happens through an explicit approval decision.
            job.LastCompletedStage = "Qa";
            job.Status = ProductionJobStatus.Succeeded;
            job.VideoProject.Status = VideoProjectStatus.AwaitingApproval;
            job.Error = null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            job.Status = ProductionJobStatus.Failed;
            job.Error = exception.Message;
            job.VideoProject.Status = VideoProjectStatus.Failed;
            logger.LogError(exception, "Production job {JobId} failed for project {ProjectId}.", job.Id, job.VideoProjectId);
        }
        await SaveAsync(db, job, cancellationToken);
    }

    private static async Task SetStageAsync(YoutubeStudioDbContext db, ProductionJob job, VideoProjectStatus status, CancellationToken cancellationToken)
    {
        job.VideoProject.Status = status;
        await SaveAsync(db, job, cancellationToken);
    }

    private static async Task MarkStageAsync(YoutubeStudioDbContext db, ProductionJob job, string stage, CancellationToken cancellationToken)
    {
        job.LastCompletedStage = stage;
        await SaveAsync(db, job, cancellationToken);
    }

    private static async Task<ResearchResult> ReuseResearchOrGenerateAsync(
        YoutubeStudioDbContext db, VideoProject project, IResearchProvider research, CancellationToken cancellationToken)
    {
        var existing = await LatestArtifactAsync(db, project.Id, ProductionArtifactType.Research, cancellationToken);
        if (existing is not null && !string.IsNullOrWhiteSpace(existing.Content))
        {
            var sources = TryReadStringArray(existing.MetadataJson, "sources");
            return new ResearchResult(existing.Content, sources);
        }

        var result = await research.ResearchAsync(new ResearchRequest(project.Prompt), cancellationToken);
        await AddArtifactAsync(db, project, ProductionArtifactType.Research, "research", result.Summary,
            JsonSerializer.Serialize(new { sources = result.Sources }), cancellationToken);
        return result;
    }

    private static async Task<ScriptResult> ReuseScriptOrGenerateAsync(
        YoutubeStudioDbContext db, VideoProject project, IScriptProvider script, ResearchResult research, CancellationToken cancellationToken)
    {
        var existing = await LatestArtifactAsync(db, project.Id, ProductionArtifactType.Script, cancellationToken);
        if (existing is not null && !string.IsNullOrWhiteSpace(existing.Content))
        {
            var title = TryReadString(existing.MetadataJson, "title") ?? project.Title ?? project.Prompt;
            return new ScriptResult(title, existing.Content);
        }

        var result = await script.GenerateScriptAsync(new ScriptRequest(project.Prompt, research.Summary), cancellationToken);
        await AddArtifactAsync(db, project, ProductionArtifactType.Script, "script", result.Script,
            JsonSerializer.Serialize(new { title = result.Title }), cancellationToken);
        return result;
    }

    private static async Task<ScenePlanResult> ReuseScenePlanOrGenerateAsync(
        YoutubeStudioDbContext db, VideoProject project, IScenePlanProvider scenePlan, ScriptResult script, CancellationToken cancellationToken)
    {
        var existing = await LatestArtifactAsync(db, project.Id, ProductionArtifactType.ScenePlan, cancellationToken);
        if (existing is not null && !string.IsNullOrWhiteSpace(existing.Content))
        {
            try
            {
                var scenes = JsonSerializer.Deserialize<List<ScenePlanItem>>(existing.Content);
                if (scenes is { Count: > 0 }) return new ScenePlanResult(scenes);
            }
            catch (JsonException) { /* fall through to regenerate */ }
        }

        var result = await scenePlan.CreateScenePlanAsync(new ScenePlanRequest(script.Title, script.Script), cancellationToken);
        await AddArtifactAsync(db, project, ProductionArtifactType.ScenePlan, "scene-plan",
            JsonSerializer.Serialize(result.Scenes), null, cancellationToken);
        return result;
    }

    private static Task<ProductionArtifact?> LatestArtifactAsync(
        YoutubeStudioDbContext db, Guid projectId, ProductionArtifactType type, CancellationToken cancellationToken) =>
        db.ProductionArtifacts.AsNoTracking()
            .Where(x => x.VideoProjectId == projectId && x.Type == type)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    private static string? TryReadString(string? json, string property)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(property, out var value) ? value.GetString() : null;
        }
        catch (JsonException) { return null; }
    }

    private static IReadOnlyList<string> TryReadStringArray(string? json, string property)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array) return [];
            return value.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList();
        }
        catch (JsonException) { return []; }
    }

    private static async Task AddArtifactAsync(YoutubeStudioDbContext db, VideoProject project, ProductionArtifactType type,
        string providerAssetId, string? content, string? metadataJson, CancellationToken cancellationToken)
    {
        var nextVersion = await db.ProductionArtifacts
            .Where(x => x.VideoProjectId == project.Id && x.Type == type)
            .Select(x => (int?)x.Version)
            .MaxAsync(cancellationToken) ?? 0;

        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = type,
            Version = nextVersion + 1,
            ProviderAssetId = providerAssetId,
            Content = content,
            MetadataJson = metadataJson
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Builds provenance/rights metadata for a generated asset as a camelCase object
    /// consistent with the other MetadataJson payloads. Placeholder providers produce
    /// synthetic content, so the rights status is <see cref="RightsStatus.Generated"/>.
    /// Real provider adapters should supply their own licensing metadata.
    /// </summary>
    private static object Provenance(string providerAssetId, string model, string? promptReference) =>
        new
        {
            provider = providerAssetId,
            model,
            rightsStatus = RightsStatus.Generated.ToString(),
            contentHash = Hash(providerAssetId + "|" + (promptReference ?? string.Empty)),
            promptReference,
            generatedAtUtc = DateTime.UtcNow
        };

    private static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static async Task AddCostAsync(YoutubeStudioDbContext db, VideoProject project, string stage,
        string provider, decimal units, decimal unitCostUsd, CancellationToken cancellationToken)
    {
        var normalizedUnits = Math.Round(units, 4);
        var total = Math.Round(normalizedUnits * unitCostUsd, 6);
        db.ProductionCosts.Add(new ProductionCost
        {
            VideoProjectId = project.Id,
            Stage = stage,
            Provider = string.IsNullOrWhiteSpace(provider) ? stage.ToLowerInvariant() : provider,
            Units = normalizedUnits,
            UnitCostUsd = unitCostUsd,
            TotalCostUsd = total
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SaveAsync(YoutubeStudioDbContext db, ProductionJob job, CancellationToken cancellationToken)
    {
        job.UpdatedAtUtc = DateTime.UtcNow;
        job.VideoProject.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Default per-stage unit costs (USD) for the development/placeholder providers.
    /// Real providers should record their own metered costs; these values keep the
    /// cost-per-video calculation populated in the MVP.
    /// </summary>
    private static class CostRates
    {
        public const decimal VoicePerSecondUsd = 0.0004m;
        public const decimal VisualPerSceneUsd = 0.02m;
        public const decimal MusicSfxPerSecondUsd = 0.0002m;
        public const decimal CaptionsPerVideoUsd = 0.01m;
        public const decimal ThumbnailPerCandidateUsd = 0.03m;
        public const decimal RenderPerSecondUsd = 0.0006m;
    }
}
