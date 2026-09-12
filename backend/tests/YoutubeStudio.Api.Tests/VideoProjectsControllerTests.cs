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
    public async Task Create_rejects_blank_prompt()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Test workspace" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.Create(
            new CreateVideoProjectRequest(workspace.Id, null, "   "),
            CancellationToken.None);

        var validation = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(validation.Value);
        Assert.Equal("Video prompt is required.", problem.Detail);
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
