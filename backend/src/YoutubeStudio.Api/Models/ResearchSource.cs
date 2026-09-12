namespace YoutubeStudio.Api.Models;

public sealed class ResearchSource : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid ResearchProjectId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
    public ResearchProject ResearchProject { get; set; } = null!;
}
