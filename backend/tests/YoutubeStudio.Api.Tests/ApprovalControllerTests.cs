using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class ApprovalControllerTests
{
    private static ApprovalController CreateController(YoutubeStudioDbContext db) => new(db, new AuditService(db));
    [Fact]
    public async Task Approve_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var result = await CreateController(db)
            .Approve(Guid.NewGuid(), new ApprovalDecisionRequest("reviewer", null), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Approve_requires_reviewer()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.AwaitingApproval);

        var result = await CreateController(db)
            .Approve(project.Id, new ApprovalDecisionRequest("  ", null), CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
    }

    [Fact]
    public async Task Approve_requires_awaiting_approval_status()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);

        var result = await CreateController(db)
            .Approve(project.Id, new ApprovalDecisionRequest("reviewer", null), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("The video project is not awaiting approval.", conflict.Value);
    }

    [Fact]
    public async Task Approve_is_blocked_without_render_artifact()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.AwaitingApproval);
        AddQaArtifact(db, project.Id, passed: true);
        await db.SaveChangesAsync();

        var result = await CreateController(db)
            .Approve(project.Id, new ApprovalDecisionRequest("reviewer", null), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("A rendered video artifact is required before approval.", conflict.Value);
        Assert.Equal(VideoProjectStatus.AwaitingApproval, (await db.VideoProjects.SingleAsync(x => x.Id == project.Id)).Status);
    }

    [Fact]
    public async Task Approve_is_blocked_when_qa_failed()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.AwaitingApproval);
        AddRenderArtifact(db, project.Id);
        AddQaArtifact(db, project.Id, passed: false);
        await db.SaveChangesAsync();

        var result = await CreateController(db)
            .Approve(project.Id, new ApprovalDecisionRequest("reviewer", null), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("The project cannot be approved because automated QA did not pass.", conflict.Value);
        Assert.Equal(VideoProjectStatus.AwaitingApproval, (await db.VideoProjects.SingleAsync(x => x.Id == project.Id)).Status);
    }

    [Fact]
    public async Task Approve_completes_project_and_records_approval_artifact()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.AwaitingApproval);
        AddRenderArtifact(db, project.Id);
        AddQaArtifact(db, project.Id, passed: true);
        await db.SaveChangesAsync();

        var result = await CreateController(db)
            .Approve(project.Id, new ApprovalDecisionRequest("Alex", "Looks great"), CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApprovalResponse>(response.Value);
        Assert.Equal("Completed", payload.Status);
        Assert.Equal("approved", payload.Decision);

        Assert.Equal(VideoProjectStatus.Completed, (await db.VideoProjects.SingleAsync(x => x.Id == project.Id)).Status);
        var approval = await db.ProductionArtifacts.SingleAsync(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Approval);
        Assert.Contains("approved", approval.MetadataJson);
        Assert.Contains("Alex", approval.MetadataJson);
    }

    [Fact]
    public async Task Approve_records_immutable_audit_event()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.AwaitingApproval);
        AddRenderArtifact(db, project.Id);
        AddQaArtifact(db, project.Id, passed: true);
        await db.SaveChangesAsync();

        await CreateController(db)
            .Approve(project.Id, new ApprovalDecisionRequest("Alex", "Looks great"), CancellationToken.None);

        var auditEvent = await db.AuditEvents.SingleAsync(x => x.VideoProjectId == project.Id);
        Assert.Equal("video.approved", auditEvent.Action);
        Assert.Equal("Alex", auditEvent.Actor);
        Assert.Equal(project.WorkspaceId, auditEvent.WorkspaceId);
    }

    [Fact]
    public async Task Reject_records_audit_event()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.AwaitingApproval);
        AddRenderArtifact(db, project.Id);
        AddQaArtifact(db, project.Id, passed: true);
        await db.SaveChangesAsync();

        await CreateController(db)
            .Reject(project.Id, new ApprovalDecisionRequest("Alex", "Thumbnail misleading"), CancellationToken.None);

        var auditEvent = await db.AuditEvents.SingleAsync(x => x.VideoProjectId == project.Id);
        Assert.Equal("video.rejected", auditEvent.Action);
    }

    [Fact]
    public async Task Approve_extracts_content_genome()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.AwaitingApproval);
        project.Script = "Artificial intelligence helps creators plan smarter videos consistently.";
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Script,
            ProviderAssetId = "script",
            Content = "Artificial intelligence helps creators plan smarter videos consistently."
        });
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.ScenePlan,
            ProviderAssetId = "scene-plan",
            Content = JsonSerializer.Serialize(new[]
            {
                new ScenePlanItem(1, "First", "Opening", 8),
                new ScenePlanItem(2, "Second", "Middle", 12)
            })
        });
        AddRenderArtifact(db, project.Id);
        AddQaArtifact(db, project.Id, passed: true);
        await db.SaveChangesAsync();

        await CreateController(db)
            .Approve(project.Id, new ApprovalDecisionRequest("Alex", null), CancellationToken.None);

        var genome = await db.ContentGenomes.SingleAsync(x => x.VideoProjectId == project.Id);
        Assert.Equal(2, genome.SceneCount);
        Assert.Equal(20, genome.DurationSeconds);
        Assert.True(genome.WordCount >= 1);
        Assert.Contains("artificial", genome.AttributesJson);
    }

    [Fact]
    public async Task Reject_requires_notes()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.AwaitingApproval);

        var result = await CreateController(db)
            .Reject(project.Id, new ApprovalDecisionRequest("reviewer", "  "), CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
    }

    [Fact]
    public async Task Reject_moves_project_to_rejected_and_records_artifact()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.AwaitingApproval);
        AddRenderArtifact(db, project.Id);
        AddQaArtifact(db, project.Id, passed: true);
        await db.SaveChangesAsync();

        var result = await CreateController(db)
            .Reject(project.Id, new ApprovalDecisionRequest("Alex", "Thumbnail is misleading"), CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApprovalResponse>(response.Value);
        Assert.Equal("Rejected", payload.Status);
        Assert.Equal("rejected", payload.Decision);

        Assert.Equal(VideoProjectStatus.Rejected, (await db.VideoProjects.SingleAsync(x => x.Id == project.Id)).Status);
        var approval = await db.ProductionArtifacts.SingleAsync(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Approval);
        Assert.Equal("Thumbnail is misleading", approval.Content);
    }

    private static void AddRenderArtifact(YoutubeStudioDbContext db, Guid projectId) =>
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = projectId,
            Type = ProductionArtifactType.Render,
            ProviderAssetId = "render-1"
        });

    private static void AddQaArtifact(YoutubeStudioDbContext db, Guid projectId, bool passed) =>
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = projectId,
            Type = ProductionArtifactType.Qa,
            ProviderAssetId = "qa-result",
            Content = JsonSerializer.Serialize(new QaResult(passed, Array.Empty<string>()))
        });

    private static async Task<VideoProject> AddProject(YoutubeStudioDbContext db, VideoProjectStatus status)
    {
        var workspace = new Workspace { Name = "Test workspace" };
        var project = new VideoProject
        {
            WorkspaceId = workspace.Id,
            Prompt = "Create a video about AI",
            Title = "AI Explained",
            Status = status
        };
        db.Workspaces.Add(workspace);
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }
}
