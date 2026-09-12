using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class OpportunitiesControllerTests
{
    [Fact]
    public async Task Create_persists_trimmed_opportunity_with_scores()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Creator workspace" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var controller = new OpportunitiesController(db);
        var result = await controller.Create(
            new CreateOpportunityRequest(workspace.Id, "  AI trends  ", 82.5m, 71m, "  audience need  ", "  strong rationale  "),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<OpportunityResponse>(created.Value);
        Assert.Equal("AI trends", response.Title);
        Assert.Equal(82.5m, response.OpportunityScore);
        Assert.Equal(71m, response.RevenueScore);
        Assert.Equal("audience need", response.AudienceProblem);
        Assert.Equal("strong rationale", response.Rationale);
    }

    [Fact]
    public async Task Create_rejects_scores_outside_zero_to_one_hundred()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Creator workspace" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var controller = new OpportunitiesController(db);
        var result = await controller.Create(
            new CreateOpportunityRequest(workspace.Id, "AI trends", 101m, 50m, null, null),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Opportunity and revenue scores must be between 0 and 100.", badRequest.Value);
    }

    [Fact]
    public async Task GetAll_filters_status_and_sorts_by_revenue_score()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Creator workspace" };
        db.Workspaces.Add(workspace);
        db.Opportunities.AddRange(
            new Opportunity { WorkspaceId = workspace.Id, Title = "Low revenue", Status = "new", OpportunityScore = 90, RevenueScore = 20 },
            new Opportunity { WorkspaceId = workspace.Id, Title = "High revenue", Status = "new", OpportunityScore = 70, RevenueScore = 80 },
            new Opportunity { WorkspaceId = workspace.Id, Title = "Other status", Status = "archived", OpportunityScore = 100, RevenueScore = 100 });
        await db.SaveChangesAsync();

        var controller = new OpportunitiesController(db);
        var result = await controller.GetAll(workspace.Id, "new", "revenueScore", "desc", CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var opportunities = Assert.IsAssignableFrom<IReadOnlyList<OpportunityResponse>>(response.Value);
        Assert.Equal(2, opportunities.Count);
        Assert.Equal("High revenue", opportunities[0].Title);
        Assert.Equal("Low revenue", opportunities[1].Title);
    }

    [Fact]
    public async Task GetAll_rejects_missing_workspace()
    {
        await using var db = CreateDb();
        var controller = new OpportunitiesController(db);

        var result = await controller.GetAll(Guid.NewGuid(), null, "opportunityScore", "desc", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }
}
