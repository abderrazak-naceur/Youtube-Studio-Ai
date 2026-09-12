using System.Threading;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class MusicSfxProviderTests
{
    [Fact]
    public async Task Placeholder_provider_returns_replaceable_audio_asset()
    {
        var provider = new PlaceholderMusicSfxProvider();

        var result = await provider.GenerateMusicSfxAsync(
            new MusicSfxRequest("Test video", "A short script.", 30),
            CancellationToken.None);

        Assert.Equal("placeholder-music-sfx", result.ProviderAssetId);
        Assert.Equal("placeholder-audio", result.MediaType);
    }
}
