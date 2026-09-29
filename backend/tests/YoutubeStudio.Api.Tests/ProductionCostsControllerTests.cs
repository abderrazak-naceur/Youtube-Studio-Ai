using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class ProductionCostsControllerTests
{
    [Fact]
    public async Task Get_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var result = await new ProductionCostsController(db, new StubWorkspaceAccess()).WithUser().Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Get_returns_zero_total_when_no_costs()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);

        var result = await new ProductionCostsController(db, new StubWorkspaceAccess()).WithUser().Get(project.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var summary = Assert.IsType<ProductionCostSummaryResponse>(response.Value);
        Assert.Equal(0m, summary.TotalCostUsd);
        Assert.Empty(summary.ByStage);
        Assert.Empty(summary.LineItems);
    }

    [Fact]
    public async Task Get_aggregates_total_and_groups_by_stage()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);
        db.ProductionCosts.AddRange(
            new ProductionCost { VideoProjectId = project.Id, Stage = "Voice", Provider = "placeholder-voice", Units = 100m, UnitCostUsd = 0.0004m, TotalCostUsd = 0.04m },
            new ProductionCost { VideoProjectId = project.Id, Stage = "Visual", Provider = "placeholder-visual", Units = 1m, UnitCostUsd = 0.02m, TotalCostUsd = 0.02m },
            new ProductionCost { VideoProjectId = project.Id, Stage = "Visual", Provider = "placeholder-visual", Units = 1m, UnitCostUsd = 0.02m, TotalCostUsd = 0.02m },
            new ProductionCost { VideoProjectId = project.Id, Stage = "Render", Provider = "placeholder-render", Units = 28m, UnitCostUsd = 0.0006m, TotalCostUsd = 0.0168m });
        await db.SaveChangesAsync();

        var result = await new ProductionCostsController(db, new StubWorkspaceAccess()).WithUser().Get(project.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var summary = Assert.IsType<ProductionCostSummaryResponse>(response.Value);
        Assert.Equal(0.0968m, summary.TotalCostUsd);
        Assert.Equal(4, summary.LineItems.Count);

        var visual = summary.ByStage.Single(x => x.Stage == "Visual");
        Assert.Equal(0.04m, visual.TotalCostUsd);
        Assert.Equal(3, summary.ByStage.Count);
    }

    [Fact]
    public async Task Get_scopes_costs_to_project()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);
        var other = await AddProject(db);
        db.ProductionCosts.AddRange(
            new ProductionCost { VideoProjectId = project.Id, Stage = "Voice", Provider = "p", Units = 1m, UnitCostUsd = 1m, TotalCostUsd = 1m },
            new ProductionCost { VideoProjectId = other.Id, Stage = "Voice", Provider = "p", Units = 5m, UnitCostUsd = 1m, TotalCostUsd = 5m });
        await db.SaveChangesAsync();

        var result = await new ProductionCostsController(db, new StubWorkspaceAccess()).WithUser().Get(project.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var summary = Assert.IsType<ProductionCostSummaryResponse>(response.Value);
        Assert.Equal(1m, summary.TotalCostUsd);
        Assert.Single(summary.LineItems);
    }

    private static async Task<VideoProject> AddProject(YoutubeStudioDbContext db)
    {
        var workspace = new Workspace { Name = "Test workspace" };
        var project = new VideoProject
        {
            WorkspaceId = workspace.Id,
            Prompt = "Create a video about AI",
            Status = VideoProjectStatus.Completed
        };
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
