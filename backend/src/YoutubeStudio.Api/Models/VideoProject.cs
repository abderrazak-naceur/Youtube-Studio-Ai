namespace YoutubeStudio.Api.Models;

public enum VideoProjectStatus
{
    Draft,
    Researching,
    Scripted,
    Planned,
    Producing,
    Rendering,
    Qa,
    AwaitingApproval,
    Completed,
    Rejected,
    Cancelled,
    Failed
}

public sealed class VideoProject : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid? ChannelId { get; set; }
    public required string Prompt { get; set; }
    public VideoProjectStatus Status { get; set; } = VideoProjectStatus.Draft;
    public string? Title { get; set; }
    public string? Script { get; set; }

    /// <summary>
    /// Whether the produced video contains AI-generated/synthetic content that requires a
    /// platform AI disclosure (SECURITY-COMPLIANCE §5). Defaults to true because the MVP
    /// pipeline generates synthetic media. Approval requires this disclosure to be
    /// acknowledged before final export.
    /// </summary>
    public bool RequiresAiDisclosure { get; set; } = true;

    public Workspace Workspace { get; set; } = null!;
    public Channel? Channel { get; set; }
}
