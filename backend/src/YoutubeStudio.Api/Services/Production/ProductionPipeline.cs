using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Services.Production;

/// <summary>
/// Executes a single production job through the full pipeline with hardened reliability:
/// bounded retries, dead-lettering, attempt/stage tracking and idempotent artifact writes
/// (PROJECT-STUDY §23). Extracted from the background worker so it is directly testable.
/// </summary>
public sealed class ProductionPipeline(
    YoutubeStudioDbContext db,
    IModelRouter router,
    IServiceProvider services,
    IOptions<ProductionOptions> options,
    ILogger<ProductionPipeline> logger)
{
    private readonly ProductionOptions _options = options.Value;

    /// <summary>Runs one job to completion or (on repeated failure) to a retry/dead-letter outcome.</summary>
    public async Task RunJobAsync(ProductionJob job, CancellationToken cancellationToken)
    {
        var research = router.Resolve(AiTask.Research, services.GetServices<IResearchProvider>());
        var script = router.Resolve(AiTask.Script, services.GetServices<IScriptProvider>());
        var scenePlan = router.Resolve(AiTask.ScenePlan, services.GetServices<IScenePlanProvider>());
        var voice = router.Resolve(AiTask.Voice, services.GetServices<IVoiceProvider>());
        var visual = router.Resolve(AiTask.Visual, services.GetServices<IVisualProvider>());
        var musicSfx = router.Resolve(AiTask.MusicSfx, services.GetServices<IMusicSfxProvider>());
        var captions = router.Resolve(AiTask.Captions, services.GetServices<ICaptionProvider>());
        var render = router.Resolve(AiTask.Render, services.GetServices<IRenderProvider>());
        var qa = router.Resolve(AiTask.Qa, services.GetServices<IQaProvider>());

        job.Status = ProductionJobStatus.Running;
        await SetStageAsync(job, VideoProjectStatus.Researching, cancellationToken);

        try
        {
            var researchResult = await research.ResearchAsync(new ResearchRequest(job.VideoProject.Prompt), cancellationToken);
            await AddArtifactAsync(job.VideoProject, ProductionArtifactType.Research, "research", researchResult.Summary,
                JsonSerializer.Serialize(new { sources = researchResult.Sources }), cancellationToken);
            await CompleteStageAsync(job, VideoProjectStatus.Researching, cancellationToken);

            var scriptResult = await script.GenerateScriptAsync(new ScriptRequest(job.VideoProject.Prompt, researchResult.Summary), cancellationToken);
            job.VideoProject.Title = scriptResult.Title;
            job.VideoProject.Script = scriptResult.Script;
            await AddArtifactAsync(job.VideoProject, ProductionArtifactType.Script, "script", scriptResult.Script,
                JsonSerializer.Serialize(new { title = scriptResult.Title }), cancellationToken);
            await SetStageAsync(job, VideoProjectStatus.Scripted, cancellationToken);
            await CompleteStageAsync(job, VideoProjectStatus.Scripted, cancellationToken);

            var planResult = await scenePlan.CreateScenePlanAsync(new ScenePlanRequest(scriptResult.Title, scriptResult.Script), cancellationToken);
            await AddArtifactAsync(job.VideoProject, ProductionArtifactType.ScenePlan, "scene-plan",
                JsonSerializer.Serialize(planResult.Scenes), null, cancellationToken);
            await SetStageAsync(job, VideoProjectStatus.Planned, cancellationToken);
            await CompleteStageAsync(job, VideoProjectStatus.Planned, cancellationToken);

            await SetStageAsync(job, VideoProjectStatus.Producing, cancellationToken);

            var voiceResult = await voice.GenerateVoiceAsync(new VoiceRequest(scriptResult.Script, null), cancellationToken);
            await AddArtifactAsync(job.VideoProject, ProductionArtifactType.Voice, voiceResult.ProviderAssetId, null,
                JsonSerializer.Serialize(new { mediaType = voiceResult.MediaType, durationSeconds = voiceResult.Duration.TotalSeconds }), cancellationToken);

            var visualAssetIds = new List<string>();
            await RemoveArtifactsAsync(job.VideoProjectId, ProductionArtifactType.Visual, cancellationToken);
            foreach (var scene in planResult.Scenes)
            {
                var result = await visual.GenerateVisualAsync(new VisualRequest(scene.VisualDirection, scene.DurationSeconds), cancellationToken);
                visualAssetIds.Add(result.ProviderAssetId);
                db.ProductionArtifacts.Add(new ProductionArtifact
                {
                    VideoProjectId = job.VideoProjectId,
                    Type = ProductionArtifactType.Visual,
                    ProviderAssetId = result.ProviderAssetId,
                    Content = scene.VisualDirection,
                    MetadataJson = JsonSerializer.Serialize(new { scene = scene.Number, mediaType = result.MediaType })
                });
            }
            await db.SaveChangesAsync(cancellationToken);

            var musicSfxResult = await musicSfx.GenerateMusicSfxAsync(
                new MusicSfxRequest(scriptResult.Title, scriptResult.Script, planResult.Scenes.Sum(x => x.DurationSeconds)), cancellationToken);
            await AddArtifactAsync(job.VideoProject, ProductionArtifactType.MusicSfx, musicSfxResult.ProviderAssetId,
                null, JsonSerializer.Serialize(new { mediaType = musicSfxResult.MediaType }), cancellationToken);

            var captionResult = await captions.GenerateCaptionsAsync(new CaptionRequest(scriptResult.Script), cancellationToken);
            await AddArtifactAsync(job.VideoProject, ProductionArtifactType.Captions, captionResult.ProviderAssetId, null, null, cancellationToken);
            await CompleteStageAsync(job, VideoProjectStatus.Producing, cancellationToken);
            await SetStageAsync(job, VideoProjectStatus.Rendering, cancellationToken);

            var renderResult = await render.RenderAsync(
                new RenderRequest([voiceResult.ProviderAssetId, .. visualAssetIds, captionResult.ProviderAssetId], musicSfxResult.ProviderAssetId), cancellationToken);
            await AddArtifactAsync(job.VideoProject, ProductionArtifactType.Render, renderResult.ProviderAssetId, null,
                JsonSerializer.Serialize(new { durationSeconds = renderResult.Duration.TotalSeconds }), cancellationToken);
            await CompleteStageAsync(job, VideoProjectStatus.Rendering, cancellationToken);
            await SetStageAsync(job, VideoProjectStatus.Qa, cancellationToken);

            var qaResult = await qa.EvaluateAsync(new QaRequest(scriptResult.Title, scriptResult.Script, renderResult.ProviderAssetId), cancellationToken);
            await AddArtifactAsync(job.VideoProject, ProductionArtifactType.Qa, "qa-result", JsonSerializer.Serialize(qaResult), null, cancellationToken);
            if (!qaResult.Passed) throw new InvalidOperationException($"Production QA failed: {string.Join("; ", qaResult.Findings)}");

            job.Status = ProductionJobStatus.Succeeded;
            job.VideoProject.Status = VideoProjectStatus.Completed;
            job.Error = null;
            await CompleteStageAsync(job, VideoProjectStatus.Qa, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            await HandleFailureAsync(job, exception, cancellationToken);
        }

        await SaveAsync(job, cancellationToken);
    }

    private async Task HandleFailureAsync(ProductionJob job, Exception exception, CancellationToken cancellationToken)
    {
        job.Error = exception.Message;

        if (job.Attempt < _options.MaxAttempts)
        {
            logger.LogWarning(exception,
                "Production job {JobId} failed on attempt {Attempt}/{MaxAttempts}; requeueing for retry.",
                job.Id, job.Attempt, _options.MaxAttempts);
            job.Attempt++;
            job.Status = ProductionJobStatus.Queued;
            job.VideoProject.Status = VideoProjectStatus.Draft;
        }
        else
        {
            logger.LogError(exception,
                "Production job {JobId} exhausted {MaxAttempts} attempts; dead-lettering.",
                job.Id, _options.MaxAttempts);
            job.Status = ProductionJobStatus.DeadLettered;
            job.VideoProject.Status = VideoProjectStatus.Failed;
        }

        await Task.CompletedTask;
    }

    private async Task SetStageAsync(ProductionJob job, VideoProjectStatus status, CancellationToken cancellationToken)
    {
        job.VideoProject.Status = status;
        await SaveAsync(job, cancellationToken);
    }

    private async Task CompleteStageAsync(ProductionJob job, VideoProjectStatus stage, CancellationToken cancellationToken)
    {
        job.LastCompletedStage = stage.ToString();
        await SaveAsync(job, cancellationToken);
    }

    /// <summary>Idempotent single-artifact write: any existing artifact of the same type is removed first.</summary>
    private async Task AddArtifactAsync(VideoProject project, ProductionArtifactType type,
        string providerAssetId, string? content, string? metadataJson, CancellationToken cancellationToken)
    {
        await RemoveArtifactsAsync(project.Id, type, cancellationToken);
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = type,
            ProviderAssetId = providerAssetId,
            Content = content,
            MetadataJson = metadataJson
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task RemoveArtifactsAsync(Guid videoProjectId, ProductionArtifactType type, CancellationToken cancellationToken)
    {
        var existing = await db.ProductionArtifacts
            .Where(x => x.VideoProjectId == videoProjectId && x.Type == type)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            db.ProductionArtifacts.RemoveRange(existing);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SaveAsync(ProductionJob job, CancellationToken cancellationToken)
    {
        job.UpdatedAtUtc = DateTime.UtcNow;
        job.VideoProject.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
