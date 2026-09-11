using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/channels")]
public sealed class ChannelsController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChannelResponse>>> GetAll(
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var channels = await db.Channels
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderBy(x => x.Name)
            .Select(x => new ChannelResponse(x.Id, x.WorkspaceId, x.Name, x.Platform, x.ExternalChannelId))
            .ToListAsync(cancellationToken);

        return Ok(channels);
    }

    [HttpPost]
    public async Task<ActionResult<ChannelResponse>> Create(
        CreateChannelRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ValidationProblem("Channel name is required.");

        if (!string.Equals(request.Platform, "youtube", StringComparison.OrdinalIgnoreCase))
            return ValidationProblem("Only YouTube channels are supported in the MVP.");

        var workspaceExists = await db.Workspaces.AnyAsync(x => x.Id == request.WorkspaceId, cancellationToken);
        if (!workspaceExists)
            return BadRequest("Workspace does not exist.");

        var channel = new Channel
        {
            WorkspaceId = request.WorkspaceId,
            Name = request.Name.Trim(),
            Platform = "youtube",
            ExternalChannelId = string.IsNullOrWhiteSpace(request.ExternalChannelId)
                ? null
                : request.ExternalChannelId.Trim()
        };

        db.Channels.Add(channel);
        await db.SaveChangesAsync(cancellationToken);

        var response = new ChannelResponse(
            channel.Id,
            channel.WorkspaceId,
            channel.Name,
            channel.Platform,
            channel.ExternalChannelId);

        return CreatedAtAction(nameof(GetAll), new { workspaceId = channel.WorkspaceId }, response);
    }
}

public sealed record CreateChannelRequest(Guid WorkspaceId, string Name, string Platform = "youtube", string? ExternalChannelId = null);
public sealed record ChannelResponse(Guid Id, Guid WorkspaceId, string Name, string Platform, string? ExternalChannelId);
