using System.Net.Http.Json;
using System.Text.Json;

namespace YoutubeStudio.Api.Services.Providers;

/// <summary>
/// Configuration for the real LLM-backed script provider. Left empty by default so the
/// MVP falls back to the placeholder; credentials live in server-side config/secrets only
/// (AUTHENTICATION.md, SECURITY §1).
/// </summary>
public sealed class ScriptProviderOptions
{
    public const string SectionName = "ScriptProvider";
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey);
}

/// <summary>
/// Script provider that calls an OpenAI-compatible chat completions endpoint when configured,
/// and otherwise delegates to the placeholder. This is the first concrete real-provider
/// adapter behind the existing <see cref="IScriptProvider"/> boundary (PROVIDER-INTEGRATIONS).
/// </summary>
public sealed class HttpScriptProvider(
    IHttpClientFactory httpClientFactory,
    ScriptProviderOptions options,
    PlaceholderScriptProvider fallback,
    ILogger<HttpScriptProvider> logger) : IScriptProvider
{
    public async Task<ScriptResult> GenerateScriptAsync(ScriptRequest request, CancellationToken cancellationToken)
    {
        if (!options.IsConfigured)
            return await fallback.GenerateScriptAsync(request, cancellationToken);

        try
        {
            var client = httpClientFactory.CreateClient("script-provider");
            client.DefaultRequestHeaders.Authorization = new("Bearer", options.ApiKey);

            var payload = new
            {
                model = options.Model,
                messages = new object[]
                {
                    new { role = "system", content = "You write concise, original YouTube video scripts. Respond with the script text only." },
                    new { role = "user", content = $"Prompt: {request.Prompt}\n\nResearch: {request.ResearchSummary}" }
                }
            };

            using var response = await client.PostAsJsonAsync(options.Endpoint, payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            var content = body
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
                return await fallback.GenerateScriptAsync(request, cancellationToken);

            var title = request.Prompt.Trim().Length <= 120
                ? request.Prompt.Trim()
                : request.Prompt.Trim()[..120].TrimEnd() + "…";
            return new ScriptResult(title, content.Trim());
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException or InvalidOperationException)
        {
            // A provider failure must not break production: fall back to the placeholder.
            logger.LogWarning(exception, "HTTP script provider failed; falling back to placeholder.");
            return await fallback.GenerateScriptAsync(request, cancellationToken);
        }
    }
}
