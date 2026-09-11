using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Services.Production;

public sealed class VideoProductionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<VideoProductionWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Video production worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unexpected error while processing a production job.");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }

        logger.LogInformation("Video production worker stopped.");
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

        var job = await db.ProductionJobs
            .Include(x => x.VideoProject)
            .Where(x => x.Status == ProductionJobStatus.Queued)
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            await Task.Delay(PollInterval, cancellationToken);
            return;
        }

        job.Status = ProductionJobStatus.Running;
        job.VideoProject.Status = VideoProjectStatus.Researching;
        await SaveAsync(db, job, cancellationToken);

        try
        {
            var researchResult = await research.ResearchAsync(
                new ResearchRequest(job.VideoProject.Prompt), cancellationToken);
            job.VideoProject.Status = VideoProjectStatus.Scripted;
            await SaveAsync(db, job, cancellationToken);

            var scriptResult = await script.GenerateScriptAsync(
                new ScriptRequest(job.VideoProject.Prompt, researchResult.Summary), cancellationToken);
            job.VideoProject.Title = scriptResult.Title;
            job.VideoProject.Script = scriptResult.Script;
            job.VideoProject.Status = VideoProjectStatus.Planned;
            await SaveAsync(db, job, cancellationToken);

            var planResult = await scenePlan.CreateScenePlanAsync(
                new ScenePlanRequest(scriptResult.Title, scriptResult.Script), cancellationToken);
            job.VideoProject.Status = VideoProjectStatus.Producing;
            await SaveAsync(db, job, cancellationToken);

            var voiceResult = await voice.GenerateVoiceAsync(
                new VoiceRequest(scriptResult.Script, null), cancellationToken);

            var visualAssetIds = new List<string>();
            foreach (var scene in planResult.Scenes)
            {
                var visualResult = await visual.GenerateVisualAsync(
                    new VisualRequest(scene.VisualDirection, scene.DurationSeconds), cancellationToken);
                visualAssetIds.Add(visualResult.ProviderAssetId);
            }

            var captionResult = await captions.GenerateCaptionsAsync(
                new CaptionRequest(scriptResult.Script), cancellationToken);

            job.VideoProject.Status = VideoProjectStatus.Rendering;
            await SaveAsync(db, job, cancellationToken);

            var renderResult = await render.RenderAsync(
                new RenderRequest([voiceResult.ProviderAssetId, ..visualAssetIds, captionResult.ProviderAssetId], null),
                cancellationToken);

            job.VideoProject.Status = VideoProjectStatus.Qa;
            await SaveAsync(db, job, cancellationToken);

            var qaResult = await qa.EvaluateAsync(
                new QaRequest(scriptResult.Title, scriptResult.Script, renderResult.ProviderAssetId),
                cancellationToken);

            if (!qaResult.Passed)
                throw new InvalidOperationException($"Production QA failed: {string.Join("; ", qaResult.Findings)}");

            job.Status = ProductionJobStatus.Succeeded;
            job.VideoProject.Status = VideoProjectStatus.Completed;
            job.Error = null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            job.Status = ProductionJobStatus.Failed;
            job.Error = exception.Message;
            job.VideoProject.Status = VideoProjectStatus.Failed;
            logger.LogError(exception, "Production job {JobId} failed for project {ProjectId}.", job.Id, job.VideoProjectId);
        }

        await SaveAsync(db, job, cancellationToken);
    }

    private static async Task SaveAsync(
        YoutubeStudioDbContext db,
        ProductionJob job,
        CancellationToken cancellationToken)
    {
        job.UpdatedAtUtc = DateTime.UtcNow;
        job.VideoProject.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
