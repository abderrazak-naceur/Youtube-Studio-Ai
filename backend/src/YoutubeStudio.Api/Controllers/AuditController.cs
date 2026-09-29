using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Read-only access to the immutable audit trail of sensitive actions
/// (SECURITY-COMPLIANCE §8, PRODUCT-REQUIREMENTS audit trail).
/// </summary>
[ApiController]
[Route("api/v1/video-projects/{videoProjectId:guid}/audit")]
public sealed class AuditController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditEventResponse>>> GetForProject(Guid videoProjectId, CancellationToken cancellationToken)
    {
        if (!await db.VideoProjects.AnyAsync(x => x.Id == videoProjectId, cancellationToken))
            return NotFound();

        var events = await db.AuditEvents.AsNoTracking()
            .Where(x => x.VideoProjectId == videoProjectId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AuditEventResponse(x.Id, x.Action, x.Actor, x.WorkspaceId, x.VideoProjectId, x.DetailsJson, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(events);
    }
}

public sealed record AuditEventResponse(
    Guid Id,
    string Action,
    string Actor,
    Guid? WorkspaceId,
    Guid? VideoProjectId,
    string? DetailsJson,
    DateTime CreatedAtUtc);
