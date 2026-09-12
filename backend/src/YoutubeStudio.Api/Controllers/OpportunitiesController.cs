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
        [FromQuery] string? status,
        [FromQuery] string sort = "opportunityScore",
        [FromQuery] string direction = "desc",
        CancellationToken cancellationToken = default)
    {
        var workspaceExists = await db.Workspaces.AnyAsync(x => x.Id == workspaceId, cancellationToken);
        if (!workspaceExists)
            return BadRequest("Workspace does not exist.");

        if (!IsSupportedSort(sort))
            return BadRequest("Sort must be opportunityScore or revenueScore.");

        if (!IsSupportedDirection(direction))
            return BadRequest("Direction must be asc or desc.");

        var normalizedSort = sort.Trim().ToLowerInvariant();
        var normalizedDirection = direction.Trim().ToLowerInvariant();
        var query = db.Opportunities
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            status = status.Trim();
            query = query.Where(x => x.Status == status);
        }

        query = (normalizedSort, normalizedDirection) switch
        {
            ("opportunityscore", "asc") => query.OrderBy(x => x.OpportunityScore).ThenBy(x => x.Title),
            ("opportunityscore", "desc") => query.OrderByDescending(x => x.OpportunityScore).ThenBy(x => x.Title),
            ("revenuescore", "asc") => query.OrderBy(x => x.RevenueScore).ThenBy(x => x.Title),
            _ => query.OrderByDescending(x => x.RevenueScore).ThenBy(x => x.Title)
        };

        var opportunities = await query
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

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Opportunity title is required.");

        if (!IsValidScore(request.OpportunityScore) || !IsValidScore(request.RevenueScore))
            return BadRequest("Opportunity and revenue scores must be between 0 and 100.");

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
            ToResponse(opportunity));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OpportunityResponse>> Update(
        Guid id,
        UpdateOpportunityRequest request,
        CancellationToken cancellationToken)
    {
        var opportunity = await db.Opportunities
            .SingleOrDefaultAsync(x => x.Id == id && x.WorkspaceId == request.WorkspaceId, cancellationToken);

        if (opportunity is null)
            return NotFound("Opportunity does not exist in the specified workspace.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Opportunity title is required.");

        if (!IsValidScore(request.OpportunityScore) || !IsValidScore(request.RevenueScore))
            return BadRequest("Opportunity and revenue scores must be between 0 and 100.");

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest("Opportunity status is required.");

        opportunity.Title = request.Title.Trim();
        opportunity.Status = request.Status.Trim();
        opportunity.AudienceProblem = request.AudienceProblem?.Trim();
        opportunity.Rationale = request.Rationale?.Trim();
        opportunity.OpportunityScore = request.OpportunityScore;
        opportunity.RevenueScore = request.RevenueScore;
        opportunity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(opportunity));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var opportunity = await db.Opportunities
            .SingleOrDefaultAsync(x => x.Id == id && x.WorkspaceId == workspaceId, cancellationToken);

        if (opportunity is null)
            return NotFound("Opportunity does not exist in the specified workspace.");

        db.Opportunities.Remove(opportunity);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static OpportunityResponse ToResponse(Opportunity opportunity) => new(
        opportunity.Id,
        opportunity.Title,
        opportunity.Status,
        opportunity.OpportunityScore,
        opportunity.RevenueScore,
        opportunity.AudienceProblem,
        opportunity.Rationale);

    private static bool IsValidScore(decimal score) => score is >= 0 and <= 100;

    private static bool IsSupportedSort(string sort) =>
        string.Equals(sort, "opportunityScore", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sort, "revenueScore", StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedDirection(string direction) =>
        string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
}

public sealed record CreateOpportunityRequest(
    Guid WorkspaceId,
    string Title,
    decimal OpportunityScore,
    decimal RevenueScore,
    string? AudienceProblem,
    string? Rationale);

public sealed record UpdateOpportunityRequest(
    Guid WorkspaceId,
    string Title,
    string Status,
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
