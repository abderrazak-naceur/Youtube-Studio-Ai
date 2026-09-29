using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Production;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Tests;

public sealed class ProductionPipelineTests
{
    [Fact]
    public async Task RunJob_completes_and_records_stage_progress_on_a_healthy_pipeline()
    {
        await using var db = CreateDb();
        var (job, services) = await SetupAsync(db);
        var pipeline = CreatePipeline(db, services, maxAttempts: 3);

        await pipeline.RunJobAsync(job, CancellationToken.None);

        Assert.Equal(ProductionJobStatus.Succeeded, job.Status);
        Assert.Equal(VideoProjectStatus.Completed, job.VideoProject.Status);
        Assert.Equal(VideoProjectStatus.Qa.ToString(), job.LastCompletedStage);
        Assert.Equal(1, job.Attempt);
        // 8 single-type artifacts + 3 visuals (one per placeholder scene).
        Assert.Equal(11, await db.ProductionArtifacts.CountAsync());
        Assert.Equal(3, await db.ProductionArtifacts.CountAsync(x => x.Type == ProductionArtifactType.Visual));
    }

    [Fact]
    public async Task RunJob_requeues_and_increments_attempt_when_a_stage_fails_below_the_limit()
    {
        await using var db = CreateDb();
        var (job, services) = await SetupAsync(db, researchFailures: 1);
        var pipeline = CreatePipeline(db, services, maxAttempts: 3);

        await pipeline.RunJobAsync(job, CancellationToken.None);

        Assert.Equal(ProductionJobStatus.Queued, job.Status);
        Assert.Equal(2, job.Attempt);
        Assert.Equal(VideoProjectStatus.Draft, job.VideoProject.Status);
        Assert.Contains("boom", job.Error);
    }

    [Fact]
    public async Task RunJob_dead_letters_when_attempts_are_exhausted()
    {
        await using var db = CreateDb();
        var (job, services) = await SetupAsync(db, researchFailures: 5);
        var pipeline = CreatePipeline(db, services, maxAttempts: 3);
        job.Attempt = 3;

        await pipeline.RunJobAsync(job, CancellationToken.None);

        Assert.Equal(ProductionJobStatus.DeadLettered, job.Status);
        Assert.Equal(VideoProjectStatus.Failed, job.VideoProject.Status);
        Assert.Contains("boom", job.Error);
    }

    [Fact]
    public async Task RunJob_is_idempotent_and_does_not_duplicate_artifacts_across_reruns()
    {
        await using var db = CreateDb();
        var (job, services) = await SetupAsync(db);
        var pipeline = CreatePipeline(db, services, maxAttempts: 3);

        await pipeline.RunJobAsync(job, CancellationToken.None);
        // Simulate a retry of the same job by resetting its runtime state and running again.
        job.Status = ProductionJobStatus.Queued;
        job.Attempt = 2;
        await pipeline.RunJobAsync(job, CancellationToken.None);

        Assert.Equal(ProductionJobStatus.Succeeded, job.Status);
        // Same artifact count as a single run — the rerun replaced artifacts instead of duplicating them.
        Assert.Equal(11, await db.ProductionArtifacts.CountAsync());
    }

    private static async Task<(ProductionJob Job, IServiceProvider Services)> SetupAsync(
        YoutubeStudioDbContext db, int researchFailures = 0)
    {
        var workspace = new Workspace { Name = "Creator workspace" };
        var project = new VideoProject { WorkspaceId = workspace.Id, Prompt = "Explain how AI helps creators plan videos.", Status = VideoProjectStatus.Draft };
        var job = new ProductionJob { VideoProjectId = project.Id, Status = ProductionJobStatus.Queued };
        db.Workspaces.Add(workspace);
        db.VideoProjects.Add(project);
        db.ProductionJobs.Add(job);
        await db.SaveChangesAsync();
        job.VideoProject = project;

        var services = new ServiceCollection();
        services.AddSingleton<IResearchProvider>(new FlakyResearchProvider(researchFailures));
        services.AddSingleton<IScriptProvider, PlaceholderScriptProvider>();
        services.AddSingleton<IScenePlanProvider, PlaceholderScenePlanProvider>();
        services.AddSingleton<IVoiceProvider, PlaceholderVoiceProvider>();
        services.AddSingleton<IVisualProvider, PlaceholderVisualProvider>();
        services.AddSingleton<IMusicSfxProvider, PlaceholderMusicSfxProvider>();
        services.AddSingleton<ICaptionProvider, PlaceholderCaptionProvider>();
        services.AddSingleton<IRenderProvider, PlaceholderRenderProvider>();
        services.AddSingleton<IQaProvider, PlaceholderQaProvider>();
        return (job, services.BuildServiceProvider());
    }

    private static ProductionPipeline CreatePipeline(YoutubeStudioDbContext db, IServiceProvider services, int maxAttempts)
    {
        var router = new ModelRouter(Options.Create(new AiProviderOptions()), NullLogger<ModelRouter>.Instance);
        return new ProductionPipeline(db, router, services, Options.Create(new ProductionOptions { MaxAttempts = maxAttempts }), NullLogger<ProductionPipeline>.Instance);
    }

    private static YoutubeStudioDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<YoutubeStudioDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    /// <summary>Research provider that throws for the first N calls, then succeeds like the placeholder.</summary>
    private sealed class FlakyResearchProvider(int failures) : IResearchProvider
    {
        private int _calls;
        public string ProviderName => "placeholder";
        public AiTask SupportedTask => AiTask.Research;

        public Task<ResearchResult> ResearchAsync(ResearchRequest request, CancellationToken cancellationToken)
        {
            _calls++;
            if (_calls <= failures) throw new InvalidOperationException("boom");
            return Task.FromResult(new ResearchResult($"Research for: {request.Prompt}", Array.Empty<string>()));
        }
    }
}
