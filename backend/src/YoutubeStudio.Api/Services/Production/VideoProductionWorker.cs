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
        var captions = scope.ServiceProvider.GetRequiredService<ICaptionProvider>();
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

            var voiceResult = await voice.GenerateVoiceAsync(new VoiceRequest(scriptResult.Script, null), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Voice, voiceResult.ProviderAssetId, null,
                JsonSerializer.Serialize(new { durationSeconds = voiceResult.Duration.TotalSeconds }), cancellationToken);

            var visualAssetIds = new List<string>();
            foreach (var scene in planResult.Scenes)
            {
                var result = await visual.GenerateVisualAsync(new VisualRequest(scene.VisualDirection, scene.DurationSeconds), cancellationToken);
                visualAssetIds.Add(result.ProviderAssetId);
                await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Visual, result.ProviderAssetId,
                    scene.VisualDirection, JsonSerializer.Serialize(new { scene = scene.Number, mediaType = result.MediaType }), cancellationToken);
            }

            var captionResult = await captions.GenerateCaptionsAsync(new CaptionRequest(scriptResult.Script), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Captions, captionResult.ProviderAssetId, null, null, cancellationToken);
            await SetStageAsync(db, job, VideoProjectStatus.Rendering, cancellationToken);

            var renderResult = await render.RenderAsync(new RenderRequest([voiceResult.ProviderAssetId, ..visualAssetIds, captionResult.ProviderAssetId], null), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Render, renderResult.ProviderAssetId, null,
                JsonSerializer.Serialize(new { durationSeconds = renderResult.Duration.TotalSeconds }), cancellationToken);
            await SetStageAsync(db, job, VideoProjectStatus.Qa, cancellationToken);

            var qaResult = await qa.EvaluateAsync(new QaRequest(scriptResult.Title, scriptResult.Script, renderResult.ProviderAssetId), cancellationToken);
            await AddArtifactAsync(db, job.VideoProject, ProductionArtifactType.Qa, "qa-result", JsonSerializer.Serialize(qaResult), null, cancellationToken);
            if (!qaResult.Passed) throw new InvalidOperationException($"Production QA failed: {string.Join("; ", qaResult.Findings)}");

            job.Status = ProductionJobStatus.Succeeded;
            job.VideoProject.Status = VideoProjectStatus.Completed;
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

    private static async Task SaveAsync(YoutubeStudioDbContext db, ProductionJob job, CancellationToken cancellationToken)
    {
        job.UpdatedAtUtc = DateTime.UtcNow;
        job.VideoProject.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
