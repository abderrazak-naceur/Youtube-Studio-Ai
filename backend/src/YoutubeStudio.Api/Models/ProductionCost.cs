namespace YoutubeStudio.Api.Models;

/// <summary>
/// A single recorded provider/rendering cost for one production stage of a video project.
/// Every provider call that has a measurable cost leaves one row so cost per video and
/// cost per provider can be computed (backlog US-022).
/// </summary>
public sealed class ProductionCost : Entity
{
    public Guid VideoProjectId { get; set; }

    /// <summary>The production stage that incurred the cost (e.g. "Voice", "Render").</summary>
    public required string Stage { get; set; }

    /// <summary>The provider that produced the cost (e.g. "placeholder-voice").</summary>
    public required string Provider { get; set; }

    /// <summary>Billable units consumed (seconds, tokens, images, etc.).</summary>
    public decimal Units { get; set; }

    /// <summary>Cost per unit in USD.</summary>
    public decimal UnitCostUsd { get; set; }

    /// <summary>Total cost in USD for this stage.</summary>
    public decimal TotalCostUsd { get; set; }

    public VideoProject VideoProject { get; set; } = null!;
}
