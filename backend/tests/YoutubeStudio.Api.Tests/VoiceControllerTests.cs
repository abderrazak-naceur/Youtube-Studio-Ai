using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class VoiceControllerTests
{
    [Fact]
    public async Task GenerateVoice_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var result = await CreateController(db, new RecordingVoiceProvider())
            .GenerateVoice(Guid.NewGuid(), new GenerateVoiceRequest("en-US-1"), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GenerateVoice_requires_producing_status()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Planned);

        var result = await CreateController(db, new RecordingVoiceProvider())
            .GenerateVoice(project.Id, new GenerateVoiceRequest("en-US-1"), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("The video project must be producing before generating voice.", conflict.Value);
    }

    [Fact]
    public async Task GenerateVoice_requires_persisted_script()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);

        var result = await CreateController(db, new RecordingVoiceProvider())
            .GenerateVoice(project.Id, new GenerateVoiceRequest(null), CancellationToken.None);

        var validation = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(validation.Value);
        Assert.Equal("A persisted script is required to generate voice.", problem.Detail);
    }

    [Fact]
    public async Task GenerateVoice_persists_voice_and_passes_script_and_voice_options()
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
        await db.SaveChangesAsync();

        var provider = new RecordingVoiceProvider();
        var result = await CreateController(db, provider)
            .GenerateVoice(project.Id, new GenerateVoiceRequest("  en-US-1  "), CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<VoiceGenerationResponse>(response.Value);
        Assert.Equal(project.Id, payload.VideoProjectId);
        Assert.Equal(nameof(VideoProjectStatus.Producing), payload.Status);
        Assert.Equal("voice-1", payload.ProviderAssetId);
        Assert.Equal("HOOK: Test script", provider.Requests.Single().Script);
        Assert.Equal("en-US-1", provider.Requests.Single().VoiceId);

        var artifacts = await db.ProductionArtifacts
            .Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Voice)
            .ToListAsync();
        Assert.Single(artifacts);
        Assert.Equal("voice-1", artifacts[0].ProviderAssetId);
        Assert.Contains("audio/mpeg", artifacts[0].MetadataJson);
        Assert.Contains("durationSeconds", artifacts[0].MetadataJson);
    }

    [Fact]
    public async Task GenerateVoice_returns_bad_gateway_and_does_not_persist_invalid_provider_output()
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
        await db.SaveChangesAsync();

        var result = await CreateController(db, new InvalidVoiceProvider())
            .GenerateVoice(project.Id, new GenerateVoiceRequest(null), CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
        Assert.Empty(await db.ProductionArtifacts.Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Voice).ToListAsync());
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

    private static VoiceController CreateController(YoutubeStudioDbContext db, IVoiceProvider provider) =>
        new(db, provider);

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }

    private sealed class RecordingVoiceProvider : IVoiceProvider
    {
        public List<VoiceRequest> Requests { get; } = [];

        public Task<VoiceResult> GenerateVoiceAsync(VoiceRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new VoiceResult("voice-1", "audio/mpeg", TimeSpan.FromSeconds(10)));
        }
    }

    private sealed class InvalidVoiceProvider : IVoiceProvider
    {
        public Task<VoiceResult> GenerateVoiceAsync(VoiceRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new VoiceResult(string.Empty, string.Empty, TimeSpan.Zero));
    }
}
