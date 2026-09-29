using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/channels")]
public sealed class ChannelsController(YoutubeStudioDbContext db, IWorkspaceAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChannelResponse>>> GetAll(
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        if (await access.GetRoleAsync(User, workspaceId, cancellationToken) is null)
            return Forbid();

        var channels = await db.Channels
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderBy(x => x.Name)
            .Select(x => new ChannelResponse(x.Id, x.WorkspaceId, x.Name, x.Platform, x.ExternalChannelId, x.Niche, x.Audience, x.Language, x.Goals))
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

        if (await access.GetRoleAsync(User, request.WorkspaceId, cancellationToken) is null)
            return Forbid();

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

        return CreatedAtAction(nameof(GetAll), new { workspaceId = channel.WorkspaceId }, ToResponse(channel));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ChannelResponse>> UpdateProfile(
        Guid id,
        UpdateChannelProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (await access.GetRoleAsync(User, request.WorkspaceId, cancellationToken) is null)
            return Forbid();

        var channel = await db.Channels.SingleOrDefaultAsync(x => x.Id == id && x.WorkspaceId == request.WorkspaceId, cancellationToken);
        if (channel is null)
            return NotFound("Channel does not exist in the specified workspace.");

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return ValidationProblem("Channel name cannot be blank.");
            channel.Name = request.Name.Trim();
        }
        if (request.Niche is not null) channel.Niche = Normalize(request.Niche);
        if (request.Audience is not null) channel.Audience = Normalize(request.Audience);
        if (!string.IsNullOrWhiteSpace(request.Language)) channel.Language = request.Language.Trim();
        if (request.Goals is not null) channel.Goals = Normalize(request.Goals);

        channel.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(channel));
    }

    private static string? Normalize(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ChannelResponse ToResponse(Channel channel) => new(
        channel.Id, channel.WorkspaceId, channel.Name, channel.Platform, channel.ExternalChannelId,
        channel.Niche, channel.Audience, channel.Language, channel.Goals);
}

public sealed record CreateChannelRequest(Guid WorkspaceId, string Name, string Platform = "youtube", string? ExternalChannelId = null);
public sealed record UpdateChannelProfileRequest(Guid WorkspaceId, string? Name, string? Niche, string? Audience, string? Language, string? Goals);
public sealed record ChannelResponse(Guid Id, Guid WorkspaceId, string Name, string Platform, string? ExternalChannelId, string? Niche, string? Audience, string Language, string? Goals);
