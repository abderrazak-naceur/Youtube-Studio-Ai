using Microsoft.AspNetCore.Mvc;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Read-only view of how the model router is currently wired: which provider each
/// production task routes to, and whether that is an explicit selection or the default
/// fallback. Makes the AI provider layer observable (PROJECT-STUDY §10, §23).
/// </summary>
[ApiController]
[Route("api/v1/ai-providers")]
public sealed class AiProvidersController(IModelRouter router) : ControllerBase
{
    [HttpGet]
    public ActionResult<AiProviderRoutingResponse> Get()
    {
        var routes = Enum.GetValues<AiTask>()
            .Select(task =>
            {
                var selection = router.Select(task);
                return new AiProviderRoute(task.ToString(), selection.ProviderName, selection.IsFallback);
            })
            .ToList();
        return Ok(new AiProviderRoutingResponse(routes));
    }
}

public sealed record AiProviderRoute(string Task, string Provider, bool IsFallback);
public sealed record AiProviderRoutingResponse(IReadOnlyList<AiProviderRoute> Routes);
