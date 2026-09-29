using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class ExperimentsControllerTests
{
    [Fact]
    public async Task Create_persists_experiment_in_draft()
    {
        await using var db = CreateDb();
        var workspace = await AddWorkspace(db);
        var controller = new ExperimentsController(db, new StubWorkspaceAccess()).WithUser();

        var result = await controller.Create(workspace.Id,
            new CreateExperimentRequest(null, "thumbnail", "Bold text lifts CTR", "ctr", 4.2, "Adopt if CTR > baseline for 7 days"),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ExperimentResponse>(created.Value);
        Assert.Equal("thumbnail", response.Variable);
        Assert.Equal("Draft", response.Status);
        Assert.Single(db.Experiments);
    }

    [Fact]
    public async Task Create_requires_variable_hypothesis_metric()
    {
        await using var db = CreateDb();
        var workspace = await AddWorkspace(db);
        var controller = new ExperimentsController(db, new StubWorkspaceAccess()).WithUser();

        var result = await controller.Create(workspace.Id,
            new CreateExperimentRequest(null, "  ", "h", "m", 0, null), CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
        Assert.Empty(db.Experiments);
    }

    [Fact]
    public async Task RecordResult_updates_status_and_result()
    {
        await using var db = CreateDb();
        var workspace = await AddWorkspace(db);
        var experiment = new Experiment { WorkspaceId = workspace.Id, Variable = "title", Hypothesis = "h", SuccessMetric = "ctr", BaselineValue = 4 };
        db.Experiments.Add(experiment);
        await db.SaveChangesAsync();

        var controller = new ExperimentsController(db, new StubWorkspaceAccess()).WithUser();
        var result = await controller.RecordResult(workspace.Id, experiment.Id,
            new RecordExperimentResultRequest(5.1, "Completed", "Variant won"), CancellationToken.None);

        var response = Assert.IsType<ExperimentResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("Completed", response.Status);
        Assert.Equal(5.1, response.ResultValue);
        Assert.Equal("Variant won", response.Outcome);
    }

    [Fact]
    public async Task GetAll_forbids_non_member()
    {
        await using var db = CreateDb();
        var workspace = await AddWorkspace(db);
        var controller = new ExperimentsController(db, new StubWorkspaceAccess(null)).WithUser();

        var result = await controller.GetAll(workspace.Id, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    private static async Task<Workspace> AddWorkspace(YoutubeStudioDbContext db)
    {
        var workspace = new Workspace { Name = "Studio" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();
        return workspace;
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(opts);
    }
}
