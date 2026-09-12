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

    [Fact]
    public async Task AddSource_persists_source_for_project()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Creator workspace" };
        var opportunity = new Opportunity { WorkspaceId = workspace.Id, Title = "AI trends", OpportunityScore = 80, RevenueScore = 70 };
        var project = new ResearchProject { WorkspaceId = workspace.Id, OpportunityId = opportunity.Id };
        db.Workspaces.Add(workspace);
        db.Opportunities.Add(opportunity);
        db.ResearchProjects.Add(project);
        await db.SaveChangesAsync();

        var controller = new ResearchProjectsController(db);
        var result = await controller.AddSource(
            project.Id,
            new CreateResearchSourceRequest(workspace.Id, project.Id, "https://example.com/article", "Example article", "{\"author\":\"Test\"}"),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ResearchSourceResponse>(created.Value);
        Assert.Equal(project.Id, response.ResearchProjectId);
        Assert.Equal(workspace.Id, response.WorkspaceId);
        Assert.Equal("https://example.com/article", response.Url);
        Assert.Equal("Example article", response.Title);
        Assert.Equal("{\"author\":\"Test\"}", response.MetadataJson);
        Assert.Single(db.ResearchSources);
    }

    [Fact]
    public async Task AddSource_rejects_project_from_another_workspace()
    {
        await using var db = CreateDb();
        var owner = new Workspace { Name = "Owner" };
        var other = new Workspace { Name = "Other" };
        var opportunity = new Opportunity { WorkspaceId = owner.Id, Title = "Private", OpportunityScore = 50, RevenueScore = 50 };
        var project = new ResearchProject { WorkspaceId = owner.Id, OpportunityId = opportunity.Id };
        db.Workspaces.AddRange(owner, other);
        db.Opportunities.Add(opportunity);
        db.ResearchProjects.Add(project);
        await db.SaveChangesAsync();

        var controller = new ResearchProjectsController(db);
        var result = await controller.AddSource(
            project.Id,
            new CreateResearchSourceRequest(other.Id, project.Id, "https://example.com/private", "Private", null),
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Empty(db.ResearchSources);
    }

    [Fact]
    public async Task AddSource_rejects_non_http_url()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Creator workspace" };
        var opportunity = new Opportunity { WorkspaceId = workspace.Id, Title = "AI trends", OpportunityScore = 80, RevenueScore = 70 };
        var project = new ResearchProject { WorkspaceId = workspace.Id, OpportunityId = opportunity.Id };
        db.Workspaces.Add(workspace);
        db.Opportunities.Add(opportunity);
        db.ResearchProjects.Add(project);
        await db.SaveChangesAsync();

        var controller = new ResearchProjectsController(db);
        var result = await controller.AddSource(
            project.Id,
            new CreateResearchSourceRequest(workspace.Id, project.Id, "ftp://example.com/article", "Example", null),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.ResearchSources);
    }

    [Fact]
    public async Task GetSources_returns_only_sources_for_project_and_workspace()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Creator workspace" };
        var other = new Workspace { Name = "Other" };
        var opportunity = new Opportunity { WorkspaceId = workspace.Id, Title = "First", OpportunityScore = 70, RevenueScore = 70 };
        var otherOpportunity = new Opportunity { WorkspaceId = other.Id, Title = "Second", OpportunityScore = 60, RevenueScore = 60 };
        var project = new ResearchProject { WorkspaceId = workspace.Id, OpportunityId = opportunity.Id };
        var otherProject = new ResearchProject { WorkspaceId = other.Id, OpportunityId = otherOpportunity.Id };
        db.Workspaces.AddRange(workspace, other);
        db.Opportunities.AddRange(opportunity, otherOpportunity);
        db.ResearchProjects.AddRange(project, otherProject);
        db.ResearchSources.AddRange(
            new ResearchSource { WorkspaceId = workspace.Id, ResearchProjectId = project.Id, Url = "https://example.com/one", Title = "One" },
            new ResearchSource { WorkspaceId = other.Id, ResearchProjectId = otherProject.Id, Url = "https://example.com/two", Title = "Two" });
        await db.SaveChangesAsync();

        var controller = new ResearchProjectsController(db);
        var result = await controller.GetSources(project.Id, workspace.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var sources = Assert.IsAssignableFrom<IReadOnlyList<ResearchSourceResponse>>(response.Value);
        Assert.Single(sources);
        Assert.Equal("https://example.com/one", sources[0].Url);
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }
}
