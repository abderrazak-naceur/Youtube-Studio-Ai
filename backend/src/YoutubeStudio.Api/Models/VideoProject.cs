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

    /// <summary>The content draft this project was seeded from, when it originated from the research→content pipeline.</summary>
    public Guid? ContentDraftId { get; set; }

    /// <summary>Origin of the project: "prompt" for a raw idea, "content_draft" when seeded from an approved draft.</summary>
    public string Source { get; set; } = "prompt";

    public Workspace Workspace { get; set; } = null!;
    public Channel? Channel { get; set; }
}
