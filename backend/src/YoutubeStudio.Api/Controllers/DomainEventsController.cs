using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Read-only access to the append-only domain event stream for a workspace (DATA-MODEL §4).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/events")]
public sealed class DomainEventsController(YoutubeStudioDbContext db, IWorkspaceAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DomainEventResponse>>> GetForWorkspace(
        Guid workspaceId,
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        if (await access.GetRoleAsync(User, workspaceId, cancellationToken) is null)
            return Forbid();

        var query = db.DomainEvents.AsNoTracking().Where(x => x.WorkspaceId == workspaceId);
        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalized = type.Trim();
            query = query.Where(x => x.Type == normalized);
        }

        var events = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(200)
            .Select(x => new DomainEventResponse(x.Id, x.Type, x.WorkspaceId, x.AggregateId, x.OccurredAtUtc, x.SchemaVersion, x.Payload))
            .ToListAsync(cancellationToken);

        return Ok(events);
    }
}

public sealed record DomainEventResponse(
    Guid Id, string Type, Guid? WorkspaceId, Guid? AggregateId, DateTime OccurredAtUtc, int SchemaVersion, string? Payload);
