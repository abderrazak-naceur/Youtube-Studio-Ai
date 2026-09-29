using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Versioned editorial identity for a channel (backlog US-005, DATA-MODEL §3 versioning).
/// Saving new DNA appends a version; prior versions are preserved.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/channels/{channelId:guid}/dna")]
public sealed class ChannelDnaController(YoutubeStudioDbContext db, IWorkspaceAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ChannelDnaResponse>> GetLatest(Guid channelId, CancellationToken cancellationToken)
    {
        var channel = await LoadChannelAsync(channelId, cancellationToken);
        if (channel is null || await access.GetRoleAsync(User, channel.WorkspaceId, cancellationToken) is null)
            return NotFound();

        var dna = await db.ChannelDnas.AsNoTracking()
            .Where(x => x.ChannelId == channelId)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return dna is null ? NotFound("No DNA has been defined for this channel.") : Ok(ToResponse(dna));
    }

    [HttpGet("versions")]
    public async Task<ActionResult<IReadOnlyList<ChannelDnaResponse>>> GetVersions(Guid channelId, CancellationToken cancellationToken)
    {
        var channel = await LoadChannelAsync(channelId, cancellationToken);
        if (channel is null || await access.GetRoleAsync(User, channel.WorkspaceId, cancellationToken) is null)
            return NotFound();

        var versions = await db.ChannelDnas.AsNoTracking()
            .Where(x => x.ChannelId == channelId)
            .OrderByDescending(x => x.Version)
            .Select(x => ToResponse(x))
            .ToListAsync(cancellationToken);

        return Ok(versions);
    }

    [HttpPost]
    public async Task<ActionResult<ChannelDnaResponse>> Create(Guid channelId, SaveChannelDnaRequest request, CancellationToken cancellationToken)
    {
        var channel = await LoadChannelAsync(channelId, cancellationToken);
        if (channel is null || await access.GetRoleAsync(User, channel.WorkspaceId, cancellationToken) is null)
            return NotFound();

        var nextVersion = (await db.ChannelDnas
            .Where(x => x.ChannelId == channelId)
            .Select(x => (int?)x.Version)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        var dna = new ChannelDna
        {
            WorkspaceId = channel.WorkspaceId,
            ChannelId = channelId,
            Version = nextVersion,
            Audience = Normalize(request.Audience),
            Positioning = Normalize(request.Positioning),
            Tone = Normalize(request.Tone),
            VisualLanguage = Normalize(request.VisualLanguage),
            RecurringFormats = Normalize(request.RecurringFormats),
            ForbiddenPatterns = Normalize(request.ForbiddenPatterns),
            StrategicGoals = Normalize(request.StrategicGoals)
        };

        db.ChannelDnas.Add(dna);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetLatest), new { channelId }, ToResponse(dna));
    }

    private Task<Channel?> LoadChannelAsync(Guid channelId, CancellationToken cancellationToken) =>
        db.Channels.AsNoTracking().SingleOrDefaultAsync(x => x.Id == channelId, cancellationToken);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ChannelDnaResponse ToResponse(ChannelDna dna) => new(
        dna.Id, dna.ChannelId, dna.Version, dna.Audience, dna.Positioning, dna.Tone,
        dna.VisualLanguage, dna.RecurringFormats, dna.ForbiddenPatterns, dna.StrategicGoals, dna.CreatedAtUtc);
}

public sealed record SaveChannelDnaRequest(
    string? Audience, string? Positioning, string? Tone, string? VisualLanguage,
    string? RecurringFormats, string? ForbiddenPatterns, string? StrategicGoals);

public sealed record ChannelDnaResponse(
    Guid Id, Guid ChannelId, int Version, string? Audience, string? Positioning, string? Tone,
    string? VisualLanguage, string? RecurringFormats, string? ForbiddenPatterns, string? StrategicGoals, DateTime CreatedAtUtc);
