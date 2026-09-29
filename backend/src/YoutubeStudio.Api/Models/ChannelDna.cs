namespace YoutubeStudio.Api.Models;

/// <summary>
/// Versioned editorial identity for a channel (DATA-MODEL Channel DNA, backlog US-005):
/// audience, positioning, tone, visual language, recurring formats, forbidden patterns and
/// strategic goals. Each save appends a new version; prior versions are never overwritten.
/// </summary>
public sealed class ChannelDna : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid ChannelId { get; set; }

    /// <summary>1-based version for this channel's DNA.</summary>
    public int Version { get; set; } = 1;

    public string? Audience { get; set; }
    public string? Positioning { get; set; }
    public string? Tone { get; set; }
    public string? VisualLanguage { get; set; }

    /// <summary>Recurring content formats (free text or JSON list).</summary>
    public string? RecurringFormats { get; set; }

    /// <summary>Patterns/behaviours to avoid.</summary>
    public string? ForbiddenPatterns { get; set; }

    public string? StrategicGoals { get; set; }

    public Channel Channel { get; set; } = null!;
}
