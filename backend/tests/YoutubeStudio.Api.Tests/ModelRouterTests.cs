using YoutubeStudio.Api.Services.Ai;

namespace YoutubeStudio.Api.Tests;

public sealed class ModelRouterTests
{
    private static readonly ModelRouter Router = new();

    [Fact]
    public void Quality_only_weighting_favors_premium_model()
    {
        var candidates = new[]
        {
            new ModelCandidate("prov-a", "cheap", AiTaskType.Script, Quality: 0.6, Reliability: 0.9, LatencyScore: 0.9, UnitCostUsd: 0.001m),
            new ModelCandidate("prov-b", "premium", AiTaskType.Script, Quality: 0.95, Reliability: 0.95, LatencyScore: 0.7, UnitCostUsd: 0.01m)
        };

        var qualityOnly = new RoutingWeights(Quality: 1.0, Cost: 0, Latency: 0, Reliability: 0);
        var decision = Router.Route(new RoutingRequest(AiTaskType.Script), candidates, qualityOnly);

        Assert.NotNull(decision);
        Assert.Equal("premium", decision!.Candidate.Model);
    }

    [Fact]
    public void Excludes_wrong_task_type()
    {
        var candidates = new[]
        {
            new ModelCandidate("prov-a", "img", AiTaskType.Image, 0.9, 0.9, 0.9, 0.001m)
        };

        Assert.Null(Router.Route(new RoutingRequest(AiTaskType.Script), candidates));
    }

    [Fact]
    public void Respects_max_cost_hard_constraint()
    {
        var candidates = new[]
        {
            new ModelCandidate("prov-a", "cheap", AiTaskType.Script, 0.6, 0.9, 0.9, 0.001m),
            new ModelCandidate("prov-b", "premium", AiTaskType.Script, 0.99, 0.99, 0.9, 0.05m)
        };

        var decision = Router.Route(new RoutingRequest(AiTaskType.Script, MaxUnitCostUsd: 0.005m), candidates);

        Assert.NotNull(decision);
        Assert.Equal("cheap", decision!.Candidate.Model);
    }

    [Fact]
    public void Excludes_unavailable_candidates()
    {
        var candidates = new[]
        {
            new ModelCandidate("prov-a", "down", AiTaskType.Script, 0.99, 0.99, 0.9, 0.001m, Available: false)
        };

        Assert.Null(Router.Route(new RoutingRequest(AiTaskType.Script), candidates));
    }

    [Fact]
    public void Respects_min_quality()
    {
        var candidates = new[]
        {
            new ModelCandidate("prov-a", "low", AiTaskType.Script, 0.4, 0.9, 0.9, 0.001m)
        };

        Assert.Null(Router.Route(new RoutingRequest(AiTaskType.Script, MinQuality: 0.8), candidates));
    }

    [Fact]
    public void Cost_weighting_can_favor_cheaper_model()
    {
        var candidates = new[]
        {
            new ModelCandidate("prov-a", "cheap", AiTaskType.Script, 0.7, 0.9, 0.9, 0.001m),
            new ModelCandidate("prov-b", "premium", AiTaskType.Script, 0.8, 0.9, 0.9, 0.02m)
        };

        var costFirst = new RoutingWeights(Quality: 0.1, Cost: 0.8, Latency: 0.05, Reliability: 0.05);
        var decision = Router.Route(new RoutingRequest(AiTaskType.Script), candidates, costFirst);

        Assert.Equal("cheap", decision!.Candidate.Model);
    }
}
