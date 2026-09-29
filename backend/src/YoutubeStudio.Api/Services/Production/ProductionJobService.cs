using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Services.Production;

public interface IProductionJobService
{
    Task<ProductionJob> EnqueueAsync(VideoProject project, string? idempotencyKey = null, CancellationToken cancellationToken = default);
}

public sealed class ProductionJobService(YoutubeStudioDbContext db) : IProductionJobService
{
    public async Task<ProductionJob> EnqueueAsync(VideoProject project, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();

        // Idempotency: a retried enqueue with the same key returns the existing job.
        if (key is not null)
        {
            var byKey = await db.ProductionJobs
                .Where(x => x.VideoProjectId == project.Id && x.IdempotencyKey == key)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (byKey is not null)
                return byKey;
        }

        // Never run two active jobs for the same project.
        var existing = await db.ProductionJobs
            .Where(x => x.VideoProjectId == project.Id &&
                        (x.Status == ProductionJobStatus.Queued || x.Status == ProductionJobStatus.Running))
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
            return existing;

        var job = new ProductionJob
        {
            VideoProjectId = project.Id,
            Status = ProductionJobStatus.Queued,
            IdempotencyKey = key
        };

        db.ProductionJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }
}
