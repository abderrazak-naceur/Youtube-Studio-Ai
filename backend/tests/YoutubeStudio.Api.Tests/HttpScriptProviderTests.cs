using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using YoutubeStudio.Api.Services.Resilience;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class HttpScriptProviderTests
{
    [Fact]
    public async Task Falls_back_to_placeholder_when_not_configured()
    {
        var options = new ScriptProviderOptions(); // no endpoint/key
        var provider = new HttpScriptProvider(
            new StubHttpClientFactory(_ => throw new InvalidOperationException("should not call http")),
            options, new PlaceholderScriptProvider(), new CircuitBreaker(), NullLogger<HttpScriptProvider>.Instance);

        var result = await provider.GenerateScriptAsync(new ScriptRequest("AI for creators", "research"), CancellationToken.None);

        Assert.Contains("HOOK", result.Script); // placeholder shape
        Assert.False(string.IsNullOrWhiteSpace(result.Title));
    }

    [Fact]
    public async Task Uses_http_response_when_configured()
    {
        var options = new ScriptProviderOptions { Endpoint = "https://api.example.com/v1/chat/completions", ApiKey = "k", Model = "test" };
        const string json = """
        {"choices":[{"message":{"content":"HOOK: Real generated script from the model."}}]}
        """;
        var provider = new HttpScriptProvider(
            new StubHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") }),
            options, new PlaceholderScriptProvider(), new CircuitBreaker(), NullLogger<HttpScriptProvider>.Instance);

        var result = await provider.GenerateScriptAsync(new ScriptRequest("AI for creators", "research"), CancellationToken.None);

        Assert.Equal("HOOK: Real generated script from the model.", result.Script);
    }

    [Fact]
    public async Task Falls_back_when_http_call_fails()
    {
        var options = new ScriptProviderOptions { Endpoint = "https://api.example.com/v1/chat/completions", ApiKey = "k" };
        var provider = new HttpScriptProvider(
            new StubHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)),
            options, new PlaceholderScriptProvider(), new CircuitBreaker(), NullLogger<HttpScriptProvider>.Instance);

        var result = await provider.GenerateScriptAsync(new ScriptRequest("AI for creators", "research"), CancellationToken.None);

        Assert.Contains("HOOK", result.Script); // fell back to placeholder
    }

    private sealed class StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> responder) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(responder));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
