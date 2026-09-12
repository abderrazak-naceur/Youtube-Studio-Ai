using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class WorkspaceChannelControllerTests
{
    [Fact]
    public async Task Create_workspace_persists_trimmed_name()
    {
        await using var db = CreateDb();
        var controller = new WorkspacesController(db);

        var result = await controller.Create(
            new CreateWorkspaceRequest("  My workspace  "),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<WorkspaceResponse>(created.Value);
        Assert.Equal("My workspace", response.Name);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.True(await db.Workspaces.AnyAsync(x => x.Id == response.Id));
    }

    [Fact]
    public async Task Create_workspace_rejects_blank_name()
    {
        await using var db = CreateDb();
        var controller = new WorkspacesController(db);

        var result = await controller.Create(
            new CreateWorkspaceRequest("   "),
            CancellationToken.None);

        var validation = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(validation.Value);
        Assert.Equal("Workspace name is required.", problem.Detail);
    }

    [Fact]
    public async Task Get_channels_returns_only_channels_for_requested_workspace()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Target" };
        var otherWorkspace = new Workspace { Name = "Other" };
        db.Workspaces.AddRange(workspace, otherWorkspace);
        await db.SaveChangesAsync();

        db.Channels.AddRange(
            new Channel { WorkspaceId = workspace.Id, Name = "Target channel", Platform = "youtube" },
            new Channel { WorkspaceId = otherWorkspace.Id, Name = "Other channel", Platform = "youtube" });
        await db.SaveChangesAsync();

        var controller = new ChannelsController(db);
        var result = await controller.GetAll(workspace.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var channels = Assert.IsAssignableFrom<IReadOnlyList<ChannelResponse>>(response.Value);
        var channel = Assert.Single(channels);
        Assert.Equal("Target channel", channel.Name);
        Assert.Equal(workspace.Id, channel.WorkspaceId);
    }

    [Fact]
    public async Task Create_channel_rejects_missing_workspace()
    {
        await using var db = CreateDb();
        var controller = new ChannelsController(db);

        var result = await controller.Create(
            new CreateChannelRequest(Guid.NewGuid(), "My channel"),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Workspace does not exist.", badRequest.Value);
    }

    [Fact]
    public async Task Create_channel_rejects_non_youtube_platform()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Target" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var controller = new ChannelsController(db);
        var result = await controller.Create(
            new CreateChannelRequest(workspace.Id, "My channel", "tiktok"),
            CancellationToken.None);

        var validation = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(validation.Value);
        Assert.Equal("Only YouTube channels are supported in the MVP.", problem.Detail);
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }
}
