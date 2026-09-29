using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class ResearchProjectsControllerTests
{
    [Fact]
    public async Task Create_persists_project_for_workspace_opportunity()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Creator workspace" };
        var opportunity = new Opportunity { WorkspaceId = workspace.Id, Title = "AI trends", OpportunityScore = 80, RevenueScore = 70 };
        db.Workspaces.Add(workspace);
        db.Opportunities.Add(opportunity);
        await db.SaveChangesAsync();

        var controller = new ResearchProjectsController(db);
        var result = await controller.Create(
            new CreateResearchProjectRequest(workspace.Id, opportunity.Id),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ResearchProjectResponse>(created.Value);
        Assert.Equal(workspace.Id, response.WorkspaceId);
        Assert.Equal(opportunity.Id, response.OpportunityId);
        Assert.Equal("draft", response.Status);
        Assert.Single(db.ResearchProjects);
    }

    [Fact]
    public async Task Create_rejects_opportunity_from_another_workspace()
    {
        await using var db = CreateDb();
        var owner = new Workspace { Name = "Owner" };
        var other = new Workspace { Name = "Other" };
        var opportunity = new Opportunity { WorkspaceId = owner.Id, Title = "Private", OpportunityScore = 50, RevenueScore = 50 };
        db.Workspaces.AddRange(owner, other);
        db.Opportunities.Add(opportunity);
        await db.SaveChangesAsync();

        var controller = new ResearchProjectsController(db);
        var result = await controller.Create(
            new CreateResearchProjectRequest(other.Id, opportunity.Id),
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Empty(db.ResearchProjects);
    }

    [Fact]
    public async Task Create_rejects_duplicate_project_for_opportunity()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Creator workspace" };
        var opportunity = new Opportunity { WorkspaceId = workspace.Id, Title = "AI trends", OpportunityScore = 80, RevenueScore = 70 };
        db.Workspaces.Add(workspace);
        db.Opportunities.Add(opportunity);
        db.ResearchProjects.Add(new ResearchProject { WorkspaceId = workspace.Id, OpportunityId = opportunity.Id });
        await db.SaveChangesAsync();

        var controller = new ResearchProjectsController(db);
        var result = await controller.Create(
            new CreateResearchProjectRequest(workspace.Id, opportunity.Id),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_returns_only_workspace_projects()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Creator workspace" };
        var other = new Workspace { Name = "Other" };
        var firstOpportunity = new Opportunity { WorkspaceId = workspace.Id, Title = "First", OpportunityScore = 70, RevenueScore = 70 };
        var secondOpportunity = new Opportunity { WorkspaceId = other.Id, Title = "Second", OpportunityScore = 60, RevenueScore = 60 };
        db.Workspaces.AddRange(workspace, other);
        db.Opportunities.AddRange(firstOpportunity, secondOpportunity);
        db.ResearchProjects.AddRange(
            new ResearchProject { WorkspaceId = workspace.Id, OpportunityId = firstOpportunity.Id },
            new ResearchProject { WorkspaceId = other.Id, OpportunityId = secondOpportunity.Id });
        await db.SaveChangesAsync();

        var controller = new ResearchProjectsController(db);
        var result = await controller.GetAll(workspace.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var projects = Assert.IsAssignableFrom<IReadOnlyList<ResearchProjectResponse>>(response.Value);
        Assert.Single(projects);
        Assert.Equal(firstOpportunity.Id, projects[0].OpportunityId);
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }
}
