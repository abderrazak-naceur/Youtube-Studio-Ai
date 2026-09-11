using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/workspaces")]
public sealed class WorkspacesController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkspaceResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var workspaces = await db.Workspaces
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new WorkspaceResponse(x.Id, x.Name, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(workspaces);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkspaceDetailsResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
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
        if (string.IsNullOrWhiteSpace(request.Name))
            return ValidationProblem("Workspace name is required.");

        var workspace = new Workspace { Name = request.Name.Trim() };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);

        var response = new WorkspaceResponse(workspace.Id, workspace.Name, workspace.CreatedAtUtc);
        return CreatedAtAction(nameof(Get), new { id = workspace.Id }, response);
    }
}

public sealed record CreateWorkspaceRequest(string Name);
public sealed record WorkspaceResponse(Guid Id, string Name, DateTime CreatedAtUtc);
public sealed record WorkspaceDetailsResponse(Guid Id, string Name, int ChannelCount, int OpportunityCount, DateTime CreatedAtUtc);
