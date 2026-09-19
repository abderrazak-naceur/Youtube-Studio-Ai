using System.Text.Json;
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

public sealed class VisualsControllerTests
{
    [Fact]
    public async Task GenerateVisuals_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var result = await CreateController(db, new RecordingVisualProvider())
            .GenerateVisuals(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GenerateVisuals_requires_planned_status()
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

        var result = await CreateController(db, new RecordingVisualProvider())
            .GenerateVisuals(project.Id, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("The video project must be planned before generating visuals.", conflict.Value);
    }

    [Fact]
    public async Task GenerateVisuals_requires_persisted_scene_plan()
    {
        await using var db = CreateDb();
        var project = await AddPlannedProject(db);

        var result = await CreateController(db, new RecordingVisualProvider())
            .GenerateVisuals(project.Id, CancellationToken.None);

        var validation = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(validation.Value);
        Assert.Equal("A persisted scene plan is required to generate visuals.", problem.Detail);
    }

    [Fact]
    public async Task GenerateVisuals_persists_one_visual_per_scene_and_marks_project_producing()
    {
        await using var db = CreateDb();
        var project = await AddPlannedProject(db);
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.ScenePlan,
            ProviderAssetId = "scene-plan",
            Content = JsonSerializer.Serialize(new[]
            {
                new ScenePlanItem(1, "Open with the hook.", "Creator at a workstation", 8),
                new ScenePlanItem(2, "Explain the idea.", "Clean product visualization", 12)
            })
        });
        await db.SaveChangesAsync();

        var provider = new RecordingVisualProvider();
        var result = await CreateController(db, provider).GenerateVisuals(project.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<VisualGenerationResponse>(response.Value);
        Assert.Equal(nameof(VideoProjectStatus.Producing), payload.Status);
        Assert.Equal(2, payload.VisualCount);
        Assert.Equal(2, provider.Requests.Count);
        Assert.Equal("Creator at a workstation", provider.Requests[0].VisualDirection);
        Assert.Equal(8, provider.Requests[0].DurationSeconds);

        var artifacts = await db.ProductionArtifacts
            .Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Visual)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();
        Assert.Equal(2, artifacts.Count);
        Assert.Equal("visual-1", artifacts[0].ProviderAssetId);
        Assert.Contains("sceneNumber", artifacts[0].MetadataJson);
        Assert.Equal(VideoProjectStatus.Producing, (await db.VideoProjects.SingleAsync(x => x.Id == project.Id)).Status);
    }

    [Fact]
    public async Task GenerateVisuals_returns_bad_gateway_and_does_not_persist_partial_assets_when_provider_output_is_invalid()
    {
        await using var db = CreateDb();
        var project = await AddPlannedProject(db);
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.ScenePlan,
            ProviderAssetId = "scene-plan",
            Content = JsonSerializer.Serialize(new[]
            {
                new ScenePlanItem(1, "Open with the hook.", "Creator at a workstation", 8),
                new ScenePlanItem(2, "Explain the idea.", "Clean product visualization", 12)
            })
        });
        await db.SaveChangesAsync();

        var result = await CreateController(db, new InvalidVisualProvider()).GenerateVisuals(project.Id, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
        Assert.Empty(await db.ProductionArtifacts.Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Visual).ToListAsync());
        Assert.Equal(VideoProjectStatus.Planned, (await db.VideoProjects.SingleAsync(x => x.Id == project.Id)).Status);
    }

    private static async Task<VideoProject> AddPlannedProject(YoutubeStudioDbContext db)
    {
        var workspace = new Workspace { Name = "Test workspace" };
        var project = new VideoProject
        {
            WorkspaceId = workspace.Id,
            Prompt = "Create a video about AI",
            Title = "AI Explained",
            Script = "HOOK: AI changes how creators work.",
            Status = VideoProjectStatus.Planned
        };
        db.Workspaces.Add(workspace);
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private static VisualsController CreateController(YoutubeStudioDbContext db, IVisualProvider provider) =>
        new(db, provider);

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }

    private sealed class RecordingVisualProvider : IVisualProvider
    {
        public List<VisualRequest> Requests { get; } = [];

        public Task<VisualResult> GenerateVisualAsync(VisualRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new VisualResult($"visual-{Requests.Count}", "image"));
        }
    }

    private sealed class InvalidVisualProvider : IVisualProvider
    {
        public Task<VisualResult> GenerateVisualAsync(VisualRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new VisualResult(string.Empty, "image"));
    }
}
