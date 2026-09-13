using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Production;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/video-projects")]
public sealed class VideoProjectsController(
    YoutubeStudioDbContext db,
    IProductionJobService productionJobs,
    IScriptProvider scriptProvider,
    IScenePlanProvider scenePlanProvider = null!) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VideoProjectListItemResponse>>> List([FromQuery] Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!await db.Workspaces.AnyAsync(x => x.Id == workspaceId, cancellationToken)) return BadRequest("Workspace does not exist.");

        var projects = await db.VideoProjects.AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(20)
            .Select(x => new VideoProjectListItemResponse(x.Id, x.Prompt, x.Status.ToString(), x.Title, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(projects);
    }

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

    [HttpPost("{id:guid}/script")]
    public async Task<ActionResult<VideoProjectResponse>> GenerateScript(Guid id, GenerateScriptRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ResearchSummary)) return ValidationProblem("Research summary is required to generate a script.");

        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();

        var result = await scriptProvider.GenerateScriptAsync(
            new ScriptRequest(project.Prompt, request.ResearchSummary.Trim()),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result.Title) || string.IsNullOrWhiteSpace(result.Script))
            return Problem("The script provider returned an empty script.", statusCode: StatusCodes.Status502BadGateway);

        project.Title = result.Title.Trim();
        project.Script = result.Script.Trim();
        project.Status = VideoProjectStatus.Scripted;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new VideoProjectResponse(project.Id, project.WorkspaceId, project.ChannelId, project.Prompt, project.Status.ToString(), project.Title, project.Script, null));
    }

    [HttpPost("{id:guid}/scene-plan")]
    public async Task<ActionResult<VideoProjectResponse>> GenerateScenePlan(Guid id, CancellationToken cancellationToken)
    {
        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();
        if (string.IsNullOrWhiteSpace(project.Title) || string.IsNullOrWhiteSpace(project.Script))
            return ValidationProblem("A title and script are required to generate a scene plan.");
        if (project.Status != VideoProjectStatus.Scripted)
            return Conflict("The video project must be scripted before generating a scene plan.");

        var result = await scenePlanProvider.CreateScenePlanAsync(
            new ScenePlanRequest(project.Title.Trim(), project.Script.Trim()),
            cancellationToken);

        if (!HasValidScenePlan(result))
            return Problem("The scene-plan provider returned an invalid scene plan.", statusCode: StatusCodes.Status502BadGateway);

        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.ScenePlan,
            ProviderAssetId = "scene-plan",
            Content = JsonSerializer.Serialize(result.Scenes),
            MetadataJson = JsonSerializer.Serialize(new { sceneCount = result.Scenes.Count })
        });

        project.Status = VideoProjectStatus.Planned;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new VideoProjectResponse(project.Id, project.WorkspaceId, project.ChannelId, project.Prompt, project.Status.ToString(), project.Title, project.Script, null));
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

    private static bool HasValidScenePlan(ScenePlanResult result) =>
        result.Scenes is { Count: > 0 } && result.Scenes.All(scene =>
            scene.Number > 0 &&
            scene.DurationSeconds > 0 &&
            !string.IsNullOrWhiteSpace(scene.Narration) &&
            !string.IsNullOrWhiteSpace(scene.VisualDirection));
}

public sealed record CreateVideoProjectRequest(Guid WorkspaceId, Guid? ChannelId, string Prompt);
public sealed record GenerateScriptRequest(string ResearchSummary);
public sealed record VideoProjectListItemResponse(Guid Id, string Prompt, string Status, string? Title, DateTime UpdatedAtUtc);
public sealed record VideoProjectResponse(Guid Id, Guid WorkspaceId, Guid? ChannelId, string Prompt, string Status, string? Title, string? Script, ProductionJobResponse? LatestJob);
public sealed record ProductionJobResponse(Guid Id, string Status, int Attempt, string? LastCompletedStage, string? Error);
