using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class ArtifactVersioningTests
{
    [Fact]
    public async Task NextVersion_increments_per_project_and_type()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);

        var v1 = await db.NextVersionAsync(project.Id, ProductionArtifactType.Script, CancellationToken.None);
        Assert.Equal(1, v1);

        db.ProductionArtifacts.Add(new ProductionArtifact { VideoProjectId = project.Id, Type = ProductionArtifactType.Script, Version = v1, ProviderAssetId = "script" });
        await db.SaveChangesAsync();

        var v2 = await db.NextVersionAsync(project.Id, ProductionArtifactType.Script, CancellationToken.None);
        Assert.Equal(2, v2);

        // A different type restarts at 1.
        Assert.Equal(1, await db.NextVersionAsync(project.Id, ProductionArtifactType.Metadata, CancellationToken.None));
    }

    [Fact]
    public async Task Artifacts_endpoint_exposes_version()
    {
        await using var db = CreateDb();
        var project = await AddProject(db);
        db.ProductionArtifacts.Add(new ProductionArtifact { VideoProjectId = project.Id, Type = ProductionArtifactType.Script, Version = 1, ProviderAssetId = "script", Content = "v1" });
        db.ProductionArtifacts.Add(new ProductionArtifact { VideoProjectId = project.Id, Type = ProductionArtifactType.Script, Version = 2, ProviderAssetId = "script", Content = "v2" });
        await db.SaveChangesAsync();

        var result = await new VideoArtifactsController(db, new StubWorkspaceAccess()).WithUser()
            .GetAll(project.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var artifacts = Assert.IsAssignableFrom<IReadOnlyList<ProductionArtifactResponse>>(ok.Value);
        Assert.Equal(2, artifacts.Count);
        Assert.Contains(artifacts, a => a.Version == 2);
    }

    private static async Task<VideoProject> AddProject(YoutubeStudioDbContext db)
    {
        var workspace = new Workspace { Name = "Studio" };
        var project = new VideoProject { WorkspaceId = workspace.Id, Prompt = "AI video", Status = VideoProjectStatus.Producing };
        db.Workspaces.Add(workspace);
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(opts);
    }
}
