using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class MetadataControllerTests
{
    [Fact]
    public async Task GenerateMetadata_returns_not_found_for_missing_project()
    {
        await using var db = CreateDb();
        var result = await CreateController(db, new RecordingMetadataProvider())
            .GenerateMetadata(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GenerateMetadata_requires_producing_status()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Planned);

        var result = await CreateController(db, new RecordingMetadataProvider())
            .GenerateMetadata(project.Id, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("The video project must be producing before generating metadata.", conflict.Value);
    }

    [Fact]
    public async Task GenerateMetadata_requires_persisted_script()
    {
        await using var db = CreateDb();
        var project = await AddProject(db, VideoProjectStatus.Producing);

        var result = await CreateController(db, new RecordingMetadataProvider())
            .GenerateMetadata(project.Id, CancellationToken.None);

        var validation = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(validation.Value);
        Assert.Equal("A persisted script is required to generate metadata.", problem.Detail);
    }

    [Fact]
    public async Task GenerateMetadata_persists_metadata_and_passes_title_and_script()
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

        var provider = new RecordingMetadataProvider();
        var result = await CreateController(db, provider)
            .GenerateMetadata(project.Id, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<MetadataGenerationResponse>(response.Value);
        Assert.Equal(project.Id, payload.VideoProjectId);
        Assert.Equal("AI Explained | Full Guide", payload.Title);
        Assert.Equal(2, payload.TagCount);

        var request = provider.Requests.Single();
        Assert.Equal("AI Explained", request.Title);
        Assert.Equal("HOOK: Test script", request.Script);

        var artifact = await db.ProductionArtifacts.SingleAsync(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Metadata);
        Assert.Contains("description", artifact.MetadataJson);
        Assert.Contains("tags", artifact.MetadataJson);
    }

    [Fact]
    public async Task GenerateMetadata_returns_bad_gateway_for_incomplete_output()
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

        var result = await CreateController(db, new EmptyMetadataProvider())
            .GenerateMetadata(project.Id, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
        Assert.Empty(await db.ProductionArtifacts.Where(x => x.VideoProjectId == project.Id && x.Type == ProductionArtifactType.Metadata).ToListAsync());
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

    private static MetadataController CreateController(YoutubeStudioDbContext db, IMetadataProvider provider) => new MetadataController(db, provider, new StubWorkspaceAccess()).WithUser();

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }

    private sealed class RecordingMetadataProvider : IMetadataProvider
    {
        public List<MetadataRequest> Requests { get; } = [];

        public Task<MetadataResult> GenerateMetadataAsync(MetadataRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new MetadataResult("AI Explained | Full Guide", "A helpful explainer.", ["ai", "creators"], "Education", "en"));
        }
    }

    private sealed class EmptyMetadataProvider : IMetadataProvider
    {
        public Task<MetadataResult> GenerateMetadataAsync(MetadataRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new MetadataResult(string.Empty, string.Empty, Array.Empty<string>(), string.Empty, string.Empty));
    }
}
