using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Exposes the structured attributes extracted from a completed video (backlog US-023).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/video-projects/{videoProjectId:guid}/content-genome")]
public sealed class ContentGenomeController(YoutubeStudioDbContext db, IWorkspaceAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ContentGenomeResponse>> Get(Guid videoProjectId, CancellationToken cancellationToken)
    {
        var workspaceId = await db.VideoProjects.AsNoTracking()
            .Where(x => x.Id == videoProjectId).Select(x => (Guid?)x.WorkspaceId)
            .SingleOrDefaultAsync(cancellationToken);
        if (workspaceId is null || await access.GetRoleAsync(User, workspaceId.Value, cancellationToken) is null)
            return NotFound();

        var genome = await db.ContentGenomes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.VideoProjectId == videoProjectId, cancellationToken);

        if (genome is null)
            return NotFound("No content genome has been extracted for this project yet.");

        IReadOnlyList<string> attributes;
        try
        {
            attributes = JsonSerializer.Deserialize<IReadOnlyList<string>>(genome.AttributesJson) ?? [];
        }
        catch (JsonException)
        {
            attributes = [];
        }

        return Ok(new ContentGenomeResponse(
            genome.VideoProjectId,
            genome.Title,
            genome.DurationSeconds,
            genome.SceneCount,
            genome.WordCount,
            attributes));
    }
}

public sealed record ContentGenomeResponse(
    Guid VideoProjectId,
    string Title,
    int DurationSeconds,
    int SceneCount,
    int WordCount,
    IReadOnlyList<string> Attributes);
