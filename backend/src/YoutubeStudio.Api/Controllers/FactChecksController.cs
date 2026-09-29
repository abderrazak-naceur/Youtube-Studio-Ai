using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/research-projects/{researchProjectId:guid}/fact-check")]
public sealed class FactChecksController(YoutubeStudioDbContext db) : ControllerBase
{
    // Domains that require stronger human review before publication (PROJECT-STUDY §13/§14).
    private static readonly IReadOnlyDictionary<string, string[]> HighRiskDomains = new Dictionary<string, string[]>
    {
        ["finance"] = ["invest", "stock", "crypto", "return", "profit", "loan", "tax", "retirement", "trading"],
        ["health"] = ["health", "medical", "disease", "cure", "treatment", "symptom", "diagnos", "drug", "vaccine", "diet"],
        ["legal"] = ["legal", "lawsuit", "court", "attorney", "liable", "regulation", "compliance", "contract"],
        ["political"] = ["election", "government", "policy", "president", "senate", "vote", "immigration"]
    };

    [HttpGet]
    public async Task<ActionResult<FactCheckReportResponse>> Get(
        Guid researchProjectId,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(researchProjectId, workspaceId, cancellationToken))
            return NotFound("Research project does not exist in the specified workspace.");

        var report = await db.FactCheckReports.AsNoTracking()
            .Include(x => x.Findings)
            .SingleOrDefaultAsync(x => x.ResearchProjectId == researchProjectId && x.WorkspaceId == workspaceId, cancellationToken);
        if (report is null) return NotFound("Fact check has not been run for this project.");
        return Ok(ToResponse(report));
    }

    [HttpPost]
    public async Task<ActionResult<FactCheckReportResponse>> Run(
        Guid researchProjectId,
        RunFactCheckRequest request,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(researchProjectId, request.WorkspaceId, cancellationToken))
            return NotFound("Research project does not exist in the specified workspace.");

        var claims = await db.ResearchClaims
            .Include(x => x.EvidenceLinks)
            .Where(x => x.ResearchProjectId == researchProjectId && x.WorkspaceId == request.WorkspaceId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (claims.Count == 0) return BadRequest("At least one research claim is required before a fact check can run.");

        var existing = await db.FactCheckReports
            .Include(x => x.Findings)
            .SingleOrDefaultAsync(x => x.ResearchProjectId == researchProjectId && x.WorkspaceId == request.WorkspaceId, cancellationToken);

        var report = existing ?? new FactCheckReport { WorkspaceId = request.WorkspaceId, ResearchProjectId = researchProjectId };
        if (existing is null) db.FactCheckReports.Add(report);
        else db.FactCheckFindings.RemoveRange(existing.Findings);

        var findings = new List<FactCheckFinding>();
        foreach (var claim in claims)
        {
            var evidenceCount = claim.EvidenceLinks.Count;
            var (riskLevel, requiresReview) = AssessRisk(claim.Text);
            var (status, rationale) = Evaluate(claim.VerificationStatus, evidenceCount);
            findings.Add(new FactCheckFinding
            {
                WorkspaceId = request.WorkspaceId,
                FactCheckReportId = report.Id,
                ResearchClaimId = claim.Id,
                Status = status,
                RiskLevel = riskLevel,
                EvidenceCount = evidenceCount,
                RequiresHumanReview = requiresReview,
                Rationale = rationale
            });
        }

        report.ClaimCount = claims.Count;
        report.VerifiedClaimCount = claims.Count(c => c.VerificationStatus == "verified");
        report.DisputedClaimCount = claims.Count(c => c.VerificationStatus == "disputed");
        report.UnverifiedClaimCount = claims.Count(c => c.VerificationStatus == "unverified");
        report.UnsupportedClaimCount = findings.Count(f => f.Status == "unsupported");
        report.RequiresHumanReview = findings.Any(f => f.RequiresHumanReview);
        report.Verdict = DetermineVerdict(findings);
        report.UpdatedAtUtc = DateTime.UtcNow;
        report.Findings = findings;
        db.FactCheckFindings.AddRange(findings);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { researchProjectId, workspaceId = report.WorkspaceId }, ToResponse(report));
    }

    private static (string Status, string Rationale) Evaluate(string verificationStatus, int evidenceCount) => verificationStatus switch
    {
        "disputed" => ("disputed", "Claim is marked disputed and must be resolved before publication."),
        "verified" when evidenceCount > 0 => ("supported", $"Verified claim backed by {evidenceCount} evidence record{(evidenceCount == 1 ? "" : "s")}."),
        "verified" => ("unsupported", "Claim is marked verified but has no linked evidence."),
        _ => ("pending", "Claim is not yet verified and needs review.")
    };

    private static (string RiskLevel, bool RequiresReview) AssessRisk(string text)
    {
        var lowered = text.ToLowerInvariant();
        foreach (var (_, keywords) in HighRiskDomains)
        {
            if (keywords.Any(keyword => lowered.Contains(keyword))) return ("high", true);
        }
        return ("low", false);
    }

    private static string DetermineVerdict(IReadOnlyCollection<FactCheckFinding> findings)
    {
        if (findings.Any(f => f.Status is "disputed" or "unsupported")) return "failed";
        if (findings.Any(f => f.Status == "pending") || findings.Any(f => f.RequiresHumanReview)) return "needs_review";
        return "passed";
    }

    private Task<bool> ProjectExistsAsync(Guid projectId, Guid workspaceId, CancellationToken cancellationToken) =>
        db.ResearchProjects.AnyAsync(x => x.Id == projectId && x.WorkspaceId == workspaceId, cancellationToken);

    private static FactCheckReportResponse ToResponse(FactCheckReport report) => new(
        report.Id, report.WorkspaceId, report.ResearchProjectId, report.Verdict,
        report.ClaimCount, report.VerifiedClaimCount, report.DisputedClaimCount, report.UnverifiedClaimCount, report.UnsupportedClaimCount,
        report.RequiresHumanReview,
        report.Findings.OrderByDescending(f => f.RiskLevel == "high").ThenBy(f => f.Status)
            .Select(f => new FactCheckFindingResponse(f.Id, f.ResearchClaimId, f.Status, f.RiskLevel, f.EvidenceCount, f.RequiresHumanReview, f.Rationale)).ToList(),
        report.CreatedAtUtc, report.UpdatedAtUtc);
}

public sealed record RunFactCheckRequest(Guid WorkspaceId);
public sealed record FactCheckFindingResponse(Guid Id, Guid ResearchClaimId, string Status, string RiskLevel, int EvidenceCount, bool RequiresHumanReview, string Rationale);
public sealed record FactCheckReportResponse(
    Guid Id, Guid WorkspaceId, Guid ResearchProjectId, string Verdict,
    int ClaimCount, int VerifiedClaimCount, int DisputedClaimCount, int UnverifiedClaimCount, int UnsupportedClaimCount,
    bool RequiresHumanReview, IReadOnlyList<FactCheckFindingResponse> Findings, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
