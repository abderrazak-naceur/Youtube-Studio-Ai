namespace YoutubeStudio.Api.Models;

/// <summary>
/// Revenue attributed to a content asset or channel (DATA-MODEL Revenue Event,
/// REVENUE-ENGINE attribution: channel → video → cluster → CTA → conversion → revenue).
/// Append-only; attribution may be approximate and its confidence is recorded.
/// </summary>
public sealed class RevenueEvent : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid? VideoProjectId { get; set; }
    public Guid? ChannelId { get; set; }

    /// <summary>Revenue stream, e.g. "ads", "affiliate", "sponsorship", "product".</summary>
    public required string Source { get; set; }

    public decimal AmountUsd { get; set; }

    /// <summary>Optional CTA the revenue is attributed to.</summary>
    public string? Cta { get; set; }

    /// <summary>Attribution confidence 0..1 (exposes uncertainty per REVENUE-ENGINE).</summary>
    public double AttributionConfidence { get; set; } = 1.0;

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
