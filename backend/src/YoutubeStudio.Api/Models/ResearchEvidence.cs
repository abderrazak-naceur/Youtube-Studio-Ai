namespace YoutubeStudio.Api.Models;

public sealed class ResearchEvidence : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid ResearchSourceId { get; set; }
    public string Quote { get; set; } = string.Empty;
    public string? Locator { get; set; }
    public string? Context { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public ResearchSource ResearchSource { get; set; } = null!;
    public ICollection<ResearchClaimEvidence> ClaimLinks { get; set; } = [];
}
