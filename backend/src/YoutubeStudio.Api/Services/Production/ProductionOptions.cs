namespace YoutubeStudio.Api.Services.Production;

/// <summary>
/// Reliability configuration for the production engine, bound from the "Production"
/// configuration section (PROJECT-STUDY §23: retry policies, dead-letter queues).
/// </summary>
public sealed class ProductionOptions
{
    public const string SectionName = "Production";

    /// <summary>
    /// Maximum number of attempts for a single production job before it is dead-lettered.
    /// A value of 3 means the initial attempt plus two retries.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;
}
