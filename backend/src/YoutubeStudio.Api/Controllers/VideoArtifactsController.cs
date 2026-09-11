using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/video-projects/{videoProjectId:guid}/artifacts")]
public sealed class VideoArtifactsController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductionArtifactResponse>>> GetAll(
        Guid videoProjectId,
        CancellationToken cancellationToken)
    {
        var exists = await db.VideoProjects.AnyAsync(x => x.Id == videoProjectId, cancellationToken);
        if (!exists)
            return NotFound();

        var artifacts = await db.ProductionArtifacts
            .AsNoTracking()
            .Where(x => x.VideoProjectId == videoProjectId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new ProductionArtifactResponse(
                x.Id,
                x.Type.ToString(),
                x.ProviderAssetId,
                x.Content,
                x.MetadataJson,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(artifacts);
    }
}

public sealed record ProductionArtifactResponse(
    Guid Id,
    string Type,
    string ProviderAssetId,
    string? Content,
    string? MetadataJson,
    DateTime CreatedAtUtc);
