namespace YoutubeStudio.Api.Production;

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
    Task<VoiceResult> SynthesizeAsync(VoiceRequest request, CancellationToken cancellationToken);
}

public interface IVisualProvider
{
    Task<VisualResult> GenerateAsync(VisualRequest request, CancellationToken cancellationToken);
}

public interface ICaptionProvider
{
    Task<CaptionResult> GenerateAsync(CaptionRequest request, CancellationToken cancellationToken);
}

public interface IRenderProvider
{
    Task<RenderResult> RenderAsync(RenderRequest request, CancellationToken cancellationToken);
}

public interface IQAProvider
{
    Task<QAResult> EvaluateAsync(QARequest request, CancellationToken cancellationToken);
}

public sealed record ScriptRequest(string Prompt, string? ResearchContext);
public sealed record ScriptResult(string Title, string Script);
public sealed record ScenePlanRequest(string Script);
public sealed record ScenePlanResult(IReadOnlyList<ScenePlanItem> Scenes);
public sealed record ScenePlanItem(int Number, string Narration, string VisualDirection);
public sealed record VoiceRequest(string Script, string VoiceId);
public sealed record VoiceResult(string AssetUri, TimeSpan Duration);
public sealed record VisualRequest(IReadOnlyList<ScenePlanItem> Scenes);
public sealed record VisualResult(IReadOnlyList<string> AssetUris);
public sealed record CaptionRequest(string AudioAssetUri, string Script);
public sealed record CaptionResult(string AssetUri);
public sealed record RenderRequest(IReadOnlyList<string> AssetUris, string VoiceAssetUri, string? CaptionAssetUri);
public sealed record RenderResult(string VideoUri, TimeSpan Duration);
public sealed record QARequest(string VideoUri, string Script);
public sealed record QAResult(bool Passed, IReadOnlyList<string> Findings);
