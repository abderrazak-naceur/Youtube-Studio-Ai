using YoutubeStudio.Api.Services.Scoring;

namespace YoutubeStudio.Api.Tests;

public sealed class OpportunityScoringTests
{
    [Fact]
    public void Perfect_benefits_and_zero_costs_score_100()
    {
        var factors = new OpportunityFactors(
            AudienceDemand: 100, SearchIntent: 100, TrendMomentum: 100, Competition: 0,
            ChannelFit: 100, MonetizationPotential: 100, ProductionEffort: 0,
            OriginalityPotential: 100, EvidenceQuality: 100);

        var result = OpportunityScoring.Score(factors);

        Assert.Equal(100, result.OpportunityScore, 1);
        Assert.Equal(100, result.RevenueScore, 1);
    }

    [Fact]
    public void High_competition_and_effort_reduce_score()
    {
        var baseFactors = new OpportunityFactors(80, 80, 80, 10, 80, 80, 10, 80, 80);
        var costly = baseFactors with { Competition = 90, ProductionEffort = 90 };

        var baseScore = OpportunityScoring.Score(baseFactors).OpportunityScore;
        var costlyScore = OpportunityScoring.Score(costly).OpportunityScore;

        Assert.True(costlyScore < baseScore);
    }

    [Fact]
    public void Weights_change_the_result()
    {
        var factors = new OpportunityFactors(90, 10, 10, 10, 10, 10, 10, 10, 10);

        var demandHeavy = OpportunityScoring.Score(factors, new OpportunityWeights(AudienceDemand: 10));
        var demandLight = OpportunityScoring.Score(factors, new OpportunityWeights(AudienceDemand: 0.1));

        // Weighting audience demand (the only high factor) more heavily raises the score.
        Assert.True(demandHeavy.OpportunityScore > demandLight.OpportunityScore);
    }

    [Fact]
    public void Contributions_sum_to_opportunity_score()
    {
        var factors = new OpportunityFactors(70, 60, 50, 40, 80, 90, 30, 55, 65);
        var result = OpportunityScoring.Score(factors);

        Assert.Equal(result.OpportunityScore, Math.Round(result.Contributions.Values.Sum(), 2), 1);
    }

    [Fact]
    public void Factors_are_clamped_to_valid_range()
    {
        var factors = new OpportunityFactors(1000, -50, 50, 50, 50, 50, 50, 50, 50);
        var result = OpportunityScoring.Score(factors);

        Assert.InRange(result.OpportunityScore, 0, 100);
    }
}
