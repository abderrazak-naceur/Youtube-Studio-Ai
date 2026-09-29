using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class AuditControllerTests
{
    [Fact]
    public async Task GetForProject_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var result = await new AuditController(db, new StubWorkspaceAccess()).WithUser().GetForProject(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetForProject_returns_events_newest_first_scoped_to_project()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);
        var other = await AddProject(db);
        db.AuditEvents.AddRange(
            new AuditEvent { VideoProjectId = project.Id, Action = "video.rejected", Actor = "Alex", CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5) },
            new AuditEvent { VideoProjectId = project.Id, Action = "video.approved", Actor = "Alex", CreatedAtUtc = DateTime.UtcNow },
            new AuditEvent { VideoProjectId = other.Id, Action = "video.approved", Actor = "Sam", CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await new AuditController(db, new StubWorkspaceAccess()).WithUser().GetForProject(project.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<IReadOnlyList<AuditEventResponse>>(response.Value);
        Assert.Equal(2, events.Count);
        Assert.Equal("video.approved", events[0].Action);
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
