namespace YoutubeStudio.Api.Services.Providers;

public sealed record ResearchRequest(string Prompt);
public sealed record ResearchResult(string Summary, IReadOnlyList<string> Sources);

public sealed record ScriptRequest(string Prompt, string ResearchSummary);
public sealed record ScriptResult(string Title, string Script);

public sealed record ScenePlanRequest(string Title, string Script);
public sealed record ScenePlanResult(IReadOnlyList<ScenePlanItem> Scenes);
public sealed record ScenePlanItem(int Number, string Narration, string VisualDirection, int DurationSeconds);

public sealed record VoiceRequest(string Script, string? VoiceId);
public sealed record VoiceResult(string ProviderAssetId, string MediaType, TimeSpan Duration);

public sealed record VisualRequest(string VisualDirection, int DurationSeconds);
public sealed record VisualResult(string ProviderAssetId, string MediaType);

public sealed record MusicSfxRequest(string Title, string Script, int DurationSeconds);
public sealed record MusicSfxResult(string ProviderAssetId, string MediaType);

public sealed record CaptionRequest(string Script);
public sealed record CaptionEntry(double StartSeconds, double EndSeconds, string Text);
public sealed record CaptionResult(string ProviderAssetId, IReadOnlyList<CaptionEntry> Entries);

public sealed record RenderRequest(IReadOnlyList<string> AssetIds, string? MusicAssetId);
public sealed record RenderResult(string ProviderAssetId, TimeSpan Duration);

public sealed record QaRequest(string Title, string Script, string? RenderAssetId);
public sealed record QaResult(bool Passed, IReadOnlyList<string> Findings);

public interface IResearchProvider : IAiProvider
{
    Task<ResearchResult> ResearchAsync(ResearchRequest request, CancellationToken cancellationToken);
}

public interface IScriptProvider : IAiProvider
{
    Task<ScriptResult> GenerateScriptAsync(ScriptRequest request, CancellationToken cancellationToken);
}

public interface IScenePlanProvider : IAiProvider
{
    Task<ScenePlanResult> CreateScenePlanAsync(ScenePlanRequest request, CancellationToken cancellationToken);
}

public interface IVoiceProvider : IAiProvider
{
    Task<VoiceResult> GenerateVoiceAsync(VoiceRequest request, CancellationToken cancellationToken);
}

public interface IVisualProvider : IAiProvider
{
    Task<VisualResult> GenerateVisualAsync(VisualRequest request, CancellationToken cancellationToken);
}

public interface IMusicSfxProvider : IAiProvider
{
    Task<MusicSfxResult> GenerateMusicSfxAsync(MusicSfxRequest request, CancellationToken cancellationToken);
}

public interface ICaptionProvider : IAiProvider
{
    Task<CaptionResult> GenerateCaptionsAsync(CaptionRequest request, CancellationToken cancellationToken);
}

public interface IRenderProvider : IAiProvider
{
    Task<RenderResult> RenderAsync(RenderRequest request, CancellationToken cancellationToken);
}

public interface IQaProvider : IAiProvider
{
    Task<QaResult> EvaluateAsync(QaRequest request, CancellationToken cancellationToken);
}
