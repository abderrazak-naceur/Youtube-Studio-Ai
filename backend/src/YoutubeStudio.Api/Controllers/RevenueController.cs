using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Records and summarizes revenue attributed to content/channels (DATA-MODEL Revenue Event,
/// REVENUE-ENGINE). Attribution may be approximate; a confidence value is stored.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/revenue")]
public sealed class RevenueController(YoutubeStudioDbContext db, IWorkspaceAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RevenueSummaryResponse>> GetSummary(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (await access.GetRoleAsync(User, workspaceId, cancellationToken) is null) return Forbid();

        var events = await db.RevenueEvents.AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        var total = events.Sum(x => x.AmountUsd);
        var bySource = events
            .GroupBy(x => x.Source)
            .Select(g => new RevenueBySource(g.Key, g.Sum(x => x.AmountUsd)))
            .OrderByDescending(x => x.AmountUsd)
            .ToList();

        var items = events
            .Select(x => new RevenueEventResponse(x.Id, x.VideoProjectId, x.ChannelId, x.Source, x.AmountUsd, x.Cta, x.AttributionConfidence, x.OccurredAtUtc))
            .ToList();

        return Ok(new RevenueSummaryResponse(workspaceId, total, bySource, items));
    }

    [HttpPost]
    public async Task<ActionResult<RevenueEventResponse>> Record(Guid workspaceId, RecordRevenueRequest request, CancellationToken cancellationToken)
    {
        if (await access.GetRoleAsync(User, workspaceId, cancellationToken) is null) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Source)) return ValidationProblem("A revenue source is required.");
        if (request.AmountUsd < 0) return ValidationProblem("Revenue amount cannot be negative.");
        if (request.AttributionConfidence is < 0 or > 1) return ValidationProblem("Attribution confidence must be between 0 and 1.");

        var revenue = new RevenueEvent
        {
            WorkspaceId = workspaceId,
            VideoProjectId = request.VideoProjectId,
            ChannelId = request.ChannelId,
            Source = request.Source.Trim(),
            AmountUsd = request.AmountUsd,
            Cta = string.IsNullOrWhiteSpace(request.Cta) ? null : request.Cta.Trim(),
            AttributionConfidence = request.AttributionConfidence,
            OccurredAtUtc = request.OccurredAtUtc == default ? DateTime.UtcNow : request.OccurredAtUtc
        };

        db.RevenueEvents.Add(revenue);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetSummary), new { workspaceId },
            new RevenueEventResponse(revenue.Id, revenue.VideoProjectId, revenue.ChannelId, revenue.Source, revenue.AmountUsd, revenue.Cta, revenue.AttributionConfidence, revenue.OccurredAtUtc));
    }
}

public sealed record RecordRevenueRequest(
    Guid? VideoProjectId, Guid? ChannelId, string Source, decimal AmountUsd, string? Cta,
    double AttributionConfidence = 1.0, DateTime OccurredAtUtc = default);

public sealed record RevenueEventResponse(
    Guid Id, Guid? VideoProjectId, Guid? ChannelId, string Source, decimal AmountUsd, string? Cta, double AttributionConfidence, DateTime OccurredAtUtc);

public sealed record RevenueBySource(string Source, decimal AmountUsd);
public sealed record RevenueSummaryResponse(Guid WorkspaceId, decimal TotalUsd, IReadOnlyList<RevenueBySource> BySource, IReadOnlyList<RevenueEventResponse> Events);
