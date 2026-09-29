using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class AnalyticsControllerTests
{
    [Fact]
    public async Task GenomeCorrelation_aggregates_views_per_attribute()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Studio" };
        var projectA = new VideoProject { WorkspaceId = workspace.Id, Prompt = "A", Status = VideoProjectStatus.Completed };
        var projectB = new VideoProject { WorkspaceId = workspace.Id, Prompt = "B", Status = VideoProjectStatus.Completed };
        db.Workspaces.Add(workspace);
        db.VideoProjects.AddRange(projectA, projectB);
        db.ContentGenomes.AddRange(
            new ContentGenome { VideoProjectId = projectA.Id, Title = "A", AttributesJson = JsonSerializer.Serialize(new[] { "ai", "tutorial" }) },
            new ContentGenome { VideoProjectId = projectB.Id, Title = "B", AttributesJson = JsonSerializer.Serialize(new[] { "ai", "news" }) });
        db.VideoOutcomes.AddRange(
            new VideoOutcome { VideoProjectId = projectA.Id, Source = "manual", MeasuredAtUtc = DateTime.UtcNow, Views = 1000 },
            new VideoOutcome { VideoProjectId = projectB.Id, Source = "manual", MeasuredAtUtc = DateTime.UtcNow, Views = 3000 });
        await db.SaveChangesAsync();

        var result = await new AnalyticsController(db, new StubWorkspaceAccess()).WithUser()
            .GenomeCorrelation(workspace.Id, CancellationToken.None);

        var response = Assert.IsType<GenomeCorrelationResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(2, response.AnalyzedProjects);

        var ai = response.Attributes.Single(x => x.Attribute == "ai");
        Assert.Equal(2, ai.VideoCount);
        Assert.Equal(2000, ai.AverageViews); // (1000 + 3000) / 2

        var tutorial = response.Attributes.Single(x => x.Attribute == "tutorial");
        Assert.Equal(1000, tutorial.AverageViews);
    }

    [Fact]
    public async Task GenomeCorrelation_ignores_projects_without_outcome()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Studio" };
        var project = new VideoProject { WorkspaceId = workspace.Id, Prompt = "A", Status = VideoProjectStatus.Completed };
        db.Workspaces.Add(workspace);
        db.VideoProjects.Add(project);
        db.ContentGenomes.Add(new ContentGenome { VideoProjectId = project.Id, Title = "A", AttributesJson = JsonSerializer.Serialize(new[] { "ai" }) });
        await db.SaveChangesAsync();

        var result = await new AnalyticsController(db, new StubWorkspaceAccess()).WithUser()
            .GenomeCorrelation(workspace.Id, CancellationToken.None);

        var response = Assert.IsType<GenomeCorrelationResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(0, response.AnalyzedProjects);
        Assert.Empty(response.Attributes);
    }

    [Fact]
    public async Task GenomeCorrelation_forbids_non_member()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Studio" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var result = await new AnalyticsController(db, new StubWorkspaceAccess(null)).WithUser()
            .GenomeCorrelation(workspace.Id, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(opts);
    }
}
