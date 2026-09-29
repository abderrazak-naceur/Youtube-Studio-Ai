namespace YoutubeStudio.Api.Services.Scoring;

/// <summary>
/// Factor inputs for opportunity scoring, each 0..100 (CONTENT-ENGINE opportunity score).
/// </summary>
public sealed record OpportunityFactors(
    double AudienceDemand,
    double SearchIntent,
    double TrendMomentum,
    double Competition,
    double ChannelFit,
    double MonetizationPotential,
    double ProductionEffort,
    double OriginalityPotential,
    double EvidenceQuality);

/// <summary>
/// Configurable weights per factor. Weights are relative; they are normalized so the
/// result stays on a 0..100 scale regardless of the values supplied (UX-UI: user-adjustable
/// without editing prompts).
/// </summary>
public sealed record OpportunityWeights(
    double AudienceDemand = 1.0,
    double SearchIntent = 1.0,
    double TrendMomentum = 0.8,
    double Competition = 1.0,
    double ChannelFit = 1.2,
    double MonetizationPotential = 1.5,
    double ProductionEffort = 0.8,
    double OriginalityPotential = 1.0,
    double EvidenceQuality = 1.0)
{
    public static OpportunityWeights Default { get; } = new();
}

public sealed record OpportunityScoreResult(
    double OpportunityScore,
    double RevenueScore,
    IReadOnlyDictionary<string, double> Contributions);

/// <summary>
/// Pure, deterministic opportunity scoring. Competition and production effort are treated
/// as costs (higher raw value reduces the score); the others are benefits.
/// </summary>
public static class OpportunityScoring
{
    public static OpportunityScoreResult Score(OpportunityFactors factors, OpportunityWeights? weights = null)
    {
        weights ??= OpportunityWeights.Default;

        // Cost factors are inverted so a high competition / effort lowers the score.
        var terms = new (string Name, double Value, double Weight)[]
        {
            ("audienceDemand", Clamp(factors.AudienceDemand), weights.AudienceDemand),
            ("searchIntent", Clamp(factors.SearchIntent), weights.SearchIntent),
            ("trendMomentum", Clamp(factors.TrendMomentum), weights.TrendMomentum),
            ("competition", 100 - Clamp(factors.Competition), weights.Competition),
            ("channelFit", Clamp(factors.ChannelFit), weights.ChannelFit),
            ("monetizationPotential", Clamp(factors.MonetizationPotential), weights.MonetizationPotential),
            ("productionEffort", 100 - Clamp(factors.ProductionEffort), weights.ProductionEffort),
            ("originalityPotential", Clamp(factors.OriginalityPotential), weights.OriginalityPotential),
            ("evidenceQuality", Clamp(factors.EvidenceQuality), weights.EvidenceQuality)
        };

        var totalWeight = terms.Sum(t => Math.Max(0, t.Weight));
        if (totalWeight <= 0) totalWeight = 1;

        var contributions = terms.ToDictionary(
            t => t.Name,
            t => Math.Round(t.Value * Math.Max(0, t.Weight) / totalWeight, 4));

        var opportunityScore = Math.Round(contributions.Values.Sum(), 2);

        // Revenue score emphasises monetization and audience demand.
        var revenueScore = Math.Round(
            0.6 * Clamp(factors.MonetizationPotential) + 0.4 * Clamp(factors.AudienceDemand), 2);

        return new OpportunityScoreResult(opportunityScore, revenueScore, contributions);
    }

    private static double Clamp(double value) => Math.Clamp(value, 0, 100);
}
