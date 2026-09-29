namespace YoutubeStudio.Api.Models;

public sealed class ContentDraft : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid ResearchProjectId { get; set; }

    /// <summary>Editorial angle: the unique take on the topic.</summary>
    public string Angle { get; set; } = string.Empty;

    /// <summary>Opening hook designed to earn the first seconds of attention.</summary>
    public string Hook { get; set; } = string.Empty;

    /// <summary>Section outline as newline-separated beats.</summary>
    public string Outline { get; set; } = string.Empty;

    /// <summary>Narration script derived from the verified claims.</summary>
    public string Script { get; set; } = string.Empty;

    /// <summary>Candidate titles as a JSON array.</summary>
    public string TitleCandidatesJson { get; set; } = "[]";

    /// <summary>Thumbnail concepts as a JSON array.</summary>
    public string ThumbnailConceptsJson { get; set; } = "[]";

    /// <summary>Video description text.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Chapter markers as a JSON array.</summary>
    public string ChaptersJson { get; set; } = "[]";

    /// <summary>SEO/metadata tags as a JSON array.</summary>
    public string TagsJson { get; set; } = "[]";

    /// <summary>Draft status: draft or ready.</summary>
    public string Status { get; set; } = "draft";

    public ResearchProject ResearchProject { get; set; } = null!;
}
