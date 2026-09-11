using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/opportunities")]
public sealed class OpportunitiesController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OpportunityResponse>>> GetAll(
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var opportunities = await db.Opportunities
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.OpportunityScore)
            .Select(x => new OpportunityResponse(
                x.Id,
                x.Title,
                x.Status,
                x.OpportunityScore,
                x.RevenueScore,
                x.AudienceProblem,
                x.Rationale))
            .ToListAsync(cancellationToken);

        return Ok(opportunities);
    }

    [HttpPost]
    public async Task<ActionResult<OpportunityResponse>> Create(
        CreateOpportunityRequest request,
        CancellationToken cancellationToken)
    {
        var exists = await db.Workspaces.AnyAsync(x => x.Id == request.WorkspaceId, cancellationToken);
        if (!exists)
            return BadRequest("Workspace does not exist.");

        var opportunity = new Opportunity
        {
            WorkspaceId = request.WorkspaceId,
            Title = request.Title.Trim(),
            AudienceProblem = request.AudienceProblem?.Trim(),
            Rationale = request.Rationale?.Trim(),
            OpportunityScore = request.OpportunityScore,
            RevenueScore = request.RevenueScore
        };

        db.Opportunities.Add(opportunity);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { workspaceId = opportunity.WorkspaceId },
            new OpportunityResponse(
                opportunity.Id,
                opportunity.Title,
                opportunity.Status,
                opportunity.OpportunityScore,
                opportunity.RevenueScore,
                opportunity.AudienceProblem,
                opportunity.Rationale));
    }
}

public sealed record CreateOpportunityRequest(
    Guid WorkspaceId,
    string Title,
    decimal OpportunityScore,
    decimal RevenueScore,
    string? AudienceProblem,
    string? Rationale);

public sealed record OpportunityResponse(
    Guid Id,
    string Title,
    string Status,
    decimal OpportunityScore,
    decimal RevenueScore,
    string? AudienceProblem,
    string? Rationale);
