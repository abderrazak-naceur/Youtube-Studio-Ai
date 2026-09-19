namespace YoutubeStudio.Api.Models;

public sealed class ResearchBrief : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid ResearchProjectId { get; set; }
    public string Markdown { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public int PendingClaimCount { get; set; }
    public ResearchProject ResearchProject { get; set; } = null!;
}
