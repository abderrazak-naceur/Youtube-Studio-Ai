using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Learning-loop analytics: correlates Content Genome attributes with measured outcomes
/// (ANALYTICS-INTELLIGENCE Content Genome feedback). Results are correlational hypotheses,
/// not causal claims (the doc requires avoiding causal language).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/analytics")]
public sealed class AnalyticsController(YoutubeStudioDbContext db, IWorkspaceAccess access) : ControllerBase
{
    [HttpGet("genome-correlation")]
    public async Task<ActionResult<GenomeCorrelationResponse>> GenomeCorrelation(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (await access.GetRoleAsync(User, workspaceId, cancellationToken) is null) return Forbid();

        // Latest outcome per project in this workspace.
        var projectIds = await db.VideoProjects.AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var genomes = await db.ContentGenomes.AsNoTracking()
            .Where(x => projectIds.Contains(x.VideoProjectId))
            .Select(x => new { x.VideoProjectId, x.AttributesJson })
            .ToListAsync(cancellationToken);

        var outcomes = await db.VideoOutcomes.AsNoTracking()
            .Where(x => projectIds.Contains(x.VideoProjectId))
            .ToListAsync(cancellationToken);

        // Use the most recent outcome per project as its performance snapshot.
        var latestByProject = outcomes
            .GroupBy(x => x.VideoProjectId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.MeasuredAtUtc).First());

        // Accumulate views per attribute across projects that have both a genome and an outcome.
        var acc = new Dictionary<string, (int Count, long Views, decimal Revenue)>(StringComparer.OrdinalIgnoreCase);
        var analyzedProjects = 0;

        foreach (var genome in genomes)
        {
            if (!latestByProject.TryGetValue(genome.VideoProjectId, out var outcome)) continue;
            analyzedProjects++;

            foreach (var attribute in ParseAttributes(genome.AttributesJson))
            {
                var current = acc.TryGetValue(attribute, out var v) ? v : (0, 0L, 0m);
                acc[attribute] = (current.Item1 + 1, current.Item2 + outcome.Views, current.Item3 + outcome.EstimatedRevenueUsd);
            }
        }

        var correlations = acc
            .Select(kv => new AttributeCorrelation(
                kv.Key,
                kv.Value.Count,
                kv.Value.Count == 0 ? 0 : Math.Round(kv.Value.Views / (double)kv.Value.Count, 2),
                kv.Value.Count == 0 ? 0 : Math.Round(kv.Value.Revenue / kv.Value.Count, 4)))
            .OrderByDescending(x => x.AverageViews)
            .ToList();

        return Ok(new GenomeCorrelationResponse(
            workspaceId,
            analyzedProjects,
            "Correlational only — not causal. Use experiments to test hypotheses.",
            correlations));
    }

    private static IEnumerable<string> ParseAttributes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) yield break;
        List<string>? attributes = null;
        try { attributes = JsonSerializer.Deserialize<List<string>>(json); }
        catch (JsonException) { yield break; }
        if (attributes is null) yield break;
        foreach (var a in attributes)
            if (!string.IsNullOrWhiteSpace(a)) yield return a.Trim();
    }
}

public sealed record AttributeCorrelation(string Attribute, int VideoCount, double AverageViews, decimal AverageRevenueUsd);
public sealed record GenomeCorrelationResponse(Guid WorkspaceId, int AnalyzedProjects, string Disclaimer, IReadOnlyList<AttributeCorrelation> Attributes);
