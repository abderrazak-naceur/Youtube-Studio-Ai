namespace YoutubeStudio.Api.Models;

/// <summary>
/// An immutable domain event (DATA-MODEL §4, EVENT-MODEL). Events support async workflows,
/// auditability and learning. Records are append-only and never mutated; there are no FKs
/// so events survive aggregate deletion.
/// </summary>
public sealed class DomainEvent : Entity
{
    /// <summary>Event type, e.g. "opportunity.created", "video.approved", "quality_gate.failed".</summary>
    public required string Type { get; set; }

    public Guid? WorkspaceId { get; set; }

    /// <summary>The id of the aggregate the event is about (opportunity, video project, etc.).</summary>
    public Guid? AggregateId { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public int SchemaVersion { get; set; } = 1;

    /// <summary>Structured JSON payload for the event.</summary>
    public string? Payload { get; set; }
}
