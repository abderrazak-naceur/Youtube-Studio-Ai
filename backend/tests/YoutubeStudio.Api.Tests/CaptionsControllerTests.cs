using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class CaptionsControllerTests
{
    [Fact]
    public async Task GenerateCaptions_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var result = await CreateController(db, new RecordingCaptionProvider()).GenerateCaptions(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GenerateCaptions_requires_producing_status()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Planned);

        var result = await CreateController(db, new RecordingCaptionProvider()).GenerateCaptions(project.Id, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("The video project must be producing before generating captions.", conflict.Value);
    }

    [Fact]
    public async Task GenerateCaptions_requires_persisted_script_voice_and_music()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);

        var result = await CreateController(db, new RecordingCaptionProvider()).GenerateCaptions(project.Id, CancellationToken.None);
        Assert.Equal("A persisted script is required to generate captions.", Assert.IsType<ValidationProblemDetails>(Assert.IsType<ObjectResult>(result.Result).Value).Detail);

        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Script,
            ProviderAssetId = "script",
            Content = "HOOK: Test script"
        });
        await db.SaveChangesAsync();

        result = await CreateController(db, new RecordingCaptionProvider()).GenerateCaptions(project.Id, CancellationToken.None);
        Assert.Equal("A persisted voice artifact is required to generate captions.", Assert.IsType<ValidationProblemDetails>(Assert.IsType<ObjectResult>(result.Result).Value).Detail);

        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Voice,
            ProviderAssetId = "voice",
            MetadataJson = "{\"durationSeconds\":20}"
        });
        await db.SaveChangesAsync();

        result = await CreateController(db, new RecordingCaptionProvider()).GenerateCaptions(project.Id, CancellationToken.None);
        Assert.Equal("A completed Music/SFX artifact is required to generate captions.", Assert.IsType<ValidationProblemDetails>(Assert.IsType<ObjectResult>(result.Result).Value).Detail);
    }

    [Fact]
    public async Task GenerateCaptions_passes_script_and_persists_timed_entries()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);
        AddPrerequisites(db, project.Id);
        await db.SaveChangesAsync();

        var provider = new RecordingCaptionProvider
        {
            Result = new CaptionResult("captions-1", [
                new CaptionEntry(0, 4, "First caption"),
                new CaptionEntry(4, 8, "Second caption")])
        };

        var result = await CreateController(db, provider).GenerateCaptions(project.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<CaptionGenerationResponse>(response.Value);
        Assert.Equal(project.Id, payload.VideoProjectId);
        Assert.Equal(nameof(VideoProjectStatus.Producing), payload.Status);
        Assert.Equal("captions-1", payload.ProviderAssetId);
        Assert.Equal(2, payload.EntryCount);
        Assert.Equal("HOOK: Test script", provider.Requests.Single().Script);

        var artifacts = await db.ProductionArtifacts
            .Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Captions)
            .ToListAsync();
        Assert.Single(artifacts);
        Assert.Equal("captions-1", artifacts[0].ProviderAssetId);
        Assert.Contains("First caption", artifacts[0].MetadataJson);
        Assert.Contains("durationSeconds", artifacts[0].MetadataJson);
    }

    [Fact]
    public async Task GenerateCaptions_rejects_overlapping_or_out_of_range_entries()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);
        AddPrerequisites(db, project.Id);
        await db.SaveChangesAsync();

        var provider = new RecordingCaptionProvider
        {
            Result = new CaptionResult("captions-invalid", [
                new CaptionEntry(0, 12, "First"),
                new CaptionEntry(11, 21, "Overlap and out of range")])
        };

        var result = await CreateController(db, provider).GenerateCaptions(project.Id, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
        Assert.Empty(await db.ProductionArtifacts
            .Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Captions)
            .ToListAsync());
    }

    [Fact]
    public async Task GenerateCaptions_rejects_invalid_provider_asset()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);
        AddPrerequisites(db, project.Id);
        await db.SaveChangesAsync();

        var result = await CreateController(db, new RecordingCaptionProvider
        {
            Result = new CaptionResult(string.Empty, [new CaptionEntry(0, 2, "Valid text")])
        }).GenerateCaptions(project.Id, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
        Assert.Empty(await db.ProductionArtifacts
            .Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Captions)
            .ToListAsync());
    }

    [Fact]
    public async Task GenerateCaptions_rejects_invalid_voice_duration_metadata()
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
            Type = ProductionArtifactType.Voice,
            ProviderAssetId = "voice",
            MetadataJson = "not-json"
        });
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.MusicSfx,
            ProviderAssetId = "music"
        });
        await db.SaveChangesAsync();

        var result = await CreateController(db, new RecordingCaptionProvider()).GenerateCaptions(project.Id, CancellationToken.None);

        var validation = Assert.IsType<ValidationProblemDetails>(Assert.IsType<ObjectResult>(result.Result).Value);
        Assert.Equal("The persisted voice artifact does not contain a valid production duration.", validation.Detail);
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

    private static void AddPrerequisites(YoutubeStudioDbContext db, Guid projectId)
    {
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = projectId,
            Type = ProductionArtifactType.Script,
            ProviderAssetId = "script",
            Content = "HOOK: Test script"
        });
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = projectId,
            Type = ProductionArtifactType.Voice,
            ProviderAssetId = "voice",
            MetadataJson = JsonSerializer.Serialize(new { durationSeconds = 20d })
        });
        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = projectId,
            Type = ProductionArtifactType.MusicSfx,
            ProviderAssetId = "music"
        });
    }

    private static CaptionsController CreateController(YoutubeStudioDbContext db, ICaptionProvider provider) => new(db, provider);

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }

    private sealed class RecordingCaptionProvider : ICaptionProvider
    {
        public List<CaptionRequest> Requests { get; } = [];
        public CaptionResult Result { get; set; } = new("captions-1", [new CaptionEntry(0, 2, "Caption")]);

        public Task<CaptionResult> GenerateCaptionsAsync(CaptionRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(Result);
        }
    }
}
