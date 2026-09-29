using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Per-video unit economics (REVENUE-ENGINE, AI CFO): revenue minus attributable production
/// cost, with contribution margin. Uses recorded provider/render costs and revenue events.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/video-projects/{videoProjectId:guid}/profitability")]
public sealed class ProfitabilityController(YoutubeStudioDbContext db, IWorkspaceAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<VideoProfitabilityResponse>> Get(Guid videoProjectId, CancellationToken cancellationToken)
    {
        var workspaceId = await db.VideoProjects.AsNoTracking()
            .Where(x => x.Id == videoProjectId).Select(x => (Guid?)x.WorkspaceId)
            .SingleOrDefaultAsync(cancellationToken);
        if (workspaceId is null || await access.GetRoleAsync(User, workspaceId.Value, cancellationToken) is null)
            return NotFound();

        var productionCost = await db.ProductionCosts.AsNoTracking()
            .Where(x => x.VideoProjectId == videoProjectId)
            .SumAsync(x => (decimal?)x.TotalCostUsd, cancellationToken) ?? 0m;

        var revenue = await db.RevenueEvents.AsNoTracking()
            .Where(x => x.VideoProjectId == videoProjectId)
            .SumAsync(x => (decimal?)x.AmountUsd, cancellationToken) ?? 0m;

        var contribution = revenue - productionCost;
        var margin = revenue <= 0 ? 0m : Math.Round(contribution / revenue, 4);

        return Ok(new VideoProfitabilityResponse(
            videoProjectId,
            Math.Round(revenue, 4),
            Math.Round(productionCost, 4),
            Math.Round(contribution, 4),
            margin));
    }
}

public sealed record VideoProfitabilityResponse(
    Guid VideoProjectId,
    decimal RevenueUsd,
    decimal ProductionCostUsd,
    decimal ContributionUsd,
    decimal ContributionMargin);
