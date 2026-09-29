namespace YoutubeStudio.Api.Models;

/// <summary>
/// A recorded performance snapshot for a published video, connecting outcomes back to
/// content decisions (backlog US-024). Multiple snapshots can be recorded over time.
/// </summary>
public sealed class VideoOutcome : Entity
{
    public Guid VideoProjectId { get; set; }

    /// <summary>Where the metrics came from (e.g. "manual", "youtube-import").</summary>
    public required string Source { get; set; }

    /// <summary>The date the metrics represent (UTC).</summary>
    public DateTime MeasuredAtUtc { get; set; }

    public long Views { get; set; }
    public long Likes { get; set; }
    public long Comments { get; set; }

    /// <summary>Average view duration in seconds.</summary>
    public double AverageViewDurationSeconds { get; set; }

    /// <summary>Estimated revenue in USD attributed to this snapshot.</summary>
    public decimal EstimatedRevenueUsd { get; set; }

    public VideoProject VideoProject { get; set; } = null!;
}
