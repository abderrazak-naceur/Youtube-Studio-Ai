using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Production;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/video-projects")]
public sealed class VideoProjectsController(YoutubeStudioDbContext db, IProductionJobService productionJobs) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VideoProjectResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var project = await db.VideoProjects.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();
        var job = await db.ProductionJobs.AsNoTracking()
            .Where(x => x.VideoProjectId == id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ProductionJobResponse(x.Id, x.Status.ToString(), x.Attempt, x.LastCompletedStage, x.Error))
            .FirstOrDefaultAsync(cancellationToken);
        return Ok(new VideoProjectResponse(project.Id, project.WorkspaceId, project.ChannelId, project.Prompt, project.Status.ToString(), project.Title, project.Script, job));
    }

    [HttpGet("{id:guid}/pipeline")]
    public async Task<ActionResult<PipelineResponse>> GetPipeline(Guid id, CancellationToken cancellationToken)
    {
        if (!await db.VideoProjects.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken))
            return NotFound();

        var artifacts = await db.ProductionArtifacts.AsNoTracking()
            .Where(x => x.VideoProjectId == id)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new PipelineArtifactResponse(
                x.Id,
                x.Type.ToString(),
                x.ProviderAssetId,
                x.Content,
                x.MetadataJson,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(new PipelineResponse(id, artifacts));
    }

    [HttpPost]
    public async Task<ActionResult<VideoProjectResponse>> Create(CreateVideoProjectRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt)) return ValidationProblem("Video prompt is required.");
        if (!await db.Workspaces.AnyAsync(x => x.Id == request.WorkspaceId, cancellationToken)) return BadRequest("Workspace does not exist.");
        if (request.ChannelId.HasValue && !await db.Channels.AnyAsync(x => x.Id == request.ChannelId.Value && x.WorkspaceId == request.WorkspaceId, cancellationToken)) return BadRequest("Channel does not belong to the workspace.");
        var project = new VideoProject { WorkspaceId = request.WorkspaceId, ChannelId = request.ChannelId, Prompt = request.Prompt.Trim(), Status = VideoProjectStatus.Draft };
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = project.Id }, new VideoProjectResponse(project.Id, project.WorkspaceId, project.ChannelId, project.Prompt, project.Status.ToString(), project.Title, project.Script, null));
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<VideoProjectResponse>> Start(Guid id, CancellationToken cancellationToken)
    {
        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();
        if (project.Status is not VideoProjectStatus.Draft and not VideoProjectStatus.Failed) return Conflict("The video project is already running or completed.");
        project.Status = VideoProjectStatus.Researching;
        project.UpdatedAtUtc = DateTime.UtcNow;
        var job = await productionJobs.EnqueueAsync(project, cancellationToken);
        return AcceptedAtAction(nameof(Get), new { id }, new VideoProjectResponse(project.Id, project.WorkspaceId, project.ChannelId, project.Prompt, project.Status.ToString(), project.Title, project.Script, new ProductionJobResponse(job.Id, job.Status.ToString(), job.Attempt, job.LastCompletedStage, job.Error)));
    }
}

public sealed record CreateVideoProjectRequest(Guid WorkspaceId, Guid? ChannelId, string Prompt);
public sealed record VideoProjectResponse(Guid Id, Guid WorkspaceId, Guid? ChannelId, string Prompt, string Status, string? Title, string? Script, ProductionJobResponse? LatestJob);
public sealed record ProductionJobResponse(Guid Id, string Status, int Attempt, string? LastCompletedStage, string? Error);
public sealed record PipelineResponse(Guid VideoProjectId, IReadOnlyList<PipelineArtifactResponse> Artifacts);
public sealed record PipelineArtifactResponse(Guid Id, string Type, string ProviderAssetId, string? Content, string? MetadataJson, DateTime CreatedAtUtc);
