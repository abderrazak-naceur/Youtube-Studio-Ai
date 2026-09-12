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

public sealed class ResearchSourcesControllerTests
{
    [Fact]
    public async Task Create_persists_source_for_its_research_project()
    {
        await using var db = CreateDb();
        var (workspace, project) = await AddProjectAsync(db);
        var controller = new ResearchSourcesController(db);

        var result = await controller.Create(project.Id,
            new CreateResearchSourceRequest(workspace.Id, "https://example.com/research", "Primary research", "{\"author\":\"Team\"}"),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var source = Assert.IsType<ResearchSourceResponse>(created.Value);
        Assert.Equal(project.Id, source.ResearchProjectId);
        Assert.Equal(workspace.Id, source.WorkspaceId);
        Assert.Equal("Primary research", source.Title);
        Assert.Single(db.ResearchSources);
    }

    [Fact]
    public async Task Create_rejects_invalid_url_and_metadata()
    {
        await using var db = CreateDb();
        var (workspace, project) = await AddProjectAsync(db);
        var controller = new ResearchSourcesController(db);

        var invalidUrl = await controller.Create(project.Id,
            new CreateResearchSourceRequest(workspace.Id, "not-a-url", "Source"), CancellationToken.None);
        var invalidMetadata = await controller.Create(project.Id,
            new CreateResearchSourceRequest(workspace.Id, "https://example.com", "Source", "not-json"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(invalidUrl.Result);
        Assert.IsType<BadRequestObjectResult>(invalidMetadata.Result);
        Assert.Empty(db.ResearchSources);
    }

    [Fact]
    public async Task GetAll_and_update_are_workspace_scoped()
    {
        await using var db = CreateDb();
        var (workspace, project) = await AddProjectAsync(db);
        var (otherWorkspace, otherProject) = await AddProjectAsync(db, "Other");
        var source = new ResearchSource
        {
            WorkspaceId = workspace.Id,
            ResearchProjectId = project.Id,
            Url = "https://example.com/original",
            Title = "Original"
        };
        var otherSource = new ResearchSource
        {
            WorkspaceId = otherWorkspace.Id,
            ResearchProjectId = otherProject.Id,
            Url = "https://example.org/private",
            Title = "Private"
        };
        db.ResearchSources.AddRange(source, otherSource);
        await db.SaveChangesAsync();
        var controller = new ResearchSourcesController(db);

        var listed = await controller.GetAll(project.Id, workspace.Id, CancellationToken.None);
        var updateOutsideWorkspace = await controller.Update(project.Id, source.Id,
            new UpdateResearchSourceRequest(otherWorkspace.Id, "https://example.com/changed", "Changed"), CancellationToken.None);
        var update = await controller.Update(project.Id, source.Id,
            new UpdateResearchSourceRequest(workspace.Id, "https://example.com/changed", "Changed", "{}"), CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(listed.Result);
        var sources = Assert.IsAssignableFrom<IReadOnlyList<ResearchSourceResponse>>(response.Value);
        Assert.Single(sources);
        Assert.Equal(source.Id, sources[0].Id);
        Assert.IsType<NotFoundObjectResult>(updateOutsideWorkspace.Result);
        Assert.Equal("Changed", Assert.IsType<ResearchSourceResponse>(Assert.IsType<OkObjectResult>(update.Result).Value).Title);
        Assert.Equal("https://example.com/changed", source.Url);
    }

    [Fact]
    public async Task Delete_removes_source_only_from_its_workspace_and_project()
    {
        await using var db = CreateDb();
        var (workspace, project) = await AddProjectAsync(db);
        var source = new ResearchSource
        {
            WorkspaceId = workspace.Id,
            ResearchProjectId = project.Id,
            Url = "https://example.com/source",
            Title = "Source"
        };
        db.ResearchSources.Add(source);
        await db.SaveChangesAsync();
        var controller = new ResearchSourcesController(db);

        var wrongWorkspace = await controller.Delete(project.Id, source.Id, Guid.NewGuid(), CancellationToken.None);
        var deleted = await controller.Delete(project.Id, source.Id, workspace.Id, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(wrongWorkspace);
        Assert.IsType<NoContentResult>(deleted);
        Assert.Empty(db.ResearchSources);
    }

    private static async Task<(Workspace Workspace, ResearchProject Project)> AddProjectAsync(YoutubeStudioDbContext db, string workspaceName = "Creator workspace")
    {
        var workspace = new Workspace { Name = workspaceName };
        var opportunity = new Opportunity { WorkspaceId = workspace.Id, Title = "AI trends", OpportunityScore = 80, RevenueScore = 70 };
        var project = new ResearchProject { WorkspaceId = workspace.Id, OpportunityId = opportunity.Id };
        db.Workspaces.Add(workspace);
        db.Opportunities.Add(opportunity);
        db.ResearchProjects.Add(project);
        await db.SaveChangesAsync();
        return (workspace, project);
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }
}
