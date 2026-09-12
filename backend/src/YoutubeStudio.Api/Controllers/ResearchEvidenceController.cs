using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/research-projects/{researchProjectId:guid}/sources/{sourceId:guid}/evidence")]
public sealed class ResearchEvidenceController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResearchEvidenceResponse>>> GetAll(Guid researchProjectId, Guid sourceId, [FromQuery] Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!await db.ResearchSources.AnyAsync(x => x.Id == sourceId && x.ResearchProjectId == researchProjectId && x.WorkspaceId == workspaceId, cancellationToken))
            return NotFound("Research source does not exist in the specified workspace and project.");
        var evidence = await db.ResearchEvidence.AsNoTracking().Where(x => x.ResearchSourceId == sourceId && x.WorkspaceId == workspaceId).OrderBy(x => x.CreatedAtUtc).Select(x => new ResearchEvidenceResponse(x.Id, x.WorkspaceId, x.ResearchSourceId, x.Quote, x.Locator, x.Context, x.MetadataJson)).ToListAsync(cancellationToken);
        return Ok(evidence);
    }

    [HttpPost]
    public async Task<ActionResult<ResearchEvidenceResponse>> Create(Guid researchProjectId, Guid sourceId, CreateResearchEvidenceRequest request, CancellationToken cancellationToken)
    {
        if (request.ResearchSourceId != sourceId) return BadRequest("Research source id does not match the route.");
        if (string.IsNullOrWhiteSpace(request.Quote)) return BadRequest("Research evidence quote is required.");
        if (!await db.ResearchSources.AnyAsync(x => x.Id == sourceId && x.ResearchProjectId == researchProjectId && x.WorkspaceId == request.WorkspaceId, cancellationToken))
            return NotFound("Research source does not exist in the specified workspace and project.");
        var evidence = new ResearchEvidence { WorkspaceId = request.WorkspaceId, ResearchSourceId = sourceId, Quote = request.Quote.Trim(), Locator = string.IsNullOrWhiteSpace(request.Locator) ? null : request.Locator.Trim(), Context = string.IsNullOrWhiteSpace(request.Context) ? null : request.Context.Trim(), MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson.Trim() };
        db.ResearchEvidence.Add(evidence);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { researchProjectId, sourceId, workspaceId = evidence.WorkspaceId }, ToResponse(evidence));
    }

    private static ResearchEvidenceResponse ToResponse(ResearchEvidence evidence) => new(evidence.Id, evidence.WorkspaceId, evidence.ResearchSourceId, evidence.Quote, evidence.Locator, evidence.Context, evidence.MetadataJson);
}

public sealed record CreateResearchEvidenceRequest(Guid WorkspaceId, Guid ResearchSourceId, string Quote, string? Locator = null, string? Context = null, string? MetadataJson = null);
public sealed record ResearchEvidenceResponse(Guid Id, Guid WorkspaceId, Guid ResearchSourceId, string Quote, string? Locator, string? Context, string MetadataJson);
