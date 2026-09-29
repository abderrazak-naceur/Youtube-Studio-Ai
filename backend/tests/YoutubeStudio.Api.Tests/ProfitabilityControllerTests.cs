using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class ProfitabilityControllerTests
{
    [Fact]
    public async Task Get_computes_contribution_and_margin()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);
        db.ProductionCosts.AddRange(
            new ProductionCost { VideoProjectId = project.Id, Stage = "Voice", Provider = "p", Units = 1, UnitCostUsd = 1, TotalCostUsd = 2m },
            new ProductionCost { VideoProjectId = project.Id, Stage = "Render", Provider = "p", Units = 1, UnitCostUsd = 1, TotalCostUsd = 3m });
        db.RevenueEvents.Add(new RevenueEvent { WorkspaceId = project.WorkspaceId, VideoProjectId = project.Id, Source = "ads", AmountUsd = 20m });
        await db.SaveChangesAsync();

        var result = await new ProfitabilityController(db, new StubWorkspaceAccess()).WithUser()
            .Get(project.Id, CancellationToken.None);

        var response = Assert.IsType<VideoProfitabilityResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(20m, response.RevenueUsd);
        Assert.Equal(5m, response.ProductionCostUsd);
        Assert.Equal(15m, response.ContributionUsd);
        Assert.Equal(0.75m, response.ContributionMargin);
    }

    [Fact]
    public async Task Get_returns_zero_margin_without_revenue()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);
        db.ProductionCosts.Add(new ProductionCost { VideoProjectId = project.Id, Stage = "Voice", Provider = "p", Units = 1, UnitCostUsd = 1, TotalCostUsd = 4m });
        await db.SaveChangesAsync();

        var result = await new ProfitabilityController(db, new StubWorkspaceAccess()).WithUser()
            .Get(project.Id, CancellationToken.None);

        var response = Assert.IsType<VideoProfitabilityResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(-4m, response.ContributionUsd);
        Assert.Equal(0m, response.ContributionMargin);
    }

    [Fact]
    public async Task Get_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var result = await new ProfitabilityController(db, new StubWorkspaceAccess()).WithUser()
            .Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Get_returns_not_found_for_non_member()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);

        var result = await new ProfitabilityController(db, new StubWorkspaceAccess(null)).WithUser()
            .Get(project.Id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static async Task<VideoProject> AddProject(YoutubeStudioDbContext db)
    {
        var workspace = new Workspace { Name = "Studio" };
        var project = new VideoProject { WorkspaceId = workspace.Id, Prompt = "AI video", Status = VideoProjectStatus.Completed };
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
