namespace YoutubeStudio.Api.Models;

/// <summary>
/// An immutable audit record for a sensitive action (approvals, rejections, permission
/// or credential changes, spending, publishing). Required by SECURITY-COMPLIANCE §8 and
/// PRODUCT-REQUIREMENTS (audit trail, P0). Records are append-only and never mutated.
/// </summary>
public sealed class AuditEvent : Entity
{
    public Guid? WorkspaceId { get; set; }
    public Guid? VideoProjectId { get; set; }

    /// <summary>Stable action identifier, e.g. "video.approved", "video.rejected".</summary>
    public required string Action { get; set; }

    /// <summary>Who performed the action (reviewer/user identity).</summary>
    public required string Actor { get; set; }

    /// <summary>Structured JSON details for the action.</summary>
    public string? DetailsJson { get; set; }
}
