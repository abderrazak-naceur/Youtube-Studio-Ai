using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class WorkspaceEconomicsControllerTests
{
    [Fact]
    public async Task Get_aggregates_totals_and_per_video_breakdown()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Studio" };
        var a = new VideoProject { WorkspaceId = workspace.Id, Prompt = "A", Title = "Video A", Status = VideoProjectStatus.Completed };
        var b = new VideoProject { WorkspaceId = workspace.Id, Prompt = "B", Title = "Video B", Status = VideoProjectStatus.Completed };
        db.Workspaces.Add(workspace);
        db.VideoProjects.AddRange(a, b);
        db.ProductionCosts.AddRange(
            new ProductionCost { VideoProjectId = a.Id, Stage = "Render", Provider = "p", Units = 1, UnitCostUsd = 1, TotalCostUsd = 5m },
            new ProductionCost { VideoProjectId = b.Id, Stage = "Render", Provider = "p", Units = 1, UnitCostUsd = 1, TotalCostUsd = 2m });
        db.RevenueEvents.AddRange(
            new RevenueEvent { WorkspaceId = workspace.Id, VideoProjectId = a.Id, Source = "ads", AmountUsd = 30m },
            new RevenueEvent { WorkspaceId = workspace.Id, VideoProjectId = b.Id, Source = "ads", AmountUsd = 4m });
        await db.SaveChangesAsync();

        var result = await new WorkspaceEconomicsController(db, new StubWorkspaceAccess()).WithUser()
            .Get(workspace.Id, CancellationToken.None);

        var response = Assert.IsType<WorkspaceEconomicsResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(34m, response.TotalRevenueUsd);
        Assert.Equal(7m, response.TotalProductionCostUsd);
        Assert.Equal(27m, response.TotalContributionUsd);
        // Video A (30-5=25) should rank above Video B (4-2=2).
        Assert.Equal(a.Id, response.Videos[0].VideoProjectId);
        Assert.Equal(25m, response.Videos[0].ContributionUsd);
    }

    [Fact]
    public async Task Get_returns_zero_for_empty_workspace()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Studio" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var result = await new WorkspaceEconomicsController(db, new StubWorkspaceAccess()).WithUser()
            .Get(workspace.Id, CancellationToken.None);

        var response = Assert.IsType<WorkspaceEconomicsResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(0m, response.TotalRevenueUsd);
        Assert.Equal(0m, response.ContributionMargin);
        Assert.Empty(response.Videos);
    }

    [Fact]
    public async Task Get_forbids_non_member()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Studio" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var result = await new WorkspaceEconomicsController(db, new StubWorkspaceAccess(null)).WithUser()
            .Get(workspace.Id, CancellationToken.None);

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
