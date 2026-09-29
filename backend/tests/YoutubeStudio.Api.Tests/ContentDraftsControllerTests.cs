using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class ContentDraftsControllerTests
{
    [Fact]
    public async Task Generate_produces_a_ready_draft_when_fact_check_passed_without_review()
    {
        await using var db = CreateDb();
        var (workspace, project) = await AddResearchAsync(db, verdict: "passed", requiresReview: false);
        var controller = new ContentDraftsController(db);

        var result = await controller.Generate(project.Id, new GenerateContentDraftRequest(workspace.Id), CancellationToken.None);

        var response = Assert.IsType<ContentDraftResponse>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
        Assert.Equal("ready", response.Status);
        Assert.Contains("Creator planning", response.Angle);
        Assert.NotEmpty(response.Hook);
        Assert.NotEmpty(response.Outline);
        Assert.Contains("Editors save time by batching", response.Script);
        Assert.NotEmpty(response.TitleCandidates);
        Assert.NotEmpty(response.ThumbnailConcepts);
        Assert.Contains("Official report", response.Description);
        Assert.NotEmpty(response.Chapters);
        Assert.Equal("0:00", response.Chapters[0].Timestamp);
        Assert.NotEmpty(response.Tags);
        Assert.Single(db.ContentDrafts);
    }

    [Fact]
    public async Task Generate_keeps_draft_status_when_fact_check_needs_review()
    {
        await using var db = CreateDb();
        var (workspace, project) = await AddResearchAsync(db, verdict: "needs_review", requiresReview: true);
        var controller = new ContentDraftsController(db);

        var response = Assert.IsType<ContentDraftResponse>(Assert.IsType<CreatedAtActionResult>(
            (await controller.Generate(project.Id, new GenerateContentDraftRequest(workspace.Id), CancellationToken.None)).Result).Value);

        Assert.Equal("draft", response.Status);
    }

    [Fact]
    public async Task Generate_rejects_when_no_fact_check_exists()
    {
        await using var db = CreateDb();
        var (workspace, project) = await AddResearchAsync(db, addFactCheck: false);
        var controller = new ContentDraftsController(db);

        var result = await controller.Generate(project.Id, new GenerateContentDraftRequest(workspace.Id), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Run a fact check before generating content.", badRequest.Value);
        Assert.Empty(db.ContentDrafts);
    }

    [Fact]
    public async Task Generate_rejects_when_fact_check_verdict_is_failed()
    {
        await using var db = CreateDb();
        var (workspace, project) = await AddResearchAsync(db, verdict: "failed", requiresReview: false);
        var controller = new ContentDraftsController(db);

        var result = await controller.Generate(project.Id, new GenerateContentDraftRequest(workspace.Id), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Content cannot be generated while the fact check verdict is failed.", badRequest.Value);
        Assert.Empty(db.ContentDrafts);
    }

    [Fact]
    public async Task Generate_rejects_when_there_are_no_verified_claims()
    {
        await using var db = CreateDb();
        var (workspace, project) = await AddResearchAsync(db, verdict: "passed", requiresReview: false, verifyClaim: false);
        var controller = new ContentDraftsController(db);

        var result = await controller.Generate(project.Id, new GenerateContentDraftRequest(workspace.Id), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("At least one verified claim is required before content can be generated.", badRequest.Value);
        Assert.Empty(db.ContentDrafts);
    }

    [Fact]
    public async Task Generate_is_workspace_scoped()
    {
        await using var db = CreateDb();
        var (_, project) = await AddResearchAsync(db, verdict: "passed", requiresReview: false);
        var controller = new ContentDraftsController(db);

        var result = await controller.Generate(project.Id, new GenerateContentDraftRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Empty(db.ContentDrafts);
    }

    [Fact]
    public async Task Generate_updates_the_existing_draft_when_run_again()
    {
        await using var db = CreateDb();
        var (workspace, project) = await AddResearchAsync(db, verdict: "needs_review", requiresReview: true);
        var controller = new ContentDraftsController(db);
        await controller.Generate(project.Id, new GenerateContentDraftRequest(workspace.Id), CancellationToken.None);

        var report = await db.FactCheckReports.SingleAsync();
        report.Verdict = "passed";
        report.RequiresHumanReview = false;
        await db.SaveChangesAsync();
        var response = Assert.IsType<ContentDraftResponse>(Assert.IsType<CreatedAtActionResult>(
            (await controller.Generate(project.Id, new GenerateContentDraftRequest(workspace.Id), CancellationToken.None)).Result).Value);

        Assert.Equal("ready", response.Status);
        Assert.Single(db.ContentDrafts);
    }

    [Fact]
    public async Task Get_returns_the_saved_draft_and_is_workspace_scoped()
    {
        await using var db = CreateDb();
        var (workspace, project) = await AddResearchAsync(db, verdict: "passed", requiresReview: false);
        var controller = new ContentDraftsController(db);
        await controller.Generate(project.Id, new GenerateContentDraftRequest(workspace.Id), CancellationToken.None);

        var found = await controller.Get(project.Id, workspace.Id, CancellationToken.None);
        var response = Assert.IsType<ContentDraftResponse>(Assert.IsType<OkObjectResult>(found.Result).Value);
        Assert.Equal("ready", response.Status);

        var missing = await controller.Get(project.Id, Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(missing.Result);
    }

    [Fact]
    public async Task Get_returns_not_found_before_content_is_generated()
    {
        await using var db = CreateDb();
        var (workspace, project) = await AddResearchAsync(db, verdict: "passed", requiresReview: false);
        var controller = new ContentDraftsController(db);

        var result = await controller.Get(project.Id, workspace.Id, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private static async Task<(Workspace Workspace, ResearchProject Project)> AddResearchAsync(
        YoutubeStudioDbContext db,
        string verdict = "passed",
        bool requiresReview = false,
        bool addFactCheck = true,
        bool verifyClaim = true)
    {
        var workspace = new Workspace { Name = "Creator workspace" };
        var opportunity = new Opportunity { WorkspaceId = workspace.Id, Title = "Creator planning", OpportunityScore = 80, RevenueScore = 70 };
        var project = new ResearchProject { WorkspaceId = workspace.Id, OpportunityId = opportunity.Id };
        var source = new ResearchSource { WorkspaceId = workspace.Id, ResearchProjectId = project.Id, Url = "https://example.com/report", Title = "Official report" };
        var claim = new ResearchClaim { WorkspaceId = workspace.Id, ResearchProjectId = project.Id, Text = "Editors save time by batching their recording sessions.", VerificationStatus = verifyClaim ? "verified" : "unverified" };
        db.Workspaces.Add(workspace);
        db.Opportunities.Add(opportunity);
        db.ResearchProjects.Add(project);
        db.ResearchSources.Add(source);
        db.ResearchClaims.Add(claim);
        if (addFactCheck)
        {
            db.FactCheckReports.Add(new FactCheckReport
            {
                WorkspaceId = workspace.Id,
                ResearchProjectId = project.Id,
                Verdict = verdict,
                ClaimCount = 1,
                VerifiedClaimCount = verifyClaim ? 1 : 0,
                RequiresHumanReview = requiresReview
            });
        }
        await db.SaveChangesAsync();
        return (workspace, project);
    }

    private static YoutubeStudioDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<YoutubeStudioDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
