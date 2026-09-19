using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class MusicSfxControllerTests
{
    [Fact]
    public async Task GenerateMusicSfx_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var result = await CreateController(db, new RecordingMusicSfxProvider())
            .GenerateMusicSfx(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GenerateMusicSfx_requires_producing_status()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Planned);

        var result = await CreateController(db, new RecordingMusicSfxProvider())
            .GenerateMusicSfx(project.Id, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("The video project must be producing before generating music/SFX.", conflict.Value);
    }

    [Fact]
    public async Task GenerateMusicSfx_requires_persisted_script()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);

        var result = await CreateController(db, new RecordingMusicSfxProvider())
            .GenerateMusicSfx(project.Id, CancellationToken.None);

        var validation = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(validation.Value);
        Assert.Equal("A persisted script is required to generate music/SFX.", problem.Detail);
    }

    [Fact]
    public async Task GenerateMusicSfx_requires_valid_scene_plan()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Script,
            ProviderAssetId = "script",
            Content = "HOOK: Test script"
        });
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.ScenePlan,
            ProviderAssetId = "scene-plan",
            Content = "not-json"
        });
        await db.SaveChangesAsync();

        var result = await CreateController(db, new RecordingMusicSfxProvider())
            .GenerateMusicSfx(project.Id, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal("The persisted scene plan is invalid.", problem.Value is ProblemDetails details ? details.Detail : problem.Value);
    }

    [Fact]
    public async Task GenerateMusicSfx_persists_asset_and_passes_title_script_and_duration()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Script,
            ProviderAssetId = "script",
            Content = "HOOK: Test script"
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
        await db.SaveChangesAsync();

        var provider = new RecordingMusicSfxProvider();
        var result = await CreateController(db, provider)
            .GenerateMusicSfx(project.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<MusicSfxGenerationResponse>(response.Value);
        Assert.Equal(project.Id, payload.VideoProjectId);
        Assert.Equal(nameof(VideoProjectStatus.Producing), payload.Status);
        Assert.Equal("music-1", payload.ProviderAssetId);

        var request = provider.Requests.Single();
        Assert.Equal("AI Explained", request.Title);
        Assert.Equal("HOOK: Test script", request.Script);
        Assert.Equal(20, request.DurationSeconds);

        var artifacts = await db.ProductionArtifacts
            .Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.MusicSfx)
            .ToListAsync();
        Assert.Single(artifacts);
        Assert.Equal("music-1", artifacts[0].ProviderAssetId);
        Assert.Contains("audio/mpeg", artifacts[0].MetadataJson);
        Assert.Contains("durationSeconds", artifacts[0].MetadataJson);
    }

    [Fact]
    public async Task GenerateMusicSfx_returns_bad_gateway_and_does_not_persist_invalid_provider_output()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Script,
            ProviderAssetId = "script",
            Content = "HOOK: Test script"
        });
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.ScenePlan,
            ProviderAssetId = "scene-plan",
            Content = JsonSerializer.Serialize(new[]
            {
                new ScenePlanItem(1, "First", "Opening", 8)
            })
        });
        await db.SaveChangesAsync();

        var result = await CreateController(db, new InvalidMusicSfxProvider())
            .GenerateMusicSfx(project.Id, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
        Assert.Empty(await db.ProductionArtifacts
            .Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.MusicSfx)
            .ToListAsync());
        Assert.Equal(VideoProjectStatus.Producing, (await db.VideoProjects.SingleAsync(x => x.Id == project.Id)).Status);
    }

    private static async Task<VideoProject> AddProject(YoutubeStudioDbContext db, VideoProjectStatus status)
    {
        var workspace = new Workspace { Name = "Test workspace" };
        var project = new VideoProject
        {
            WorkspaceId = workspace.Id,
            Prompt = "Create a video about AI",
            Title = "AI Explained",
            Script = "HOOK: AI changes how creators work.",
            Status = status
        };
        db.Workspaces.Add(workspace);
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private static MusicSfxController CreateController(YoutubeStudioDbContext db, IMusicSfxProvider provider) =>
        new(db, provider);

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }

    private sealed class RecordingMusicSfxProvider : IMusicSfxProvider
    {
        public List<MusicSfxRequest> Requests { get; } = [];

        public Task<MusicSfxResult> GenerateMusicSfxAsync(MusicSfxRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new MusicSfxResult("music-1", "audio/mpeg"));
        }
    }

    private sealed class InvalidMusicSfxProvider : IMusicSfxProvider
    {
        public Task<MusicSfxResult> GenerateMusicSfxAsync(MusicSfxRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new MusicSfxResult(string.Empty, string.Empty));
    }
}
