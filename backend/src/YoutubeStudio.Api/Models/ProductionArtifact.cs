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
    Thumbnail,
    Metadata,
    Render,
    Qa,
    Approval
}

public sealed class ProductionArtifact : Entity
{
    public Guid VideoProjectId { get; set; }
    public ProductionArtifactType Type { get; set; }

    /// <summary>
    /// 1-based version for this (project, type). Regenerating an artifact appends a new
    /// version rather than overwriting the previous one, so AI-generated artifacts are
    /// never silently overwritten (DATA-MODEL §3).
    /// </summary>
    public int Version { get; set; } = 1;

    public string ProviderAssetId { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? MetadataJson { get; set; }
    public VideoProject VideoProject { get; set; } = null!;
}
