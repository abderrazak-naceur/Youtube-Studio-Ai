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
            var researchResult = await research.ResearchAsync(new ResearchRequest(job.VideoProject.Prompt), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Research, "research", researchResult.Summary,
                JsonSerializer.Serialize(new { sources = researchResult.Sources }), cancellationToken);

            var scriptResult = await script.GenerateScriptAsync(new ScriptRequest(job.VideoProject.Prompt, researchResult.Summary), cancellationToken);
            job.VideoProject.Title = scriptResult.Title;
            job.VideoProject.Script = scriptResult.Script;
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Script, "script", scriptResult.Script,
                JsonSerializer.Serialize(new { title = scriptResult.Title }), cancellationToken);
            await SetStageAsync(db, job, VideoProjectStatus.Scripted, cancellationToken);

            var planResult = await scenePlan.CreateScenePlanAsync(new ScenePlanRequest(scriptResult.Title, scriptResult.Script), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.ScenePlan, "scene-plan",
                JsonSerializer.Serialize(planResult.Scenes), null, cancellationToken);
            await SetStageAsync(db, job, VideoProjectStatus.Planned, cancellationToken);

            await SetStageAsync(db, job, VideoProjectStatus.Producing, cancellationToken);

            var voiceResult = await voice.GenerateVoiceAsync(new VoiceRequest(scriptResult.Script, null), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Voice, voiceResult.ProviderAssetId, null,
                JsonSerializer.Serialize(new { mediaType = voiceResult.MediaType, durationSeconds = voiceResult.Duration.TotalSeconds }), cancellationToken);
            await AddCostAsync(db, job.VideoProject, "Voice", voiceResult.ProviderAssetId,
                (decimal)voiceResult.Duration.TotalSeconds, CostRates.VoicePerSecondUsd, cancellationToken);

            var visualAssetIds = new List<string>();
            foreach (var scene in planResult.Scenes)
            {
                var result = await visual.GenerateVisualAsync(new VisualRequest(scene.VisualDirection, scene.DurationSeconds), cancellationToken);
                visualAssetIds.Add(result.ProviderAssetId);
                await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Visual, result.ProviderAssetId,
                    scene.VisualDirection, JsonSerializer.Serialize(new { scene = scene.Number, mediaType = result.MediaType }), cancellationToken);
                await AddCostAsync(db, job.VideoProject, "Visual", result.ProviderAssetId,
                    1m, CostRates.VisualPerSceneUsd, cancellationToken);
            }

            var musicSfxResult = await musicSfx.GenerateMusicSfxAsync(
                new MusicSfxRequest(scriptResult.Title, scriptResult.Script, planResult.Scenes.Sum(x => x.DurationSeconds)), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.MusicSfx, musicSfxResult.ProviderAssetId,
                null, JsonSerializer.Serialize(new { mediaType = musicSfxResult.MediaType }), cancellationToken);
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
                JsonSerializer.Serialize(new { candidates = thumbnailResult.Candidates }), cancellationToken);
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
                HasMetadata: !string.IsNullOrWhiteSpace(metadataResult.Title)), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Qa, "qa-result", JsonSerializer.Serialize(qaResult), null, cancellationToken);
            if (!qaResult.Passed) throw new InvalidOperationException($"Production QA failed: {string.Join("; ", qaResult.Findings)}");

            // The automated pipeline stops at the human quality gate. Final export
            // to Completed only happens through an explicit approval decision.
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

    private static async Task AddArtifactAsync(YoutubeStudioDbContext db, VideoProject project, ProductionArtifactType type,
        string providerAssetId, string? content, string? metadataJson, CancellationToken cancellationToken)
    {
        db.ProductionArtifacts.Add(new ProductionArtifact { VideoProjectId = project.Id, Type = type, ProviderAssetId = providerAssetId, Content = content, MetadataJson = metadataJson });
        await db.SaveChangesAsync(cancellationToken);
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
