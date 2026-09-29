using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class AiProvidersControllerTests
{
    private static AiProvidersController CreateController(AiProviderOptions options) =>
        new(new ModelRouter(Options.Create(options), NullLogger<ModelRouter>.Instance));

    [Fact]
    public void Get_returns_a_route_for_every_task_defaulting_to_the_fallback_provider()
    {
        var controller = CreateController(new AiProviderOptions { DefaultProvider = "placeholder" });

        var response = Assert.IsType<AiProviderRoutingResponse>(Assert.IsType<OkObjectResult>(controller.Get().Result).Value);

        Assert.Equal(Enum.GetValues<AiTask>().Length, response.Routes.Count);
        Assert.All(response.Routes, route =>
        {
            Assert.Equal("placeholder", route.Provider);
            Assert.True(route.IsFallback);
        });
    }

    [Fact]
    public void Get_reflects_an_explicit_provider_selection()
    {
        var options = new AiProviderOptions { DefaultProvider = "placeholder" };
        options.Selections["Voice"] = "elevenlabs";
        var controller = CreateController(options);

        var response = Assert.IsType<AiProviderRoutingResponse>(Assert.IsType<OkObjectResult>(controller.Get().Result).Value);

        var voice = Assert.Single(response.Routes, route => route.Task == "Voice");
        Assert.Equal("elevenlabs", voice.Provider);
        Assert.False(voice.IsFallback);
    }
}
