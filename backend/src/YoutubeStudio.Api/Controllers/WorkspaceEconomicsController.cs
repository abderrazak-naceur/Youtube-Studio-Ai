using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Workspace-level unit economics for the AI CFO view (REVENUE-ENGINE): total revenue,
/// production cost, contribution and margin, with a per-video breakdown.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/economics")]
public sealed class WorkspaceEconomicsController(YoutubeStudioDbContext db, IWorkspaceAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WorkspaceEconomicsResponse>> Get(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (await access.GetRoleAsync(User, workspaceId, cancellationToken) is null) return Forbid();

        var projects = await db.VideoProjects.AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .Select(x => new { x.Id, x.Title, x.Prompt })
            .ToListAsync(cancellationToken);
        var projectIds = projects.Select(p => p.Id).ToList();

        var costByProject = (await db.ProductionCosts.AsNoTracking()
            .Where(x => projectIds.Contains(x.VideoProjectId))
            .GroupBy(x => x.VideoProjectId)
            .Select(g => new { ProjectId = g.Key, Total = g.Sum(x => x.TotalCostUsd) })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ProjectId, x => x.Total);

        var revenueByProject = (await db.RevenueEvents.AsNoTracking()
            .Where(x => x.VideoProjectId != null && projectIds.Contains(x.VideoProjectId!.Value))
            .GroupBy(x => x.VideoProjectId!.Value)
            .Select(g => new { ProjectId = g.Key, Total = g.Sum(x => x.AmountUsd) })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ProjectId, x => x.Total);

        var perVideo = projects
            .Select(p =>
            {
                var cost = costByProject.GetValueOrDefault(p.Id, 0m);
                var revenue = revenueByProject.GetValueOrDefault(p.Id, 0m);
                return new VideoEconomics(p.Id, p.Title ?? p.Prompt, Math.Round(revenue, 4), Math.Round(cost, 4), Math.Round(revenue - cost, 4));
            })
            .OrderByDescending(x => x.ContributionUsd)
            .ToList();

        var totalRevenue = perVideo.Sum(x => x.RevenueUsd);
        var totalCost = perVideo.Sum(x => x.ProductionCostUsd);
        var contribution = totalRevenue - totalCost;
        var margin = totalRevenue <= 0 ? 0m : Math.Round(contribution / totalRevenue, 4);

        return Ok(new WorkspaceEconomicsResponse(
            workspaceId,
            Math.Round(totalRevenue, 4),
            Math.Round(totalCost, 4),
            Math.Round(contribution, 4),
            margin,
            perVideo));
    }
}

public sealed record VideoEconomics(Guid VideoProjectId, string Title, decimal RevenueUsd, decimal ProductionCostUsd, decimal ContributionUsd);
public sealed record WorkspaceEconomicsResponse(
    Guid WorkspaceId, decimal TotalRevenueUsd, decimal TotalProductionCostUsd, decimal TotalContributionUsd, decimal ContributionMargin,
    IReadOnlyList<VideoEconomics> Videos);
