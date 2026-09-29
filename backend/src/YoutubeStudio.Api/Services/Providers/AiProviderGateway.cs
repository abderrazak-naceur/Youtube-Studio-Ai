namespace YoutubeStudio.Api.Services.Providers;

/// <summary>
/// The set of AI-backed production tasks the model router can route. Kept in one place so
/// provider selection, configuration and observability all speak the same vocabulary
/// (PROJECT-STUDY §10 Model Router, §34 provider-agnostic AI gateway).
/// </summary>
public enum AiTask
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

/// <summary>
/// Marker implemented by every provider so the router can identify, enumerate and select
/// implementations by name instead of hard-coding a single vendor per task.
/// </summary>
public interface IAiProvider
{
    /// <summary>Stable provider name, e.g. "placeholder" or "openai".</summary>
    string ProviderName { get; }

    /// <summary>The production task this provider serves.</summary>
    AiTask SupportedTask { get; }
}

/// <summary>
/// Configuration for the AI provider layer, bound from the "AiProviders" configuration section.
/// Maps a task name to the preferred provider name. Missing entries fall back to the default provider.
/// </summary>
public sealed class AiProviderOptions
{
    public const string SectionName = "AiProviders";

    /// <summary>Provider used when a task has no explicit selection. Defaults to the deterministic placeholder.</summary>
    public string DefaultProvider { get; set; } = "placeholder";

    /// <summary>Per-task provider selection, keyed by <see cref="AiTask"/> name (case-insensitive).</summary>
    public Dictionary<string, string> Selections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>A resolved routing decision for a single task.</summary>
public sealed record ProviderSelection(AiTask Task, string ProviderName, bool IsFallback);

/// <summary>
/// Selects the provider to use for a given task and records the decision so routing is observable.
/// </summary>
public interface IModelRouter
{
    /// <summary>Returns the provider selected for the task, honouring configuration and falling back to the default.</summary>
    ProviderSelection Select(AiTask task);

    /// <summary>Resolves the concrete provider instance registered for the task's selected name.</summary>
    TProvider Resolve<TProvider>(AiTask task, IEnumerable<TProvider> registered) where TProvider : IAiProvider;
}
