using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class ModelRouterTests
{
    private sealed record FakeProvider(string ProviderName, AiTask SupportedTask) : IAiProvider;

    private static ModelRouter CreateRouter(AiProviderOptions options) =>
        new(Options.Create(options), NullLogger<ModelRouter>.Instance);

    [Fact]
    public void Select_falls_back_to_the_default_provider_when_no_selection_is_configured()
    {
        var router = CreateRouter(new AiProviderOptions { DefaultProvider = "placeholder" });

        var selection = router.Select(AiTask.Script);

        Assert.Equal(AiTask.Script, selection.Task);
        Assert.Equal("placeholder", selection.ProviderName);
        Assert.True(selection.IsFallback);
    }

    [Fact]
    public void Select_honours_a_configured_provider_for_a_task()
    {
        var options = new AiProviderOptions { DefaultProvider = "placeholder" };
        options.Selections["Script"] = "openai";
        var router = CreateRouter(options);

        var selection = router.Select(AiTask.Script);

        Assert.Equal("openai", selection.ProviderName);
        Assert.False(selection.IsFallback);
    }

    [Fact]
    public void Select_is_case_insensitive_on_the_task_key()
    {
        var options = new AiProviderOptions();
        options.Selections["sCrIpT"] = "anthropic";
        var router = CreateRouter(options);

        Assert.Equal("anthropic", router.Select(AiTask.Script).ProviderName);
    }

    [Fact]
    public void Resolve_returns_the_configured_provider_instance()
    {
        var options = new AiProviderOptions { DefaultProvider = "placeholder" };
        options.Selections["Voice"] = "elevenlabs";
        var router = CreateRouter(options);
        var providers = new IAiProvider[]
        {
            new FakeProvider("placeholder", AiTask.Voice),
            new FakeProvider("elevenlabs", AiTask.Voice)
        };

        var resolved = router.Resolve(AiTask.Voice, providers);

        Assert.Equal("elevenlabs", resolved.ProviderName);
    }

    [Fact]
    public void Resolve_falls_back_to_default_when_the_configured_provider_is_not_registered()
    {
        var options = new AiProviderOptions { DefaultProvider = "placeholder" };
        options.Selections["Voice"] = "not-registered";
        var router = CreateRouter(options);
        var providers = new IAiProvider[]
        {
            new FakeProvider("placeholder", AiTask.Voice)
        };

        var resolved = router.Resolve(AiTask.Voice, providers);

        Assert.Equal("placeholder", resolved.ProviderName);
    }

    [Fact]
    public void Resolve_only_considers_providers_for_the_requested_task()
    {
        var router = CreateRouter(new AiProviderOptions { DefaultProvider = "placeholder" });
        var providers = new IAiProvider[]
        {
            new FakeProvider("placeholder", AiTask.Render),
            new FakeProvider("placeholder", AiTask.Qa)
        };

        var resolved = router.Resolve(AiTask.Qa, providers);

        Assert.Equal(AiTask.Qa, resolved.SupportedTask);
    }

    [Fact]
    public void Resolve_throws_when_no_provider_is_registered_for_the_task()
    {
        var router = CreateRouter(new AiProviderOptions());

        Assert.Throws<InvalidOperationException>(() => router.Resolve(AiTask.Render, Array.Empty<IAiProvider>()));
    }

    [Fact]
    public void Placeholder_providers_declare_their_name_and_task()
    {
        Assert.Equal(AiTask.Research, new PlaceholderResearchProvider().SupportedTask);
        Assert.Equal(AiTask.Qa, new PlaceholderQaProvider().SupportedTask);
        Assert.Equal("placeholder", new PlaceholderVoiceProvider().ProviderName);
    }
}
