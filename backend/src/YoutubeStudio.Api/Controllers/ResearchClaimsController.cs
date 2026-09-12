using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/research-projects/{researchProjectId:guid}/claims")]
public sealed class ResearchClaimsController(YoutubeStudioDbContext db) : ControllerBase
{
    private static readonly HashSet<string> VerificationStatuses = ["unverified", "verified", "disputed"];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResearchClaimResponse>>> GetAll(
        Guid researchProjectId,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(researchProjectId, workspaceId, cancellationToken))
            return NotFound("Research project does not exist in the specified workspace.");

        var claims = await db.ResearchClaims.AsNoTracking()
            .Where(x => x.ResearchProjectId == researchProjectId && x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ResearchClaimResponse(x.Id, x.WorkspaceId, x.ResearchProjectId, x.Text, x.VerificationStatus, x.MetadataJson, x.EvidenceLinks.Select(l => l.ResearchEvidenceId).ToList(), x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(claims);
    }

    [HttpPost]
    public async Task<ActionResult<ResearchClaimResponse>> Create(
        Guid researchProjectId,
        CreateResearchClaimRequest request,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(researchProjectId, request.WorkspaceId, cancellationToken))
            return NotFound("Research project does not exist in the specified workspace.");

        if (string.IsNullOrWhiteSpace(request.Text)) return BadRequest("Research claim text is required.");
        if (request.EvidenceIds is null || request.EvidenceIds.Count == 0) return BadRequest("At least one evidence record is required for a claim.");
        if (!VerificationStatuses.Contains(request.VerificationStatus ?? "unverified")) return BadRequest("Verification status must be unverified, verified or disputed.");

        var evidenceIds = request.EvidenceIds.Distinct().ToArray();
        var validEvidenceIds = await db.ResearchEvidence.AsNoTracking()
            .Where(e => evidenceIds.Contains(e.Id) && e.WorkspaceId == request.WorkspaceId && e.ResearchSource.ResearchProjectId == researchProjectId && e.ResearchSource.WorkspaceId == request.WorkspaceId)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (validEvidenceIds.Count != evidenceIds.Length) return BadRequest("Every evidence record must belong to the specified research project and workspace.");

        var claim = new ResearchClaim
        {
            WorkspaceId = request.WorkspaceId,
            ResearchProjectId = researchProjectId,
            Text = request.Text.Trim(),
            VerificationStatus = request.VerificationStatus ?? "unverified",
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson.Trim()
        };
        db.ResearchClaims.Add(claim);
        foreach (var evidenceId in evidenceIds) db.ResearchClaimEvidence.Add(new ResearchClaimEvidence { ResearchClaimId = claim.Id, ResearchEvidenceId = evidenceId });
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { researchProjectId, workspaceId = claim.WorkspaceId }, await ToResponseAsync(claim.Id, cancellationToken));
    }

    [HttpPut("{id:guid}/verification")]
    public async Task<ActionResult<ResearchClaimResponse>> UpdateVerification(
        Guid researchProjectId,
        Guid id,
        UpdateResearchClaimVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!VerificationStatuses.Contains(request.VerificationStatus)) return BadRequest("Verification status must be unverified, verified or disputed.");
        var claim = await db.ResearchClaims.SingleOrDefaultAsync(x => x.Id == id && x.ResearchProjectId == researchProjectId && x.WorkspaceId == request.WorkspaceId, cancellationToken);
        if (claim is null) return NotFound("Research claim does not exist in the specified workspace and project.");
        claim.VerificationStatus = request.VerificationStatus;
        claim.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await ToResponseAsync(claim.Id, cancellationToken));
    }

    private Task<bool> ProjectExistsAsync(Guid projectId, Guid workspaceId, CancellationToken cancellationToken) => db.ResearchProjects.AnyAsync(x => x.Id == projectId && x.WorkspaceId == workspaceId, cancellationToken);

    private async Task<ResearchClaimResponse> ToResponseAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.ResearchClaims.AsNoTracking().Where(x => x.Id == id).Select(x => new ResearchClaimResponse(x.Id, x.WorkspaceId, x.ResearchProjectId, x.Text, x.VerificationStatus, x.MetadataJson, x.EvidenceLinks.Select(l => l.ResearchEvidenceId).ToList(), x.CreatedAtUtc, x.UpdatedAtUtc)).SingleAsync(cancellationToken);
    }
}

public sealed record CreateResearchClaimRequest(Guid WorkspaceId, string Text, IReadOnlyCollection<Guid> EvidenceIds, string? VerificationStatus = null, string? MetadataJson = null);
public sealed record UpdateResearchClaimVerificationRequest(Guid WorkspaceId, string VerificationStatus);
public sealed record ResearchClaimResponse(Guid Id, Guid WorkspaceId, Guid ResearchProjectId, string Text, string VerificationStatus, string MetadataJson, IReadOnlyList<Guid> EvidenceIds, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
