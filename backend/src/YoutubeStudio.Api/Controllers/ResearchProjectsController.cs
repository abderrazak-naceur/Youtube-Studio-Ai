using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/research-projects")]
public sealed class ResearchProjectsController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResearchProjectResponse>>> GetAll([FromQuery] Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!await db.Workspaces.AnyAsync(x => x.Id == workspaceId, cancellationToken)) return BadRequest("Workspace does not exist.");
        var projects = await db.ResearchProjects.AsNoTracking().Where(x => x.WorkspaceId == workspaceId).OrderByDescending(x => x.CreatedAtUtc).Select(x => new ResearchProjectResponse(x.Id, x.WorkspaceId, x.OpportunityId, x.Status)).ToListAsync(cancellationToken);
        return Ok(projects);
    }

    [HttpPost]
    public async Task<ActionResult<ResearchProjectResponse>> Create(CreateResearchProjectRequest request, CancellationToken cancellationToken)
    {
        var opportunity = await db.Opportunities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.OpportunityId && x.WorkspaceId == request.WorkspaceId, cancellationToken);
        if (opportunity is null) return NotFound("Opportunity does not exist in the specified workspace.");
        if (await db.ResearchProjects.AsNoTracking().AnyAsync(x => x.OpportunityId == request.OpportunityId, cancellationToken)) return Conflict("A research project already exists for this opportunity.");
        var project = new ResearchProject { WorkspaceId = request.WorkspaceId, OpportunityId = request.OpportunityId, Status = "draft" };
        db.ResearchProjects.Add(project); await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { workspaceId = project.WorkspaceId }, new ResearchProjectResponse(project.Id, project.WorkspaceId, project.OpportunityId, project.Status));
    }
}

public sealed record CreateResearchProjectRequest(Guid WorkspaceId, Guid OpportunityId);
public sealed record ResearchProjectResponse(Guid Id, Guid WorkspaceId, Guid OpportunityId, string Status);
