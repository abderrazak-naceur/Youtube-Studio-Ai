using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Production;

namespace YoutubeStudio.Api.Tests;

public sealed class ProductionJobServiceTests
{
    [Fact]
    public async Task Enqueue_with_same_idempotency_key_returns_same_job()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);
        var service = new ProductionJobService(db);

        var first = await service.EnqueueAsync(project, "key-1", CancellationToken.None);
        // Simulate the first job having finished so the active-job guard would not apply.
        first.Status = ProductionJobStatus.Succeeded;
        await db.SaveChangesAsync();

        var second = await service.EnqueueAsync(project, "key-1", CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await db.ProductionJobs.CountAsync(x => x.VideoProjectId == project.Id));
    }

    [Fact]
    public async Task Enqueue_without_key_does_not_duplicate_active_job()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);
        var service = new ProductionJobService(db);

        var first = await service.EnqueueAsync(project, null, CancellationToken.None);
        var second = await service.EnqueueAsync(project, null, CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
    }

    private static async Task<VideoProject> AddProject(YoutubeStudioDbContext db)
    {
        var workspace = new Workspace { Name = "Studio" };
        var project = new VideoProject { WorkspaceId = workspace.Id, Prompt = "AI video", Status = VideoProjectStatus.Draft };
        db.Workspaces.Add(workspace);
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(opts);
    }
}
