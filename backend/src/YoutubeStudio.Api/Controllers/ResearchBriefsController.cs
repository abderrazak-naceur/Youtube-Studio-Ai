using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/research-projects/{researchProjectId:guid}/brief")]
public sealed class ResearchBriefsController(YoutubeStudioDbContext db, IWorkspaceAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ResearchBriefResponse>> Get(
        Guid researchProjectId,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(researchProjectId, workspaceId, cancellationToken))
            return NotFound("Research project does not exist in the specified workspace.");

        var brief = await db.ResearchBriefs.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ResearchProjectId == researchProjectId && x.WorkspaceId == workspaceId, cancellationToken);
        if (brief is null) return NotFound("Research brief has not been saved for this project.");
        return Ok(ToResponse(brief));
    }

    [HttpPost]
    public async Task<ActionResult<ResearchBriefResponse>> Save(
        Guid researchProjectId,
        SaveResearchBriefRequest request,
        CancellationToken cancellationToken)
    {
        var project = await db.ResearchProjects
            .Include(x => x.Opportunity)
            .Include(x => x.Sources)
            .Include(x => x.Brief)
            .SingleOrDefaultAsync(x => x.Id == researchProjectId && x.WorkspaceId == request.WorkspaceId, cancellationToken);
        if (project is null) return NotFound("Research project does not exist in the specified workspace.");

        var claims = await db.ResearchClaims
            .Include(x => x.EvidenceLinks)
            .ThenInclude(x => x.ResearchEvidence)
            .ThenInclude(x => x.ResearchSource)
            .Where(x => x.ResearchProjectId == researchProjectId && x.WorkspaceId == request.WorkspaceId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var verifiedClaims = claims.Where(claim => claim.VerificationStatus == "verified").ToList();
        if (verifiedClaims.Count == 0) return BadRequest("At least one verified claim is required before a production brief can be saved.");

        var pendingClaimCount = claims.Count(claim => claim.VerificationStatus != "verified");
        var markdown = BuildMarkdown(project.Opportunity.Title, verifiedClaims, project.Sources.OrderBy(source => source.Title).ToList(), pendingClaimCount);
        var status = pendingClaimCount == 0 ? "published" : "draft";

        var brief = project.Brief;
        if (brief is null)
        {
            brief = new ResearchBrief { WorkspaceId = request.WorkspaceId, ResearchProjectId = researchProjectId };
            db.ResearchBriefs.Add(brief);
            project.Brief = brief;
        }

        brief.Markdown = markdown;
        brief.Status = status;
        brief.PendingClaimCount = pendingClaimCount;
        brief.UpdatedAtUtc = DateTime.UtcNow;
        project.Status = status == "published" ? "complete" : project.Status;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { researchProjectId, workspaceId = brief.WorkspaceId }, ToResponse(brief));
    }

    private async Task<bool> ProjectExistsAsync(Guid projectId, Guid workspaceId, CancellationToken cancellationToken) =>
        await access.GetRoleAsync(User, workspaceId, cancellationToken) is not null &&
        await db.ResearchProjects.AnyAsync(x => x.Id == projectId && x.WorkspaceId == workspaceId, cancellationToken);

    private static ResearchBriefResponse ToResponse(ResearchBrief brief) =>
        new(brief.Id, brief.WorkspaceId, brief.ResearchProjectId, brief.Markdown, brief.Status, brief.PendingClaimCount, brief.CreatedAtUtc, brief.UpdatedAtUtc);

    private static string BuildMarkdown(string projectTitle, IReadOnlyList<ResearchClaim> verifiedClaims, IReadOnlyList<ResearchSource> sources, int pendingClaimCount)
    {
        var lines = new StringBuilder();
        var title = string.IsNullOrWhiteSpace(projectTitle) ? "Research brief" : projectTitle.Trim();
        lines.AppendLine($"# Research brief: {title}");
        lines.AppendLine();
        lines.AppendLine("## Verified claims");
        foreach (var claim in verifiedClaims)
        {
            lines.AppendLine($"- {claim.Text}");
            foreach (var link in claim.EvidenceLinks)
            {
                var evidence = link.ResearchEvidence;
                var locator = string.IsNullOrWhiteSpace(evidence.Locator) ? "" : $" ({evidence.Locator})";
                lines.AppendLine($"  - Evidence: “{evidence.Quote}” — {evidence.ResearchSource.Title}{locator}");
            }
        }

        lines.AppendLine();
        lines.AppendLine("## Sources");
        if (sources.Count == 0) lines.AppendLine("No sources recorded.");
        foreach (var source in sources) lines.AppendLine($"- {source.Title}: {source.Url}");
        lines.AppendLine();
        lines.AppendLine("## Review status");
        lines.AppendLine($"- {pendingClaimCount} claim{(pendingClaimCount == 1 ? "" : "s")} still require review.");
        return lines.ToString().TrimEnd() + "\n";
    }
}

public sealed record SaveResearchBriefRequest(Guid WorkspaceId);
public sealed record ResearchBriefResponse(Guid Id, Guid WorkspaceId, Guid ResearchProjectId, string Markdown, string Status, int PendingClaimCount, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
