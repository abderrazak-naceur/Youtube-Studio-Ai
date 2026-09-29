using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Production;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class VideoProjectFromContentDraftTests
{
    [Fact]
    public async Task CreateFromContentDraft_seeds_a_project_from_a_ready_draft()
    {
        await using var db = CreateDb();
        var (workspace, draft) = await AddContentDraftAsync(db, status: "ready");
        var controller = CreateController(db);

        var result = await controller.CreateFromContentDraft(
            new CreateVideoProjectFromContentDraftRequest(workspace.Id, draft.Id), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<VideoProjectResponse>(created.Value);
        Assert.Equal("content_draft", response.Source);
        Assert.Equal(draft.Id, response.ContentDraftId);
        Assert.Equal(draft.Angle, response.Prompt);
        Assert.Equal(draft.Script, response.Script);
        var stored = await db.VideoProjects.SingleAsync();
        Assert.Equal(draft.Id, stored.ContentDraftId);
        Assert.Equal("content_draft", stored.Source);
    }

    [Fact]
    public async Task CreateFromContentDraft_rejects_a_draft_that_is_not_ready()
    {
        await using var db = CreateDb();
        var (workspace, draft) = await AddContentDraftAsync(db, status: "draft");
        var controller = CreateController(db);

        var result = await controller.CreateFromContentDraft(
            new CreateVideoProjectFromContentDraftRequest(workspace.Id, draft.Id), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Empty(db.VideoProjects);
    }

    [Fact]
    public async Task CreateFromContentDraft_is_workspace_scoped()
    {
        await using var db = CreateDb();
        var (_, draft) = await AddContentDraftAsync(db, status: "ready");
        var controller = CreateController(db);

        var result = await controller.CreateFromContentDraft(
            new CreateVideoProjectFromContentDraftRequest(Guid.NewGuid(), draft.Id), CancellationToken.None);

        // A non-existent workspace is rejected before the draft lookup.
        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.VideoProjects);
    }

    [Fact]
    public async Task CreateFromContentDraft_returns_not_found_for_a_missing_draft()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Creator workspace" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.CreateFromContentDraft(
            new CreateVideoProjectFromContentDraftRequest(workspace.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private static async Task<(Workspace Workspace, ContentDraft Draft)> AddContentDraftAsync(YoutubeStudioDbContext db, string status)
    {
        var workspace = new Workspace { Name = "Creator workspace" };
        var opportunity = new Opportunity { WorkspaceId = workspace.Id, Title = "Creator planning", OpportunityScore = 80, RevenueScore = 70 };
        var project = new ResearchProject { WorkspaceId = workspace.Id, OpportunityId = opportunity.Id };
        var draft = new ContentDraft
        {
            WorkspaceId = workspace.Id,
            ResearchProjectId = project.Id,
            Angle = "A clear, evidence-based explanation of creator planning.",
            Hook = "What most people get wrong about creator planning.",
            Script = "Here is what the evidence really says about creator planning.",
            Status = status
        };
        db.Workspaces.Add(workspace);
        db.Opportunities.Add(opportunity);
        db.ResearchProjects.Add(project);
        db.ContentDrafts.Add(draft);
        await db.SaveChangesAsync();
        return (workspace, draft);
    }

    private static VideoProjectsController CreateController(YoutubeStudioDbContext db) =>
        new(db, new StubProductionJobService(), new PlaceholderScriptProvider(), new PlaceholderScenePlanProvider());

    private static YoutubeStudioDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<YoutubeStudioDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class StubProductionJobService : IProductionJobService
    {
        public Task<ProductionJob> EnqueueAsync(VideoProject project, CancellationToken cancellationToken) =>
            Task.FromResult(new ProductionJob { VideoProjectId = project.Id });
    }
}
