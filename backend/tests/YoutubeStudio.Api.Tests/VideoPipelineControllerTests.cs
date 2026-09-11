using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class VideoPipelineControllerTests
{
    [Fact]
    public async Task Get_returns_project_pipeline_with_latest_job_and_artifacts_in_creation_order()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Test workspace" };
        db.Workspaces.Add(workspace);
        var project = new VideoProject
        {
            WorkspaceId = workspace.Id,
            Prompt = "Create a video about AI",
            Status = VideoProjectStatus.Scripted,
            Title = "AI video"
        };
        db.VideoProjects.Add(project);
        await db.SaveChangesAsync();

        var older = DateTime.UtcNow.AddMinutes(-2);
        var newer = DateTime.UtcNow.AddMinutes(-1);
        db.ProductionArtifacts.AddRange(
            new ProductionArtifact
            {
                VideoProjectId = project.Id,
                Type = ProductionArtifactType.Research,
                ProviderAssetId = "research-1",
                CreatedAtUtc = older
            },
            new ProductionArtifact
            {
                VideoProjectId = project.Id,
                Type = ProductionArtifactType.Script,
                ProviderAssetId = "script-1",
                CreatedAtUtc = newer
            });
        db.ProductionJobs.Add(new ProductionJob
        {
            VideoProjectId = project.Id,
            Status = ProductionJobStatus.Running,
            Attempt = 2,
            Error = null,
            UpdatedAtUtc = newer
        });
        await db.SaveChangesAsync();

        var controller = new VideoPipelineController(db);
        var result = await controller.Get(project.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<VideoPipelineResponse>(ok.Value);
        Assert.Equal(project.Id, response.VideoProjectId);
        Assert.Equal(nameof(VideoProjectStatus.Scripted), response.Status);
        Assert.Equal("AI video", response.Title);
        Assert.NotNull(response.LatestJob);
        Assert.Equal(nameof(ProductionJobStatus.Running), response.LatestJob!.Status);
        Assert.Equal(2, response.LatestJob.Attempt);
        Assert.Equal(2, response.Artifacts.Count);
        Assert.Equal(nameof(ProductionArtifactType.Research), response.Artifacts[0].Type);
        Assert.Equal("research-1", response.Artifacts[0].ProviderAssetId);
        Assert.Equal(nameof(ProductionArtifactType.Script), response.Artifacts[1].Type);
        Assert.Equal("script-1", response.Artifacts[1].ProviderAssetId);
    }

    [Fact]
    public async Task Get_returns_not_found_for_unknown_project()
    {
        await using var db = CreateDb();
        var controller = new VideoPipelineController(db);

        var result = await controller.Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }
}
