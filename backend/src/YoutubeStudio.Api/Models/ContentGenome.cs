namespace YoutubeStudio.Api.Models;

/// <summary>
/// Structured attributes extracted from a completed video so every published result
/// leaves learning data behind (backlog US-023). One genome per video project.
/// </summary>
public sealed class ContentGenome : Entity
{
    public Guid VideoProjectId { get; set; }

    /// <summary>The resolved title of the produced video.</summary>
    public required string Title { get; set; }

    /// <summary>Total planned/rendered duration in seconds.</summary>
    public int DurationSeconds { get; set; }

    /// <summary>Number of scenes in the produced scene plan.</summary>
    public int SceneCount { get; set; }

    /// <summary>Approximate script word count.</summary>
    public int WordCount { get; set; }

    /// <summary>JSON array of extracted topic/keyword attributes.</summary>
    public required string AttributesJson { get; set; }

    public VideoProject VideoProject { get; set; } = null!;
}
