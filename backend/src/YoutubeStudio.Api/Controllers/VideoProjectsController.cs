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
                x.Script,
                db.ProductionJobs
                    .Where(job => job.VideoProjectId == x.Id)
                    .OrderByDescending(job => job.CreatedAtUtc)
                    .Select(job => new ProductionJobResponse(job.Id, job.Status.ToString(), job.Attempt, job.Error))
                    .FirstOrDefault()))
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

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<VideoProjectResponse>> Start(Guid id, CancellationToken cancellationToken)
    {
        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null)
            return NotFound();

        if (project.Status is not VideoProjectStatus.Draft and not VideoProjectStatus.Failed)
            return Conflict("The video project is already running or completed.");

        var job = new ProductionJob
        {
            VideoProjectId = project.Id,
            Status = ProductionJobStatus.Queued
        };

        project.Status = VideoProjectStatus.Researching;
        project.UpdatedAtUtc = DateTime.UtcNow;
        db.ProductionJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);

        return AcceptedAtAction(nameof(Get), new { id = project.Id }, ToResponse(project, job));
    }

    private static VideoProjectResponse ToResponse(VideoProject project, ProductionJob? job = null) =>
        new(project.Id, project.WorkspaceId, project.ChannelId, project.Prompt,
            project.Status.ToString(), project.Title, project.Script,
            job is null ? null : new ProductionJobResponse(job.Id, job.Status.ToString(), job.Attempt, job.Error));
}

public sealed record CreateVideoProjectRequest(Guid WorkspaceId, Guid? ChannelId, string Prompt);

public sealed record VideoProjectResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? ChannelId,
    string Prompt,
    string Status,
    string? Title,
    string? Script,
    ProductionJobResponse? LatestJob);

public sealed record ProductionJobResponse(Guid Id, string Status, int Attempt, string? Error);
