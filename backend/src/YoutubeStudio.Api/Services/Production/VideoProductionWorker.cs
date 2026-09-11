using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

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
        job.VideoProject.UpdatedAtUtc = DateTime.UtcNow;
        job.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await RunPlaceholderPipelineAsync(job.VideoProject, db, cancellationToken);
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

        job.UpdatedAtUtc = DateTime.UtcNow;
        job.VideoProject.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task RunPlaceholderPipelineAsync(
        VideoProject project,
        YoutubeStudioDbContext db,
        CancellationToken cancellationToken)
    {
        project.Status = VideoProjectStatus.Researching;
        await SaveStageAsync(db, project, cancellationToken);

        project.Status = VideoProjectStatus.Scripted;
        project.Title ??= BuildTitle(project.Prompt);
        project.Script ??= BuildPlaceholderScript(project.Prompt);
        await SaveStageAsync(db, project, cancellationToken);

        project.Status = VideoProjectStatus.Planned;
        await SaveStageAsync(db, project, cancellationToken);

        project.Status = VideoProjectStatus.Producing;
        await SaveStageAsync(db, project, cancellationToken);

        project.Status = VideoProjectStatus.Rendering;
        await SaveStageAsync(db, project, cancellationToken);

        project.Status = VideoProjectStatus.Qa;
        await SaveStageAsync(db, project, cancellationToken);
    }

    private static async Task SaveStageAsync(
        YoutubeStudioDbContext db,
        VideoProject project,
        CancellationToken cancellationToken)
    {
        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await Task.Yield();
    }

    private static string BuildTitle(string prompt)
    {
        var normalized = string.Join(' ', prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= 120 ? normalized : normalized[..120].TrimEnd() + "…";
    }

    private static string BuildPlaceholderScript(string prompt) =>
        $"HOOK: {prompt.Trim()}\n\nBODY: This is a production placeholder. The real Research and Script providers will replace this stage.\n\nCTA: Continue to the next production stage.";
}
