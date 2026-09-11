using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class VideoProjectsControllerTests
{
    [Fact]
    public async Task Create_rejects_missing_workspace()
    {
        await using var db = CreateDb();
        var controller = new VideoProjectsController(db);

        var result = await controller.Create(
            new CreateVideoProjectRequest(Guid.NewGuid(), null, "Create a video about AI"),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_persists_draft_project()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Test workspace" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var controller = new VideoProjectsController(db);
        var result = await controller.Create(
            new CreateVideoProjectRequest(workspace.Id, null, "  Create a video about AI  "),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<VideoProjectResponse>(created.Value);
        Assert.Equal("Create a video about AI", response.Prompt);
        Assert.Equal(nameof(VideoProjectStatus.Draft), response.Status);
        Assert.NotEqual(Guid.Empty, response.Id);
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }
}
