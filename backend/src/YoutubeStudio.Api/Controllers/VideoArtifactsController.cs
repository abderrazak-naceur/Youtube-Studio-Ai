using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/video-projects/{videoProjectId:guid}/artifacts")]
public sealed class VideoArtifactsController(YoutubeStudioDbContext db, IWorkspaceAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductionArtifactResponse>>> GetAll(
        Guid videoProjectId,
        CancellationToken cancellationToken)
    {
        var workspaceId = await db.VideoProjects.AsNoTracking()
            .Where(x => x.Id == videoProjectId)
            .Select(x => (Guid?)x.WorkspaceId)
            .SingleOrDefaultAsync(cancellationToken);
        if (workspaceId is null)
            return NotFound();
        if (await access.GetRoleAsync(User, workspaceId.Value, cancellationToken) is null)
            return NotFound();

        var artifacts = await db.ProductionArtifacts
            .AsNoTracking()
            .Where(x => x.VideoProjectId == videoProjectId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new ProductionArtifactResponse(
                x.Id,
                x.Type.ToString(),
                x.Version,
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
    int Version,
    string ProviderAssetId,
    string? Content,
    string? MetadataJson,
    DateTime CreatedAtUtc);
