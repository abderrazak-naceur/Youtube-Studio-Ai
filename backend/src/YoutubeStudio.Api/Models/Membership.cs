namespace YoutubeStudio.Api.Models;

/// <summary>
/// Workspace roles. Higher roles include the capabilities of lower ones for the checks
/// that matter in the MVP (Reviewer can approve; Owner can do everything).
/// </summary>
public enum WorkspaceRole
{
    /// <summary>Read/produce content but cannot approve or manage members.</summary>
    Editor = 0,

    /// <summary>Can approve/reject videos at the quality gate.</summary>
    Reviewer = 1,

    /// <summary>Full control of the workspace, including membership.</summary>
    Owner = 2
}

/// <summary>
/// Links a <see cref="User"/> to a <see cref="Workspace"/> with a role, and is the
/// enforcement point for tenant isolation (DATA-MODEL §6, SECURITY §1).
/// </summary>
public sealed class Membership : Entity
{
    public Guid UserId { get; set; }
    public Guid WorkspaceId { get; set; }
    public WorkspaceRole Role { get; set; } = WorkspaceRole.Editor;

    public User User { get; set; } = null!;
    public Workspace Workspace { get; set; } = null!;
}
