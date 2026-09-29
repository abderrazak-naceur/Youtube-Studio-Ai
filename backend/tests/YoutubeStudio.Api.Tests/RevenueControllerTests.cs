using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class RevenueControllerTests
{
    [Fact]
    public async Task Record_persists_revenue_event()
    {
        await using var db = CreateDb();
        var workspace = await AddWorkspace(db);
        var controller = new RevenueController(db, new StubWorkspaceAccess()).WithUser();

        var result = await controller.Record(workspace.Id,
            new RecordRevenueRequest(null, null, "affiliate", 12.50m, "buy link", 0.8, default), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<RevenueEventResponse>(created.Value);
        Assert.Equal("affiliate", response.Source);
        Assert.Equal(12.50m, response.AmountUsd);
        Assert.Single(db.RevenueEvents);
    }

    [Fact]
    public async Task Record_rejects_negative_amount_and_bad_confidence()
    {
        await using var db = CreateDb();
        var workspace = await AddWorkspace(db);
        var controller = new RevenueController(db, new StubWorkspaceAccess()).WithUser();

        Assert.IsType<ObjectResult>((await controller.Record(workspace.Id, new RecordRevenueRequest(null, null, "ads", -1m, null, 1.0, default), CancellationToken.None)).Result);
        Assert.IsType<ObjectResult>((await controller.Record(workspace.Id, new RecordRevenueRequest(null, null, "ads", 1m, null, 2.0, default), CancellationToken.None)).Result);
        Assert.Empty(db.RevenueEvents);
    }

    [Fact]
    public async Task Summary_totals_and_groups_by_source()
    {
        await using var db = CreateDb();
        var workspace = await AddWorkspace(db);
        db.RevenueEvents.AddRange(
            new RevenueEvent { WorkspaceId = workspace.Id, Source = "ads", AmountUsd = 10m },
            new RevenueEvent { WorkspaceId = workspace.Id, Source = "ads", AmountUsd = 5m },
            new RevenueEvent { WorkspaceId = workspace.Id, Source = "affiliate", AmountUsd = 20m });
        await db.SaveChangesAsync();

        var result = await new RevenueController(db, new StubWorkspaceAccess()).WithUser().GetSummary(workspace.Id, CancellationToken.None);

        var summary = Assert.IsType<RevenueSummaryResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(35m, summary.TotalUsd);
        Assert.Equal("affiliate", summary.BySource[0].Source); // highest first
        Assert.Equal(15m, summary.BySource.Single(x => x.Source == "ads").AmountUsd);
    }

    [Fact]
    public async Task Summary_forbids_non_member()
    {
        await using var db = CreateDb();
        var workspace = await AddWorkspace(db);
        var result = await new RevenueController(db, new StubWorkspaceAccess(null)).WithUser().GetSummary(workspace.Id, CancellationToken.None);
        Assert.IsType<ForbidResult>(result.Result);
    }

    private static async Task<Workspace> AddWorkspace(YoutubeStudioDbContext db)
    {
        var workspace = new Workspace { Name = "Studio" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();
        return workspace;
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(opts);
    }
}
