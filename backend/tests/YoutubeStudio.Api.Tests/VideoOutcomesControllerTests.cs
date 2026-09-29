using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class VideoOutcomesControllerTests
{
    [Fact]
    public async Task Create_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var result = await new VideoOutcomesController(db)
            .Create(Guid.NewGuid(), ValidRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_requires_source()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Completed);

        var result = await new VideoOutcomesController(db)
            .Create(project.Id, ValidRequest() with { Source = " " }, CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_rejects_negative_metrics()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Completed);

        var result = await new VideoOutcomesController(db)
            .Create(project.Id, ValidRequest() with { Views = -1 }, CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_requires_completed_project()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);

        var result = await new VideoOutcomesController(db)
            .Create(project.Id, ValidRequest(), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("Outcomes can only be recorded for completed projects.", conflict.Value);
    }

    [Fact]
    public async Task Create_persists_outcome()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Completed);

        var result = await new VideoOutcomesController(db)
            .Create(project.Id, ValidRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<VideoOutcomeResponse>(created.Value);
        Assert.Equal("manual", response.Source);
        Assert.Equal(1000, response.Views);
        Assert.Single(db.VideoOutcomes);
    }

    [Fact]
    public async Task GetAll_returns_only_project_outcomes_newest_first()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Completed);
        var other = await AddProject(db, VideoProjectStatus.Completed);
        db.VideoOutcomes.AddRange(
            new VideoOutcome { VideoProjectId = project.Id, Source = "manual", MeasuredAtUtc = DateTime.UtcNow.AddDays(-2), Views = 10 },
            new VideoOutcome { VideoProjectId = project.Id, Source = "manual", MeasuredAtUtc = DateTime.UtcNow, Views = 50 },
            new VideoOutcome { VideoProjectId = other.Id, Source = "manual", MeasuredAtUtc = DateTime.UtcNow, Views = 99 });
        await db.SaveChangesAsync();

        var result = await new VideoOutcomesController(db).GetAll(project.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var outcomes = Assert.IsAssignableFrom<IReadOnlyList<VideoOutcomeResponse>>(response.Value);
        Assert.Equal(2, outcomes.Count);
        Assert.Equal(50, outcomes[0].Views);
    }

    private static RecordOutcomeRequest ValidRequest() =>
        new("manual", DateTime.UtcNow, 1000, 80, 12, 95.5, 4.25m);

    private static async Task<VideoProject> AddProject(YoutubeStudioDbContext db, VideoProjectStatus status)
    {
        var workspace = new Workspace { Name = "Test workspace" };
        var project = new VideoProject { WorkspaceId = workspace.Id, Prompt = "AI video", Status = status };
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
