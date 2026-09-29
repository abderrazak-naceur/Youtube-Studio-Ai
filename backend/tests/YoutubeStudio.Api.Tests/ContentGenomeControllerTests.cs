using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class ContentGenomeControllerTests
{
    [Fact]
    public async Task Get_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var result = await new ContentGenomeController(db, new StubWorkspaceAccess()).WithUser().Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Get_returns_not_found_when_genome_not_extracted()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);

        var result = await new ContentGenomeController(db, new StubWorkspaceAccess()).WithUser().Get(project.Id, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Get_returns_extracted_genome()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);
        db.ContentGenomes.Add(new ContentGenome
        {
            VideoProjectId = project.Id,
            Title = "AI Explained",
            DurationSeconds = 28,
            SceneCount = 3,
            WordCount = 120,
            AttributesJson = JsonSerializer.Serialize(new[] { "artificial", "creators" })
        });
        await db.SaveChangesAsync();

        var result = await new ContentGenomeController(db, new StubWorkspaceAccess()).WithUser().Get(project.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var genome = Assert.IsType<ContentGenomeResponse>(response.Value);
        Assert.Equal("AI Explained", genome.Title);
        Assert.Equal(3, genome.SceneCount);
        Assert.Equal(28, genome.DurationSeconds);
        Assert.Contains("artificial", genome.Attributes);
    }

    private static async Task<VideoProject> AddProject(YoutubeStudioDbContext db)
    {
        var workspace = new Workspace { Name = "Test workspace" };
        var project = new VideoProject { WorkspaceId = workspace.Id, Prompt = "AI video", Status = VideoProjectStatus.Completed };
        db.Workspaces.Add(workspace);
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }
}
