namespace YoutubeStudio.Api.Models;

public enum ExperimentStatus
{
    Draft,
    Running,
    Completed,
    Abandoned
}

/// <summary>
/// A controlled change to a single variable (title, thumbnail, hook, format, …) with a
/// hypothesis, baseline, success metric, observation window and decision rule
/// (DATA-MODEL Experiment, ANALYTICS-INTELLIGENCE experiments).
/// </summary>
public sealed class Experiment : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid? VideoProjectId { get; set; }

    /// <summary>The variable under test, e.g. "title", "thumbnail", "hook".</summary>
    public required string Variable { get; set; }

    public required string Hypothesis { get; set; }

    /// <summary>The metric used to decide the outcome, e.g. "ctr", "retention".</summary>
    public required string SuccessMetric { get; set; }

    public double BaselineValue { get; set; }

    /// <summary>Plain-language rule for deciding the winner.</summary>
    public string? DecisionRule { get; set; }

    public ExperimentStatus Status { get; set; } = ExperimentStatus.Draft;

    /// <summary>Recorded result value once observed (nullable until measured).</summary>
    public double? ResultValue { get; set; }

    public string? Outcome { get; set; }
}
