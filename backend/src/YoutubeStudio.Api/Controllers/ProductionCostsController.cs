using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Exposes recorded provider/rendering costs for a video project so cost per video
/// and cost per provider can be measured (backlog US-022).
/// </summary>
[ApiController]
[Route("api/v1/video-projects/{videoProjectId:guid}/costs")]
public sealed class ProductionCostsController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProductionCostSummaryResponse>> Get(Guid videoProjectId, CancellationToken cancellationToken)
    {
        if (!await db.VideoProjects.AnyAsync(x => x.Id == videoProjectId, cancellationToken))
            return NotFound();

        var costs = await db.ProductionCosts.AsNoTracking()
            .Where(x => x.VideoProjectId == videoProjectId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var lineItems = costs
            .Select(x => new ProductionCostLineItem(x.Stage, x.Provider, x.Units, x.UnitCostUsd, x.TotalCostUsd, x.CreatedAtUtc))
            .ToList();

        var byStage = costs
            .GroupBy(x => x.Stage)
            .Select(g => new ProductionCostByStage(g.Key, g.Sum(x => x.TotalCostUsd)))
            .OrderByDescending(x => x.TotalCostUsd)
            .ToList();

        var total = costs.Sum(x => x.TotalCostUsd);

        return Ok(new ProductionCostSummaryResponse(videoProjectId, total, byStage, lineItems));
    }
}

public sealed record ProductionCostLineItem(string Stage, string Provider, decimal Units, decimal UnitCostUsd, decimal TotalCostUsd, DateTime CreatedAtUtc);
public sealed record ProductionCostByStage(string Stage, decimal TotalCostUsd);
public sealed record ProductionCostSummaryResponse(Guid VideoProjectId, decimal TotalCostUsd, IReadOnlyList<ProductionCostByStage> ByStage, IReadOnlyList<ProductionCostLineItem> LineItems);
