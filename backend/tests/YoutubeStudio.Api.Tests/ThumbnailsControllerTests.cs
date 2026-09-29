using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class ThumbnailsControllerTests
{
    [Fact]
    public async Task GenerateThumbnails_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var result = await CreateController(db, new RecordingThumbnailProvider())
            .GenerateThumbnails(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GenerateThumbnails_requires_producing_status()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Planned);

        var result = await CreateController(db, new RecordingThumbnailProvider())
            .GenerateThumbnails(project.Id, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("The video project must be producing before generating thumbnails.", conflict.Value);
    }

    [Fact]
    public async Task GenerateThumbnails_requires_persisted_script()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);

        var result = await CreateController(db, new RecordingThumbnailProvider())
            .GenerateThumbnails(project.Id, CancellationToken.None);

        var validation = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(validation.Value);
        Assert.Equal("A persisted script is required to generate thumbnails.", problem.Detail);
    }

    [Fact]
    public async Task GenerateThumbnails_persists_candidates_and_passes_title_and_script()
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

        var provider = new RecordingThumbnailProvider();
        var result = await CreateController(db, provider)
            .GenerateThumbnails(project.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ThumbnailGenerationResponse>(response.Value);
        Assert.Equal(project.Id, payload.VideoProjectId);
        Assert.Equal(nameof(VideoProjectStatus.Producing), payload.Status);
        Assert.Equal(2, payload.CandidateCount);
        Assert.Equal("thumb-1", payload.PrimaryProviderAssetId);

        var request = provider.Requests.Single();
        Assert.Equal("AI Explained", request.Title);
        Assert.Equal("HOOK: Test script", request.Script);
        Assert.Equal(3, request.CandidateCount);

        var artifacts = await db.ProductionArtifacts
            .Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Thumbnail)
            .ToListAsync();
        Assert.Single(artifacts);
        Assert.Equal("thumb-1", artifacts[0].ProviderAssetId);
        Assert.Contains("thumb-2", artifacts[0].MetadataJson);
        Assert.Contains("image/png", artifacts[0].MetadataJson);
    }

    [Fact]
    public async Task GenerateThumbnails_returns_bad_gateway_for_invalid_provider_output()
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

        var result = await CreateController(db, new InvalidThumbnailProvider())
            .GenerateThumbnails(project.Id, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
        Assert.Empty(await db.ProductionArtifacts
            .Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Thumbnail)
            .ToListAsync());
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

    private static ThumbnailsController CreateController(YoutubeStudioDbContext db, IThumbnailProvider provider) =>
        new(db, provider);

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }

    private sealed class RecordingThumbnailProvider : IThumbnailProvider
    {
        public List<ThumbnailRequest> Requests { get; } = [];

        public Task<ThumbnailResult> GenerateThumbnailsAsync(ThumbnailRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new ThumbnailResult(
            [
                new ThumbnailCandidate("thumb-1", "Headline one", "image/png"),
                new ThumbnailCandidate("thumb-2", "Headline two", "image/png")
            ]));
        }
    }

    private sealed class InvalidThumbnailProvider : IThumbnailProvider
    {
        public Task<ThumbnailResult> GenerateThumbnailsAsync(ThumbnailRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new ThumbnailResult(Array.Empty<ThumbnailCandidate>()));
    }
}
