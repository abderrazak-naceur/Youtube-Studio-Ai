using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Production;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class ScenePlanControllerTests
{
    [Fact]
    public async Task GenerateScenePlan_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var controller = CreateController(db, new RecordingScenePlanProvider());

        var result = await controller.GenerateScenePlan(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GenerateScenePlan_rejects_project_without_script()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Test workspace" };
        var project = new VideoProject { WorkspaceId = workspace.Id, Prompt = "Create a video about AI", Status = VideoProjectStatus.Draft };
        db.Workspaces.Add(workspace);
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();

        var result = await CreateController(db, new RecordingScenePlanProvider()).GenerateScenePlan(project.Id, CancellationToken.None);

        var validation = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(validation.Value);
        Assert.Equal("A title and script are required to generate a scene plan.", problem.Detail);
    }

    [Fact]
    public async Task GenerateScenePlan_requires_scripted_status()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Test workspace" };
        var project = new VideoProject
        {
            WorkspaceId = workspace.Id,
            Prompt = "Create a video about AI",
            Title = "AI Explained",
            Script = "HOOK: AI changes how creators work.",
            Status = VideoProjectStatus.Draft
        };
        db.Workspaces.Add(workspace);
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();

        var result = await CreateController(db, new RecordingScenePlanProvider()).GenerateScenePlan(project.Id, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("The video project must be scripted before generating a scene plan.", conflict.Value);
    }

    [Fact]
    public async Task GenerateScenePlan_persists_provider_scenes_and_marks_project_planned()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Test workspace" };
        var project = new VideoProject
        {
            WorkspaceId = workspace.Id,
            Prompt = "Create a video about AI",
            Title = "AI Explained",
            Script = "HOOK: AI changes how creators work.",
            Status = VideoProjectStatus.Scripted
        };
        db.Workspaces.Add(workspace);
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();

        var provider = new RecordingScenePlanProvider();
        var result = await CreateController(db, provider).GenerateScenePlan(project.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<VideoProjectResponse>(response.Value);
        Assert.Equal(nameof(VideoProjectStatus.Planned), payload.Status);
        Assert.Equal(project.Title, provider.Request?.Title);
        Assert.Equal(project.Script, provider.Request?.Script);

        var artifact = await db.ProductionArtifacts.SingleAsync(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.ScenePlan);
        Assert.Equal("scene-plan", artifact.ProviderAssetId);
        Assert.Contains("AI changes how creators work", artifact.Content);
        Assert.Contains("sceneCount", artifact.MetadataJson);
        Assert.Equal(VideoProjectStatus.Planned, (await db.VideoProjects.SingleAsync(x => x.Id == project.Id)).Status);
    }

    [Fact]
    public async Task GenerateScenePlan_returns_bad_gateway_and_preserves_project_when_provider_output_is_invalid()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Test workspace" };
        var project = new VideoProject
        {
            WorkspaceId = workspace.Id,
            Prompt = "Create a video about AI",
            Title = "AI Explained",
            Script = "HOOK: AI changes how creators work.",
            Status = VideoProjectStatus.Scripted
        };
        db.Workspaces.Add(workspace);
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();

        var result = await CreateController(db, new EmptyScenePlanProvider()).GenerateScenePlan(project.Id, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
        Assert.Equal("The scene-plan provider returned an invalid scene plan.", ((ProblemDetails)problem.Value!).Detail);
        Assert.Empty(await db.ProductionArtifacts.Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.ScenePlan).ToListAsync());
        Assert.Equal(VideoProjectStatus.Scripted, (await db.VideoProjects.SingleAsync(x => x.Id == project.Id)).Status);
    }

    private static VideoProjectsController CreateController(YoutubeStudioDbContext db, IScenePlanProvider provider) =>
        new(db, new ProductionJobService(db), new PlaceholderScriptProvider(), provider);

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }

    private sealed class RecordingScenePlanProvider : IScenePlanProvider
    {
        public ScenePlanRequest? Request { get; private set; }

        public Task<ScenePlanResult> CreateScenePlanAsync(ScenePlanRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new ScenePlanResult([
                new ScenePlanItem(1, "AI changes how creators work.", "Creator at a workstation with AI tools", 8),
                new ScenePlanItem(2, "Show the key takeaway.", "Clean closing visual", 6)
            ]));
        }
    }

    private sealed class EmptyScenePlanProvider : IScenePlanProvider
    {
        public Task<ScenePlanResult> CreateScenePlanAsync(ScenePlanRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new ScenePlanResult([]));
    }
}
