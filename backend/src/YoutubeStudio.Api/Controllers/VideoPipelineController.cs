using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/video-projects/{videoProjectId:guid}/pipeline")]
public sealed class VideoPipelineController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<VideoPipelineResponse>> Get(Guid videoProjectId, CancellationToken cancellationToken)
    {
        var project = await db.VideoProjects.AsNoTracking()
            .Where(x => x.Id == videoProjectId)
            .Select(x => new { x.Id, Status = x.Status.ToString(), x.Title, x.UpdatedAtUtc })
            .SingleOrDefaultAsync(cancellationToken);
        if (project is null) return NotFound();

        var artifacts = await db.ProductionArtifacts.AsNoTracking()
            .Where(x => x.VideoProjectId == videoProjectId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new PipelineArtifactResponse(x.Id, x.Type.ToString(), x.ProviderAssetId, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var job = await db.ProductionJobs.AsNoTracking()
            .Where(x => x.VideoProjectId == videoProjectId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new PipelineJobResponse(x.Id, x.Status.ToString(), x.Attempt, x.Error, x.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new VideoPipelineResponse(project.Id, project.Status, project.Title, project.UpdatedAtUtc, job, artifacts));
    }
}

public sealed record VideoPipelineResponse(Guid VideoProjectId, string Status, string? Title, DateTime UpdatedAtUtc,
    PipelineJobResponse? LatestJob, IReadOnlyList<PipelineArtifactResponse> Artifacts);
public sealed record PipelineJobResponse(Guid Id, string Status, int Attempt, string? Error, DateTime UpdatedAtUtc);
public sealed record PipelineArtifactResponse(Guid Id, string Type, string ProviderAssetId, DateTime CreatedAtUtc);
