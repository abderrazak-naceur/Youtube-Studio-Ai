using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class ResearchBriefsControllerTests
{
    [Fact]
    public async Task Save_persists_an_evidence_backed_brief_and_completes_research()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence) = await AddResearchAsync(db);
        var claim = new ResearchClaim { WorkspaceId = workspace.Id, ResearchProjectId = project.Id, Text = "Supported fact", VerificationStatus = "verified" };
        db.ResearchClaims.Add(claim);
        db.ResearchClaimEvidence.Add(new ResearchClaimEvidence { ResearchClaimId = claim.Id, ResearchEvidenceId = evidence.Id });
        await db.SaveChangesAsync();
        var controller = new ResearchBriefsController(db, new StubWorkspaceAccess()).WithUser();

        var result = await controller.Save(project.Id, new SaveResearchBriefRequest(workspace.Id), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ResearchBriefResponse>(created.Value);
        Assert.Equal("published", response.Status);
        Assert.Equal(0, response.PendingClaimCount);
        Assert.Contains("Supported fact", response.Markdown);
        Assert.Contains("A source-backed excerpt.", response.Markdown);
        Assert.Contains("Official report", response.Markdown);
        Assert.Equal("complete", (await db.ResearchProjects.SingleAsync()).Status);
        Assert.Single(db.ResearchBriefs);
    }

    [Fact]
    public async Task Save_keeps_research_draft_when_claims_still_need_review()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence) = await AddResearchAsync(db);
        var verified = new ResearchClaim { WorkspaceId = workspace.Id, ResearchProjectId = project.Id, Text = "Ready claim", VerificationStatus = "verified" };
        var pending = new ResearchClaim { WorkspaceId = workspace.Id, ResearchProjectId = project.Id, Text = "Needs review", VerificationStatus = "unverified" };
        db.ResearchClaims.AddRange(verified, pending);
        db.ResearchClaimEvidence.Add(new ResearchClaimEvidence { ResearchClaimId = verified.Id, ResearchEvidenceId = evidence.Id });
        await db.SaveChangesAsync();
        var controller = new ResearchBriefsController(db, new StubWorkspaceAccess()).WithUser();

        var result = await controller.Save(project.Id, new SaveResearchBriefRequest(workspace.Id), CancellationToken.None);

        var response = Assert.IsType<ResearchBriefResponse>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
        Assert.Equal("draft", response.Status);
        Assert.Equal(1, response.PendingClaimCount);
        Assert.Contains("1 claim still require review.", response.Markdown);
        Assert.Equal("draft", (await db.ResearchProjects.SingleAsync()).Status);
    }

    [Fact]
    public async Task Save_rejects_when_no_verified_claim_exists()
    {
        await using var db = CreateDb();
        var (workspace, project, _) = await AddResearchAsync(db);
        db.ResearchClaims.Add(new ResearchClaim { WorkspaceId = workspace.Id, ResearchProjectId = project.Id, Text = "Unverified", VerificationStatus = "unverified" });
        await db.SaveChangesAsync();
        var controller = new ResearchBriefsController(db, new StubWorkspaceAccess()).WithUser();

        var result = await controller.Save(project.Id, new SaveResearchBriefRequest(workspace.Id), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("At least one verified claim is required before a production brief can be saved.", badRequest.Value);
        Assert.Empty(db.ResearchBriefs);
    }

    [Fact]
    public async Task Save_is_workspace_scoped()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence) = await AddResearchAsync(db);
        var claim = new ResearchClaim { WorkspaceId = workspace.Id, ResearchProjectId = project.Id, Text = "Supported fact", VerificationStatus = "verified" };
        db.ResearchClaims.Add(claim);
        db.ResearchClaimEvidence.Add(new ResearchClaimEvidence { ResearchClaimId = claim.Id, ResearchEvidenceId = evidence.Id });
        await db.SaveChangesAsync();
        var controller = new ResearchBriefsController(db, new StubWorkspaceAccess()).WithUser();

        var result = await controller.Save(project.Id, new SaveResearchBriefRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Empty(db.ResearchBriefs);
    }

    [Fact]
    public async Task Get_returns_saved_brief_for_workspace()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence) = await AddResearchAsync(db);
        var claim = new ResearchClaim { WorkspaceId = workspace.Id, ResearchProjectId = project.Id, Text = "Supported fact", VerificationStatus = "verified" };
        db.ResearchClaims.Add(claim);
        db.ResearchClaimEvidence.Add(new ResearchClaimEvidence { ResearchClaimId = claim.Id, ResearchEvidenceId = evidence.Id });
        await db.SaveChangesAsync();
        var controller = new ResearchBriefsController(db, new StubWorkspaceAccess()).WithUser();
        await controller.Save(project.Id, new SaveResearchBriefRequest(workspace.Id), CancellationToken.None);

        var result = await controller.Get(project.Id, workspace.Id, CancellationToken.None);

        var response = Assert.IsType<ResearchBriefResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Contains("Supported fact", response.Markdown);
        Assert.IsType<NotFoundObjectResult>((await controller.Get(project.Id, Guid.NewGuid(), CancellationToken.None)).Result);
    }

    private static async Task<(Workspace Workspace, ResearchProject Project, ResearchEvidence Evidence)> AddResearchAsync(YoutubeStudioDbContext db)
    {
        var workspace = new Workspace { Name = "Creator workspace" };
        var opportunity = new Opportunity { WorkspaceId = workspace.Id, Title = "Creator planning", OpportunityScore = 80, RevenueScore = 70 };
        var project = new ResearchProject { WorkspaceId = workspace.Id, OpportunityId = opportunity.Id };
        var source = new ResearchSource { WorkspaceId = workspace.Id, ResearchProjectId = project.Id, Url = "https://example.com/report", Title = "Official report" };
        var evidence = new ResearchEvidence { WorkspaceId = workspace.Id, ResearchSourceId = source.Id, Quote = "A source-backed excerpt.", Locator = "p. 4" };
        db.Workspaces.Add(workspace);
        db.Opportunities.Add(opportunity);
        db.ResearchProjects.Add(project);
        db.ResearchSources.Add(source);
        db.ResearchEvidence.Add(evidence);
        await db.SaveChangesAsync();
        return (workspace, project, evidence);
    }

    private static YoutubeStudioDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<YoutubeStudioDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
