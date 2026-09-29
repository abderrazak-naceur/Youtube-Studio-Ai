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

public sealed class FactChecksControllerTests
{
    [Fact]
    public async Task Run_passes_when_every_claim_is_verified_with_evidence_and_low_risk()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence) = await AddResearchAsync(db);
        await AddClaimAsync(db, workspace.Id, project.Id, "Editors save time when they batch record", "verified", evidence.Id);
        var controller = new FactChecksController(db);

        var result = await controller.Run(project.Id, new RunFactCheckRequest(workspace.Id), CancellationToken.None);

        var response = Assert.IsType<FactCheckReportResponse>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
        Assert.Equal("passed", response.Verdict);
        Assert.False(response.RequiresHumanReview);
        Assert.Equal(1, response.ClaimCount);
        Assert.Equal(1, response.VerifiedClaimCount);
        Assert.Equal(0, response.UnsupportedClaimCount);
        var finding = Assert.Single(response.Findings);
        Assert.Equal("supported", finding.Status);
        Assert.Equal("low", finding.RiskLevel);
        Assert.Equal(1, finding.EvidenceCount);
        Assert.Single(db.FactCheckReports);
    }

    [Fact]
    public async Task Run_fails_when_a_claim_is_disputed()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence) = await AddResearchAsync(db);
        await AddClaimAsync(db, workspace.Id, project.Id, "Batch recording helps", "verified", evidence.Id);
        await AddClaimAsync(db, workspace.Id, project.Id, "Contested statement", "disputed", evidence.Id);
        var controller = new FactChecksController(db);

        var response = Assert.IsType<FactCheckReportResponse>(Assert.IsType<CreatedAtActionResult>(
            (await controller.Run(project.Id, new RunFactCheckRequest(workspace.Id), CancellationToken.None)).Result).Value);

        Assert.Equal("failed", response.Verdict);
        Assert.Equal(1, response.DisputedClaimCount);
        Assert.Contains(response.Findings, f => f.Status == "disputed");
    }

    [Fact]
    public async Task Run_fails_when_a_verified_claim_has_no_evidence()
    {
        await using var db = CreateDb();
        var (workspace, project, _) = await AddResearchAsync(db);
        await AddClaimAsync(db, workspace.Id, project.Id, "Verified but unsupported", "verified", evidenceId: null);
        var controller = new FactChecksController(db);

        var response = Assert.IsType<FactCheckReportResponse>(Assert.IsType<CreatedAtActionResult>(
            (await controller.Run(project.Id, new RunFactCheckRequest(workspace.Id), CancellationToken.None)).Result).Value);

        Assert.Equal("failed", response.Verdict);
        Assert.Equal(1, response.UnsupportedClaimCount);
        Assert.Contains(response.Findings, f => f.Status == "unsupported");
    }

    [Fact]
    public async Task Run_needs_review_when_a_claim_is_still_unverified()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence) = await AddResearchAsync(db);
        await AddClaimAsync(db, workspace.Id, project.Id, "Backed statement", "verified", evidence.Id);
        await AddClaimAsync(db, workspace.Id, project.Id, "Needs review", "unverified", evidence.Id);
        var controller = new FactChecksController(db);

        var response = Assert.IsType<FactCheckReportResponse>(Assert.IsType<CreatedAtActionResult>(
            (await controller.Run(project.Id, new RunFactCheckRequest(workspace.Id), CancellationToken.None)).Result).Value);

        Assert.Equal("needs_review", response.Verdict);
        Assert.Equal(1, response.UnverifiedClaimCount);
        Assert.Contains(response.Findings, f => f.Status == "pending");
    }

    [Fact]
    public async Task Run_flags_high_risk_domains_for_human_review()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence) = await AddResearchAsync(db);
        await AddClaimAsync(db, workspace.Id, project.Id, "This investment strategy doubles your stock returns", "verified", evidence.Id);
        var controller = new FactChecksController(db);

        var response = Assert.IsType<FactCheckReportResponse>(Assert.IsType<CreatedAtActionResult>(
            (await controller.Run(project.Id, new RunFactCheckRequest(workspace.Id), CancellationToken.None)).Result).Value);

        Assert.Equal("needs_review", response.Verdict);
        Assert.True(response.RequiresHumanReview);
        var finding = Assert.Single(response.Findings);
        Assert.Equal("high", finding.RiskLevel);
        Assert.True(finding.RequiresHumanReview);
    }

    [Fact]
    public async Task Run_rejects_when_project_has_no_claims()
    {
        await using var db = CreateDb();
        var (workspace, project, _) = await AddResearchAsync(db);
        var controller = new FactChecksController(db);

        var result = await controller.Run(project.Id, new RunFactCheckRequest(workspace.Id), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("At least one research claim is required before a fact check can run.", badRequest.Value);
        Assert.Empty(db.FactCheckReports);
    }

    [Fact]
    public async Task Run_is_workspace_scoped()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence) = await AddResearchAsync(db);
        await AddClaimAsync(db, workspace.Id, project.Id, "Backed statement", "verified", evidence.Id);
        var controller = new FactChecksController(db);

        var result = await controller.Run(project.Id, new RunFactCheckRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Empty(db.FactCheckReports);
    }

    [Fact]
    public async Task Run_replaces_findings_when_executed_again()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence) = await AddResearchAsync(db);
        var claim = await AddClaimAsync(db, workspace.Id, project.Id, "Needs review", "unverified", evidence.Id);
        var controller = new FactChecksController(db);
        await controller.Run(project.Id, new RunFactCheckRequest(workspace.Id), CancellationToken.None);

        claim.VerificationStatus = "verified";
        await db.SaveChangesAsync();
        var response = Assert.IsType<FactCheckReportResponse>(Assert.IsType<CreatedAtActionResult>(
            (await controller.Run(project.Id, new RunFactCheckRequest(workspace.Id), CancellationToken.None)).Result).Value);

        Assert.Equal("passed", response.Verdict);
        Assert.Single(db.FactCheckReports);
        Assert.Equal(1, await db.FactCheckFindings.CountAsync());
    }

    [Fact]
    public async Task Get_returns_the_saved_report_for_the_workspace()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence) = await AddResearchAsync(db);
        await AddClaimAsync(db, workspace.Id, project.Id, "Backed statement", "verified", evidence.Id);
        var controller = new FactChecksController(db);
        await controller.Run(project.Id, new RunFactCheckRequest(workspace.Id), CancellationToken.None);

        var found = await controller.Get(project.Id, workspace.Id, CancellationToken.None);
        var response = Assert.IsType<FactCheckReportResponse>(Assert.IsType<OkObjectResult>(found.Result).Value);
        Assert.Equal("passed", response.Verdict);

        var missing = await controller.Get(project.Id, Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(missing.Result);
    }

    [Fact]
    public async Task Get_returns_not_found_before_a_fact_check_runs()
    {
        await using var db = CreateDb();
        var (workspace, project, _) = await AddResearchAsync(db);
        var controller = new FactChecksController(db);

        var result = await controller.Get(project.Id, workspace.Id, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private static async Task<ResearchClaim> AddClaimAsync(YoutubeStudioDbContext db, Guid workspaceId, Guid projectId, string text, string verificationStatus, Guid? evidenceId)
    {
        var claim = new ResearchClaim { WorkspaceId = workspaceId, ResearchProjectId = projectId, Text = text, VerificationStatus = verificationStatus };
        db.ResearchClaims.Add(claim);
        if (evidenceId is { } id) db.ResearchClaimEvidence.Add(new ResearchClaimEvidence { ResearchClaimId = claim.Id, ResearchEvidenceId = id });
        await db.SaveChangesAsync();
        return claim;
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
