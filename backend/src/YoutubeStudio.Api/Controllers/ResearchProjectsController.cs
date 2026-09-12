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

    [HttpGet("{researchProjectId:guid}/sources")]
    public async Task<ActionResult<IReadOnlyList<ResearchSourceResponse>>> GetSources(Guid researchProjectId, [FromQuery] Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!await db.ResearchProjects.AnyAsync(x => x.Id == researchProjectId && x.WorkspaceId == workspaceId, cancellationToken)) return NotFound("Research project does not exist in the specified workspace.");
        var sources = await db.ResearchSources.AsNoTracking().Where(x => x.ResearchProjectId == researchProjectId && x.WorkspaceId == workspaceId).OrderBy(x => x.CreatedAtUtc).Select(x => new ResearchSourceResponse(x.Id, x.ResearchProjectId, x.WorkspaceId, x.Url, x.Title, x.MetadataJson)).ToListAsync(cancellationToken);
        return Ok(sources);
    }

    [HttpPost("{researchProjectId:guid}/sources")]
    public async Task<ActionResult<ResearchSourceResponse>> AddSource(Guid researchProjectId, CreateResearchSourceRequest request, CancellationToken cancellationToken)
    {
        if (request.ResearchProjectId != researchProjectId) return BadRequest("Research project id does not match the route.");
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) return BadRequest("Url must be an absolute HTTP or HTTPS URL.");
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Title is required.");
        if (!await db.ResearchProjects.AnyAsync(x => x.Id == researchProjectId && x.WorkspaceId == request.WorkspaceId, cancellationToken)) return NotFound("Research project does not exist in the specified workspace.");
        var source = new ResearchSource { WorkspaceId = request.WorkspaceId, ResearchProjectId = researchProjectId, Url = request.Url.Trim(), Title = request.Title.Trim(), MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson };
        db.ResearchSources.Add(source); await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetSources), new { researchProjectId, workspaceId = source.WorkspaceId }, new ResearchSourceResponse(source.Id, source.ResearchProjectId, source.WorkspaceId, source.Url, source.Title, source.MetadataJson));
    }

    [HttpGet("{researchProjectId:guid}/sources/{sourceId:guid}/evidence")]
    public async Task<ActionResult<IReadOnlyList<ResearchEvidenceResponse>>> GetEvidence(Guid researchProjectId, Guid sourceId, [FromQuery] Guid workspaceId, CancellationToken cancellationToken)
    {
        var sourceExists = await db.ResearchSources.AnyAsync(x => x.Id == sourceId && x.ResearchProjectId == researchProjectId && x.WorkspaceId == workspaceId, cancellationToken);
        if (!sourceExists) return NotFound("Research source does not exist in the specified workspace.");
        var evidence = await db.ResearchEvidence.AsNoTracking().Where(x => x.ResearchSourceId == sourceId && x.WorkspaceId == workspaceId).OrderBy(x => x.CreatedAtUtc).Select(x => new ResearchEvidenceResponse(x.Id, x.ResearchSourceId, x.WorkspaceId, x.Quote, x.Locator, x.Context, x.MetadataJson)).ToListAsync(cancellationToken);
        return Ok(evidence);
    }

    [HttpPost("{researchProjectId:guid}/sources/{sourceId:guid}/evidence")]
    public async Task<ActionResult<ResearchEvidenceResponse>> AddEvidence(Guid researchProjectId, Guid sourceId, CreateResearchEvidenceRequest request, CancellationToken cancellationToken)
    {
        if (request.ResearchSourceId != sourceId) return BadRequest("Research source id does not match the route.");
        if (string.IsNullOrWhiteSpace(request.Quote)) return BadRequest("Quote is required.");
        var sourceExists = await db.ResearchSources.AnyAsync(x => x.Id == sourceId && x.ResearchProjectId == researchProjectId && x.WorkspaceId == request.WorkspaceId, cancellationToken);
        if (!sourceExists) return NotFound("Research source does not exist in the specified workspace.");
        var evidence = new ResearchEvidence { WorkspaceId = request.WorkspaceId, ResearchSourceId = sourceId, Quote = request.Quote.Trim(), Locator = string.IsNullOrWhiteSpace(request.Locator) ? null : request.Locator.Trim(), Context = string.IsNullOrWhiteSpace(request.Context) ? null : request.Context.Trim(), MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson };
        db.ResearchEvidence.Add(evidence); await db.SaveChangesAsync(cancellationToken);
        var response = new ResearchEvidenceResponse(evidence.Id, evidence.ResearchSourceId, evidence.WorkspaceId, evidence.Quote, evidence.Locator, evidence.Context, evidence.MetadataJson);
        return CreatedAtAction(nameof(GetEvidence), new { researchProjectId, sourceId, workspaceId = evidence.WorkspaceId }, response);
    }
}

public sealed record CreateResearchProjectRequest(Guid WorkspaceId, Guid OpportunityId);
public sealed record ResearchProjectResponse(Guid Id, Guid WorkspaceId, Guid OpportunityId, string Status);
public sealed record CreateResearchSourceRequest(Guid WorkspaceId, Guid ResearchProjectId, string Url, string Title, string? MetadataJson);
public sealed record ResearchSourceResponse(Guid Id, Guid ResearchProjectId, Guid WorkspaceId, string Url, string Title, string MetadataJson);
public sealed record CreateResearchEvidenceRequest(Guid WorkspaceId, Guid ResearchSourceId, string Quote, string? Locator, string? Context, string? MetadataJson);
public sealed record ResearchEvidenceResponse(Guid Id, Guid ResearchSourceId, Guid WorkspaceId, string Quote, string? Locator, string? Context, string MetadataJson);
