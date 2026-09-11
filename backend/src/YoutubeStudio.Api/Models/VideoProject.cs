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
    Completed,
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
    public Workspace Workspace { get; set; } = null!;
    public Channel? Channel { get; set; }
}
