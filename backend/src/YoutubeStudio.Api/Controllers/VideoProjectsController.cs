using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/video-projects")]
public sealed class VideoProjectsController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VideoProjectResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var project = await db.VideoProjects
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new VideoProjectResponse(
                x.Id,
                x.WorkspaceId,
                x.ChannelId,
                x.Prompt,
                x.Status.ToString(),
                x.Title,
                x.Script))
            .SingleOrDefaultAsync(cancellationToken);

        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<VideoProjectResponse>> Create(
        CreateVideoProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return ValidationProblem("Video prompt is required.");

        var workspaceExists = await db.Workspaces.AnyAsync(x => x.Id == request.WorkspaceId, cancellationToken);
        if (!workspaceExists)
            return BadRequest("Workspace does not exist.");

        if (request.ChannelId.HasValue)
        {
            var channelBelongsToWorkspace = await db.Channels.AnyAsync(
                x => x.Id == request.ChannelId.Value && x.WorkspaceId == request.WorkspaceId,
                cancellationToken);

            if (!channelBelongsToWorkspace)
                return BadRequest("Channel does not belong to the workspace.");
        }

        var project = new VideoProject
        {
            WorkspaceId = request.WorkspaceId,
            ChannelId = request.ChannelId,
            Prompt = request.Prompt.Trim(),
            Status = VideoProjectStatus.Draft
        };

        db.VideoProjects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = project.Id }, ToResponse(project));
    }

    private static VideoProjectResponse ToResponse(VideoProject project) =>
        new(project.Id, project.WorkspaceId, project.ChannelId, project.Prompt,
            project.Status.ToString(), project.Title, project.Script);
}

public sealed record CreateVideoProjectRequest(Guid WorkspaceId, Guid? ChannelId, string Prompt);

public sealed record VideoProjectResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? ChannelId,
    string Prompt,
    string Status,
    string? Title,
    string? Script);
