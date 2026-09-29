using Microsoft.Extensions.Options;

namespace YoutubeStudio.Api.Services.Providers;

/// <summary>
/// Configuration-driven model router. Chooses the provider for each task from the
/// "AiProviders" options, falling back to the default provider when no valid selection
/// exists. Every decision is logged so provider routing stays observable.
/// </summary>
public sealed class ModelRouter(IOptions<AiProviderOptions> options, ILogger<ModelRouter> logger) : IModelRouter
{
    private readonly AiProviderOptions _options = options.Value;

    public ProviderSelection Select(AiTask task)
    {
        var configured = _options.Selections.TryGetValue(task.ToString(), out var name) && !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : null;

        var isFallback = configured is null;
        var provider = configured ?? _options.DefaultProvider;
        return new ProviderSelection(task, provider, isFallback);
    }

    public TProvider Resolve<TProvider>(AiTask task, IEnumerable<TProvider> registered) where TProvider : IAiProvider
    {
        var candidates = registered.Where(p => p.SupportedTask == task).ToList();
        if (candidates.Count == 0)
            throw new InvalidOperationException($"No provider is registered for task '{task}'.");

        var selection = Select(task);
        var match = candidates.FirstOrDefault(p => string.Equals(p.ProviderName, selection.ProviderName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            var fallback = candidates.FirstOrDefault(p => string.Equals(p.ProviderName, _options.DefaultProvider, StringComparison.OrdinalIgnoreCase))
                ?? candidates[0];
            logger.LogWarning(
                "No provider named '{Selected}' is registered for task {Task}; falling back to '{Fallback}'.",
                selection.ProviderName, task, fallback.ProviderName);
            return fallback;
        }

        logger.LogInformation(
            "Routing task {Task} to provider '{Provider}'{Fallback}.",
            task, match.ProviderName, selection.IsFallback ? " (default)" : string.Empty);
        return match;
    }
}
