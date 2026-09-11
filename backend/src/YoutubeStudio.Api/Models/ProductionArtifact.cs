namespace YoutubeStudio.Api.Models;

public enum ProductionArtifactType
{
    Research,
    Script,
    ScenePlan,
    Voice,
    Visual,
    MusicSfx,
    Captions,
    Render,
    Qa
}

public sealed class ProductionArtifact : Entity
{
    public Guid VideoProjectId { get; set; }
    public ProductionArtifactType Type { get; set; }
    public string ProviderAssetId { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? MetadataJson { get; set; }
    public VideoProject VideoProject { get; set; } = null!;
}
