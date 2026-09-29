using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services;
using YoutubeStudio.Api.Services.Auth;
using YoutubeStudio.Api.Services.Scoring;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/opportunities")]
public sealed class OpportunitiesController(YoutubeStudioDbContext db, IWorkspaceAccess access, IDomainEventPublisher events) : ControllerBase
{
    [HttpPost("score")]
    public async Task<ActionResult<OpportunityScoreResponse>> Score(ScoreOpportunityRequest request, CancellationToken cancellationToken)
    {
        if (await access.GetRoleAsync(User, request.WorkspaceId, cancellationToken) is null)
            return Forbid();

        var factors = new OpportunityFactors(
            request.AudienceDemand, request.SearchIntent, request.TrendMomentum, request.Competition,
            request.ChannelFit, request.MonetizationPotential, request.ProductionEffort,
            request.OriginalityPotential, request.EvidenceQuality);

        var weights = request.Weights is null
            ? OpportunityWeights.Default
            : new OpportunityWeights(
                request.Weights.AudienceDemand, request.Weights.SearchIntent, request.Weights.TrendMomentum,
                request.Weights.Competition, request.Weights.ChannelFit, request.Weights.MonetizationPotential,
                request.Weights.ProductionEffort, request.Weights.OriginalityPotential, request.Weights.EvidenceQuality);

        var result = OpportunityScoring.Score(factors, weights);
        return Ok(new OpportunityScoreResponse(result.OpportunityScore, result.RevenueScore, result.Contributions));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OpportunityResponse>>> GetAll(
        [FromQuery] Guid workspaceId,
        [FromQuery] string? status,
        [FromQuery] string sort = "opportunityScore",
        [FromQuery] string direction = "desc",
        CancellationToken cancellationToken = default)
    {
        if (await access.GetRoleAsync(User, workspaceId, cancellationToken) is null)
            return Forbid();

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
        if (await access.GetRoleAsync(User, request.WorkspaceId, cancellationToken) is null)
            return Forbid();

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
        events.Publish("opportunity.created", opportunity.WorkspaceId, opportunity.Id,
            new { opportunity.Title, opportunity.OpportunityScore, opportunity.RevenueScore });
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
        if (await access.GetRoleAsync(User, request.WorkspaceId, cancellationToken) is null)
            return Forbid();

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
        if (await access.GetRoleAsync(User, workspaceId, cancellationToken) is null)
            return Forbid();

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

public sealed record OpportunityWeightsPayload(
    double AudienceDemand = 1.0,
    double SearchIntent = 1.0,
    double TrendMomentum = 0.8,
    double Competition = 1.0,
    double ChannelFit = 1.2,
    double MonetizationPotential = 1.5,
    double ProductionEffort = 0.8,
    double OriginalityPotential = 1.0,
    double EvidenceQuality = 1.0);

public sealed record ScoreOpportunityRequest(
    Guid WorkspaceId,
    double AudienceDemand,
    double SearchIntent,
    double TrendMomentum,
    double Competition,
    double ChannelFit,
    double MonetizationPotential,
    double ProductionEffort,
    double OriginalityPotential,
    double EvidenceQuality,
    OpportunityWeightsPayload? Weights = null);

public sealed record OpportunityScoreResponse(
    double OpportunityScore,
    double RevenueScore,
    IReadOnlyDictionary<string, double> Contributions);
