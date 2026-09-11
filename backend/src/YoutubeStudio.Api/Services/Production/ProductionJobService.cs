using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Services.Production;

public interface IProductionJobService
{
    Task<ProductionJob> EnqueueAsync(VideoProject project, CancellationToken cancellationToken);
}

public sealed class ProductionJobService(YoutubeStudioDbContext db) : IProductionJobService
{
    public async Task<ProductionJob> EnqueueAsync(VideoProject project, CancellationToken cancellationToken)
    {
        var existing = await db.ProductionJobs
            .Where(x => x.VideoProjectId == project.Id && x.Status is ProductionJobStatus.Queued or ProductionJobStatus.Running)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
            return existing;

        var job = new ProductionJob
        {
            VideoProjectId = project.Id,
            Status = ProductionJobStatus.Queued
        };

        db.ProductionJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }
}
