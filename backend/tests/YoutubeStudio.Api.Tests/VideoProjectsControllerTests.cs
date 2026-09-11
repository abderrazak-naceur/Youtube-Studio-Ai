using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Production;

namespace YoutubeStudio.Api.Tests;

public sealed class VideoProjectsControllerTests
{
    [Fact]
    public async Task Create_rejects_missing_workspace()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var result = await controller.Create(
            new CreateVideoProjectRequest(Guid.NewGuid(), null, "Create a video about AI"),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_rejects_channel_from_another_workspace()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Target workspace" };
        var otherWorkspace = new Workspace { Name = "Other workspace" };
        db.Workspaces.AddRange(workspace, otherWorkspace);
        await db.SaveChangesAsync();

        var channel = new Channel { WorkspaceId = otherWorkspace.Id, Name = "Other channel" };
        db.Channels.Add(channel);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.Create(
            new CreateVideoProjectRequest(workspace.Id, channel.Id, "Create a video about AI"),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Channel does not belong to the workspace.", badRequest.Value);
    }

    [Fact]
    public async Task Create_persists_draft_project()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Test workspace" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.Create(
            new CreateVideoProjectRequest(workspace.Id, null, "  Create a video about AI  "),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<VideoProjectResponse>(created.Value);
        Assert.Equal("Create a video about AI", response.Prompt);
        Assert.Equal(nameof(VideoProjectStatus.Draft), response.Status);
        Assert.NotEqual(Guid.Empty, response.Id);
    }

    [Fact]
    public async Task Start_enqueues_job_and_moves_project_to_researching()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Test workspace" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var project = new VideoProject
        {
            WorkspaceId = workspace.Id,
            Prompt = "Create a video about AI",
            Status = VideoProjectStatus.Draft
        };
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.Start(project.Id, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedAtActionResult>(result.Result);
        var response = Assert.IsType<VideoProjectResponse>(accepted.Value);
        Assert.Equal(nameof(VideoProjectStatus.Researching), response.Status);
        Assert.NotNull(response.LatestJob);
        Assert.Equal(nameof(ProductionJobStatus.Queued), response.LatestJob!.Status);

        var persisted = await db.VideoProjects.SingleAsync(x => x.Id == project.Id);
        var job = await db.ProductionJobs.SingleAsync(x => x.VideoProjectId == project.Id);
        Assert.Equal(VideoProjectStatus.Researching, persisted.Status);
        Assert.Equal(ProductionJobStatus.Queued, job.Status);
    }

    [Fact]
    public async Task Start_does_not_create_duplicate_job_when_project_is_already_running()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Test workspace" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var project = new VideoProject
        {
            WorkspaceId = workspace.Id,
            Prompt = "Create a video about AI",
            Status = VideoProjectStatus.Draft
        };
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var first = await controller.Start(project.Id, CancellationToken.None);
        Assert.IsType<AcceptedAtActionResult>(first.Result);

        var second = await controller.Start(project.Id, CancellationToken.None);
        var conflict = Assert.IsType<ConflictObjectResult>(second.Result);
        Assert.Equal("The video project is already running or completed.", conflict.Value);
        Assert.Equal(1, await db.ProductionJobs.CountAsync(x => x.VideoProjectId == project.Id));
    }

    [Fact]
    public async Task GetPipeline_returns_artifacts_in_creation_order()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Test workspace" };
        db.Workspaces.Add(workspace);
        var project = new VideoProject
        {
            WorkspaceId = workspace.Id,
            Prompt = "Create a video about AI",
            Status = VideoProjectStatus.Scripted
        };
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();

        var first = new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Research,
            ProviderAssetId = "research-1",
            Content = "source evidence",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2)
        };
        var second = new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Script,
            ProviderAssetId = "script-1",
            Content = "generated script",
            MetadataJson = "{\"version\":1}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };
        db.ProductionArtifacts.AddRange(first, second);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetPipeline(project.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PipelineResponse>(ok.Value);
        Assert.Equal(project.Id, response.VideoProjectId);
        Assert.Equal(2, response.Artifacts.Count);
        Assert.Equal(nameof(ProductionArtifactType.Research), response.Artifacts[0].Type);
        Assert.Equal("research-1", response.Artifacts[0].ProviderAssetId);
        Assert.Equal(nameof(ProductionArtifactType.Script), response.Artifacts[1].Type);
        Assert.Equal("generated script", response.Artifacts[1].Content);
    }

    [Fact]
    public async Task GetPipeline_returns_not_found_for_unknown_project()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var result = await controller.GetPipeline(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static VideoProjectsController CreateController(YoutubeStudioDbContext db) =>
        new(db, new ProductionJobService(db));

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }
}
