namespace YoutubeStudio.Api.Services.Ai;

/// <summary>Task categories the router can route (MODEL-ROUTER route categories).</summary>
public enum AiTaskType
{
    Script,
    Image,
    Video,
    Tts,
    Embedding,
    Research,
    Qa
}

/// <summary>
/// A candidate model/provider the router can choose from. Scores are 0..1; cost is per unit
/// (lower is better). Registered via configuration so no single provider is hard-coded.
/// </summary>
public sealed record ModelCandidate(
    string Provider,
    string Model,
    AiTaskType TaskType,
    double Quality,
    double Reliability,
    double LatencyScore,
    decimal UnitCostUsd,
    bool Available = true);

/// <summary>
/// Routing request with hard constraints (MODEL-ROUTER: hard constraints run before scoring).
/// </summary>
public sealed record RoutingRequest(
    AiTaskType TaskType,
    decimal? MaxUnitCostUsd = null,
    double MinQuality = 0);

/// <summary>Selected model plus the decision score and rationale.</summary>
public sealed record RoutingDecision(ModelCandidate Candidate, double Score, string Rationale);

public sealed record RoutingWeights(
    double Quality = 0.5,
    double Cost = 0.25,
    double Latency = 0.1,
    double Reliability = 0.15)
{
    public static RoutingWeights Default { get; } = new();
}

public interface IModelRouter
{
    /// <summary>
    /// Selects the best available candidate for the task, applying hard constraints
    /// (availability, task type, max cost, min quality) before weighted scoring. Returns
    /// null when no candidate satisfies the constraints.
    /// </summary>
    RoutingDecision? Route(RoutingRequest request, IReadOnlyList<ModelCandidate> candidates, RoutingWeights? weights = null);
}

public sealed class ModelRouter : IModelRouter
{
    public RoutingDecision? Route(RoutingRequest request, IReadOnlyList<ModelCandidate> candidates, RoutingWeights? weights = null)
    {
        weights ??= RoutingWeights.Default;

        // Hard constraints first.
        var eligible = candidates.Where(c =>
            c.Available &&
            c.TaskType == request.TaskType &&
            c.Quality >= request.MinQuality &&
            (request.MaxUnitCostUsd is null || c.UnitCostUsd <= request.MaxUnitCostUsd.Value))
            .ToList();

        if (eligible.Count == 0) return null;

        // Cost efficiency is relative to the cheapest eligible candidate.
        var minCost = eligible.Min(c => c.UnitCostUsd);

        RoutingDecision? best = null;
        foreach (var candidate in eligible)
        {
            var costEfficiency = candidate.UnitCostUsd <= 0
                ? 1.0
                : (double)(minCost <= 0 ? 1m : minCost / candidate.UnitCostUsd);

            var score =
                weights.Quality * candidate.Quality +
                weights.Cost * costEfficiency +
                weights.Latency * candidate.LatencyScore +
                weights.Reliability * candidate.Reliability;

            if (best is null || score > best.Score)
            {
                var rationale = $"quality={candidate.Quality:0.00}, costEff={costEfficiency:0.00}, latency={candidate.LatencyScore:0.00}, reliability={candidate.Reliability:0.00}";
                best = new RoutingDecision(candidate, Math.Round(score, 4), rationale);
            }
        }

        return best;
    }
}
