using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces")]
public sealed class WorkspacesController(YoutubeStudioDbContext db, IWorkspaceAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkspaceResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = access.GetUserId(User);
        if (userId is null) return Unauthorized();

        // Only workspaces the caller is a member of (tenant isolation).
        var workspaces = await db.Memberships.AsNoTracking()
            .Where(m => m.UserId == userId.Value)
            .Select(m => m.Workspace)
            .OrderBy(x => x.Name)
            .Select(x => new WorkspaceResponse(x.Id, x.Name, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(workspaces);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkspaceDetailsResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (await access.GetRoleAsync(User, id, cancellationToken) is null)
            return NotFound();

        var workspace = await db.Workspaces
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new WorkspaceDetailsResponse(
                x.Id,
                x.Name,
                x.Channels.Count,
                x.Opportunities.Count,
                x.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return workspace is null ? NotFound() : Ok(workspace);
    }

    [HttpPost]
    public async Task<ActionResult<WorkspaceResponse>> Create(
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = access.GetUserId(User);
        if (userId is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Name))
            return ValidationProblem("Workspace name is required.");

        var workspace = new Workspace { Name = request.Name.Trim() };
        db.Workspaces.Add(workspace);
        // The creator becomes the workspace Owner.
        db.Memberships.Add(new Membership { UserId = userId.Value, WorkspaceId = workspace.Id, Role = WorkspaceRole.Owner });
        await db.SaveChangesAsync(cancellationToken);

        var response = new WorkspaceResponse(workspace.Id, workspace.Name, workspace.CreatedAtUtc);
        return CreatedAtAction(nameof(Get), new { id = workspace.Id }, response);
    }
}

public sealed record CreateWorkspaceRequest(string Name);
public sealed record WorkspaceResponse(Guid Id, string Name, DateTime CreatedAtUtc);
public sealed record WorkspaceDetailsResponse(Guid Id, string Name, int ChannelCount, int OpportunityCount, DateTime CreatedAtUtc);
