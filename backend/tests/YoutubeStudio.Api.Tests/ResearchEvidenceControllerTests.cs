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

public sealed class ResearchEvidenceControllerTests
{
    [Fact]
    public async Task Create_persists_evidence_for_its_source()
    {
        await using var db = CreateDb();
        var (workspace, project, source) = await AddSourceAsync(db);
        var controller = new ResearchEvidenceController(db);

        var result = await controller.Create(project.Id, source.Id,
            new CreateResearchEvidenceRequest(workspace.Id, source.Id, "A precise factual quote", "p. 12", "Surrounding context"), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ResearchEvidenceResponse>(created.Value);
        Assert.Equal(workspace.Id, response.WorkspaceId);
        Assert.Equal(source.Id, response.ResearchSourceId);
        Assert.Equal("A precise factual quote", response.Quote);
        Assert.Single(db.ResearchEvidence);
    }

    [Fact]
    public async Task GetAll_isolated_by_workspace_and_source()
    {
        await using var db = CreateDb();
        var (workspace, project, source) = await AddSourceAsync(db);
        var (otherWorkspace, otherProject, otherSource) = await AddSourceAsync(db, "Other");
        db.ResearchEvidence.AddRange(
            new ResearchEvidence { WorkspaceId = workspace.Id, ResearchSourceId = source.Id, Quote = "Visible" },
            new ResearchEvidence { WorkspaceId = otherWorkspace.Id, ResearchSourceId = otherSource.Id, Quote = "Private" });
        await db.SaveChangesAsync();
        var controller = new ResearchEvidenceController(db);

        var listed = await controller.GetAll(project.Id, source.Id, workspace.Id, CancellationToken.None);
        var wrongWorkspace = await controller.GetAll(project.Id, source.Id, otherWorkspace.Id, CancellationToken.None);
        var wrongProject = await controller.GetAll(otherProject.Id, source.Id, workspace.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(listed.Result);
        var evidence = Assert.IsAssignableFrom<IReadOnlyList<ResearchEvidenceResponse>>(response.Value);
        Assert.Single(evidence);
        Assert.Equal("Visible", evidence[0].Quote);
        Assert.IsType<NotFoundObjectResult>(wrongWorkspace.Result);
        Assert.IsType<NotFoundObjectResult>(wrongProject.Result);
    }

    [Fact]
    public async Task Create_rejects_empty_quote_and_cross_workspace_source()
    {
        await using var db = CreateDb();
        var (workspace, project, source) = await AddSourceAsync(db);
        var controller = new ResearchEvidenceController(db);

        var empty = await controller.Create(project.Id, source.Id,
            new CreateResearchEvidenceRequest(workspace.Id, source.Id, " "), CancellationToken.None);
        var crossWorkspace = await controller.Create(project.Id, source.Id,
            new CreateResearchEvidenceRequest(Guid.NewGuid(), source.Id, "Quote"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(empty.Result);
        Assert.IsType<NotFoundObjectResult>(crossWorkspace.Result);
        Assert.Empty(db.ResearchEvidence);
    }

    private static async Task<(Workspace Workspace, ResearchProject Project, ResearchSource Source)> AddSourceAsync(YoutubeStudioDbContext db, string workspaceName = "Creator workspace")
    {
        var workspace = new Workspace { Name = workspaceName };
        var opportunity = new Opportunity { WorkspaceId = workspace.Id, Title = "AI trends", OpportunityScore = 80, RevenueScore = 70 };
        var project = new ResearchProject { WorkspaceId = workspace.Id, OpportunityId = opportunity.Id };
        var source = new ResearchSource { WorkspaceId = workspace.Id, ResearchProjectId = project.Id, Url = "https://example.com/research", Title = "Primary source" };
        db.Workspaces.Add(workspace);
        db.Opportunities.Add(opportunity);
        db.ResearchProjects.Add(project);
        db.ResearchSources.Add(source);
        await db.SaveChangesAsync();
        return (workspace, project, source);
    }

    private static YoutubeStudioDbContext CreateDb() => new(new DbContextOptionsBuilder<YoutubeStudioDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
