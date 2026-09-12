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

public sealed class ResearchClaimsControllerTests
{
    [Fact]
    public async Task Create_persists_claim_with_multiple_evidence_links()
    {
        await using var db = CreateDb();
        var (workspace, project, evidenceA, evidenceB) = await AddResearchAsync(db);
        var controller = new ResearchClaimsController(db);

        var result = await controller.Create(project.Id,
            new CreateResearchClaimRequest(workspace.Id, "AI adoption is accelerating", [evidenceA.Id, evidenceB.Id]), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ResearchClaimResponse>(created.Value);
        Assert.Equal("unverified", response.VerificationStatus);
        Assert.Equal(2, response.EvidenceIds.Count);
        Assert.Equal(2, await db.ResearchClaimEvidence.CountAsync());
    }

    [Fact]
    public async Task Create_rejects_evidence_from_another_workspace()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence, _) = await AddResearchAsync(db);
        var (otherWorkspace, _, otherEvidence, _) = await AddResearchAsync(db, "Other");
        var controller = new ResearchClaimsController(db);

        var result = await controller.Create(project.Id,
            new CreateResearchClaimRequest(workspace.Id, "Claim", [evidence.Id, otherEvidence.Id]), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.ResearchClaims);
        _ = otherWorkspace;
    }

    [Fact]
    public async Task Create_rejects_invalid_metadata_json()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence, _) = await AddResearchAsync(db);
        var controller = new ResearchClaimsController(db);

        var result = await controller.Create(project.Id,
            new CreateResearchClaimRequest(workspace.Id, "Claim", [evidence.Id], MetadataJson: "not-json"), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("MetadataJson must be a valid JSON object.", badRequest.Value);
        Assert.Empty(db.ResearchClaims);
    }

    [Fact]
    public async Task Create_rejects_non_object_metadata_json()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence, _) = await AddResearchAsync(db);
        var controller = new ResearchClaimsController(db);

        var result = await controller.Create(project.Id,
            new CreateResearchClaimRequest(workspace.Id, "Claim", [evidence.Id], MetadataJson: "[]"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.ResearchClaims);
    }

    [Fact]
    public async Task Research_claim_evidence_is_a_pure_composite_join_entity()
    {
        await using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(ResearchClaimEvidence));

        Assert.NotNull(entity);
        Assert.Equal(
            new[] { nameof(ResearchClaimEvidence.ResearchClaimId), nameof(ResearchClaimEvidence.ResearchEvidenceId) },
            entity!.FindPrimaryKey()!.Properties.Select(property => property.Name).ToArray());
        Assert.Null(entity.FindProperty("Id"));
        Assert.Null(entity.FindProperty("CreatedAtUtc"));
        Assert.Null(entity.FindProperty("UpdatedAtUtc"));
    }

    [Fact]
    public async Task Verification_update_is_workspace_scoped()
    {
        await using var db = CreateDb();
        var (workspace, project, evidence, _) = await AddResearchAsync(db);
        var controller = new ResearchClaimsController(db);
        var create = await controller.Create(project.Id,
            new CreateResearchClaimRequest(workspace.Id, "Claim", [evidence.Id]), CancellationToken.None);
        var created = Assert.IsType<CreatedAtActionResult>(create.Result);
        var claim = Assert.IsType<ResearchClaimResponse>(created.Value);

        var wrong = await controller.UpdateVerification(project.Id, claim.Id,
            new UpdateResearchClaimVerificationRequest(Guid.NewGuid(), "verified"), CancellationToken.None);
        var updated = await controller.UpdateVerification(project.Id, claim.Id,
            new UpdateResearchClaimVerificationRequest(workspace.Id, "verified"), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(wrong.Result);
        Assert.Equal("verified", Assert.IsType<ResearchClaimResponse>(Assert.IsType<OkObjectResult>(updated.Result).Value).VerificationStatus);
    }

    private static async Task<(Workspace Workspace, ResearchProject Project, ResearchEvidence EvidenceA, ResearchEvidence EvidenceB)> AddResearchAsync(YoutubeStudioDbContext db, string workspaceName = "Creator workspace")
    {
        var workspace = new Workspace { Name = workspaceName };
        var opportunity = new Opportunity { WorkspaceId = workspace.Id, Title = "AI trends", OpportunityScore = 80, RevenueScore = 70 };
        var project = new ResearchProject { WorkspaceId = workspace.Id, OpportunityId = opportunity.Id };
        var source = new ResearchSource { WorkspaceId = workspace.Id, ResearchProjectId = project.Id, Url = "https://example.com/research", Title = "Primary source" };
        var evidenceA = new ResearchEvidence { WorkspaceId = workspace.Id, ResearchSourceId = source.Id, Quote = "Evidence A" };
        var evidenceB = new ResearchEvidence { WorkspaceId = workspace.Id, ResearchSourceId = source.Id, Quote = "Evidence B" };
        db.Workspaces.Add(workspace); db.Opportunities.Add(opportunity); db.ResearchProjects.Add(project); db.ResearchSources.Add(source); db.ResearchEvidence.AddRange(evidenceA, evidenceB);
        await db.SaveChangesAsync();
        return (workspace, project, evidenceA, evidenceB);
    }

    private static YoutubeStudioDbContext CreateDb() => new(new DbContextOptionsBuilder<YoutubeStudioDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
