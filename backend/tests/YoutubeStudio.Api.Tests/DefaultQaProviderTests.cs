using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class DefaultQaProviderTests
{
    private static readonly string ValidScript = string.Join(' ',
        Enumerable.Range(0, 40).Select(i => $"sentence{i % 12}"));

    [Fact]
    public async Task Passes_when_all_gates_satisfied()
    {
        var result = await new DefaultQaProvider().EvaluateAsync(ValidRequest(), CancellationToken.None);

        Assert.True(result.Passed);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task Fails_when_render_missing()
    {
        var result = await new DefaultQaProvider().EvaluateAsync(ValidRequest() with { RenderAssetId = "" }, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains("Missing rendered video asset.", result.Findings);
    }

    [Fact]
    public async Task Fails_when_captions_missing()
    {
        var result = await new DefaultQaProvider().EvaluateAsync(ValidRequest() with { HasCaptions = false }, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains("Captions are missing.", result.Findings);
    }

    [Fact]
    public async Task Fails_when_metadata_or_thumbnail_missing()
    {
        var result = await new DefaultQaProvider().EvaluateAsync(
            ValidRequest() with { HasThumbnail = false, HasMetadata = false }, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains("Thumbnail is missing.", result.Findings);
        Assert.Contains("Publish metadata is missing.", result.Findings);
    }

    [Fact]
    public async Task Fails_when_script_too_short()
    {
        var result = await new DefaultQaProvider().EvaluateAsync(
            ValidRequest() with { Script = "too short script" }, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains("Script is too short to be a coherent narrative.", result.Findings);
    }

    [Fact]
    public async Task Fails_originality_heuristic_on_repetitive_script()
    {
        var repetitive = string.Join(' ', Enumerable.Repeat("spammy", 40));
        var result = await new DefaultQaProvider().EvaluateAsync(
            ValidRequest() with { Script = repetitive }, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains("Script fails the originality heuristic (excessive word repetition).", result.Findings);
    }

    [Fact]
    public async Task Fails_when_no_scenes()
    {
        var result = await new DefaultQaProvider().EvaluateAsync(ValidRequest() with { SceneCount = 0 }, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains("Scene plan has no scenes.", result.Findings);
    }

    private static QaRequest ValidRequest() =>
        new("AI Explained", ValidScript, "render-1", SceneCount: 3, VoiceDurationSeconds: 28, RenderDurationSeconds: 28,
            HasCaptions: true, HasThumbnail: true, HasMetadata: true);
}
