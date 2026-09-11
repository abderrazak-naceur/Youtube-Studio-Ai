namespace YoutubeStudio.Api.Services.Providers;

public sealed record ResearchRequest(string Prompt);
public sealed record ResearchResult(string Summary, IReadOnlyList<string> Sources);

public sealed record ScriptRequest(string Prompt, string ResearchSummary);
public sealed record ScriptResult(string Title, string Script);

public sealed record ScenePlanRequest(string Title, string Script);
public sealed record ScenePlanResult(IReadOnlyList<ScenePlanItem> Scenes);
public sealed record ScenePlanItem(int Number, string Narration, string VisualDirection, int DurationSeconds);

public sealed record VoiceRequest(string Script, string? VoiceId);
public sealed record VoiceResult(string ProviderAssetId, TimeSpan Duration);

public sealed record VisualRequest(string VisualDirection, int DurationSeconds);
public sealed record VisualResult(string ProviderAssetId, string MediaType);

public sealed record MusicSfxRequest(string Title, string Script, int DurationSeconds);
public sealed record MusicSfxResult(string ProviderAssetId, string MediaType);

public sealed record CaptionRequest(string Script);
public sealed record CaptionResult(string ProviderAssetId);

public sealed record RenderRequest(IReadOnlyList<string> AssetIds, string? MusicAssetId);
public sealed record RenderResult(string ProviderAssetId, TimeSpan Duration);

public sealed record QaRequest(string Title, string Script, string? RenderAssetId);
public sealed record QaResult(bool Passed, IReadOnlyList<string> Findings);

public interface IResearchProvider
{
    Task<ResearchResult> ResearchAsync(ResearchRequest request, CancellationToken cancellationToken);
}

public interface IScriptProvider
{
    Task<ScriptResult> GenerateScriptAsync(ScriptRequest request, CancellationToken cancellationToken);
}

public interface IScenePlanProvider
{
    Task<ScenePlanResult> CreateScenePlanAsync(ScenePlanRequest request, CancellationToken cancellationToken);
}

public interface IVoiceProvider
{
    Task<VoiceResult> GenerateVoiceAsync(VoiceRequest request, CancellationToken cancellationToken);
}

public interface IVisualProvider
{
    Task<VisualResult> GenerateVisualAsync(VisualRequest request, CancellationToken cancellationToken);
}

public interface IMusicSfxProvider
{
    Task<MusicSfxResult> GenerateMusicSfxAsync(MusicSfxRequest request, CancellationToken cancellationToken);
}

public interface ICaptionProvider
{
    Task<CaptionResult> GenerateCaptionsAsync(CaptionRequest request, CancellationToken cancellationToken);
}

public interface IRenderProvider
{
    Task<RenderResult> RenderAsync(RenderRequest request, CancellationToken cancellationToken);
}

public interface IQaProvider
{
    Task<QaResult> EvaluateAsync(QaRequest request, CancellationToken cancellationToken);
}
