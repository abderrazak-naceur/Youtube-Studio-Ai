using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Controlled experiments (A/B on title/thumbnail/hook/…) per DATA-MODEL Experiment and
/// ANALYTICS-INTELLIGENCE. Each experiment defines a hypothesis, baseline, success metric
/// and decision rule; results are recorded when observed.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/experiments")]
public sealed class ExperimentsController(YoutubeStudioDbContext db, IWorkspaceAccess access) : ControllerBase
{
    private static readonly HashSet<string> Statuses = ["Draft", "Running", "Completed", "Abandoned"];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExperimentResponse>>> GetAll(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (await access.GetRoleAsync(User, workspaceId, cancellationToken) is null) return Forbid();

        var experiments = await db.Experiments.AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => ToResponse(x))
            .ToListAsync(cancellationToken);

        return Ok(experiments);
    }

    [HttpPost]
    public async Task<ActionResult<ExperimentResponse>> Create(Guid workspaceId, CreateExperimentRequest request, CancellationToken cancellationToken)
    {
        if (await access.GetRoleAsync(User, workspaceId, cancellationToken) is null) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Variable)) return ValidationProblem("Experiment variable is required.");
        if (string.IsNullOrWhiteSpace(request.Hypothesis)) return ValidationProblem("Experiment hypothesis is required.");
        if (string.IsNullOrWhiteSpace(request.SuccessMetric)) return ValidationProblem("A success metric is required.");

        var experiment = new Experiment
        {
            WorkspaceId = workspaceId,
            VideoProjectId = request.VideoProjectId,
            Variable = request.Variable.Trim(),
            Hypothesis = request.Hypothesis.Trim(),
            SuccessMetric = request.SuccessMetric.Trim(),
            BaselineValue = request.BaselineValue,
            DecisionRule = string.IsNullOrWhiteSpace(request.DecisionRule) ? null : request.DecisionRule.Trim(),
            Status = ExperimentStatus.Draft
        };

        db.Experiments.Add(experiment);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { workspaceId }, ToResponse(experiment));
    }

    [HttpPost("{id:guid}/result")]
    public async Task<ActionResult<ExperimentResponse>> RecordResult(Guid workspaceId, Guid id, RecordExperimentResultRequest request, CancellationToken cancellationToken)
    {
        if (await access.GetRoleAsync(User, workspaceId, cancellationToken) is null) return Forbid();
        if (!Statuses.Contains(request.Status)) return BadRequest("Status must be Draft, Running, Completed or Abandoned.");

        var experiment = await db.Experiments.SingleOrDefaultAsync(x => x.Id == id && x.WorkspaceId == workspaceId, cancellationToken);
        if (experiment is null) return NotFound("Experiment does not exist in the specified workspace.");

        experiment.ResultValue = request.ResultValue;
        experiment.Outcome = string.IsNullOrWhiteSpace(request.Outcome) ? null : request.Outcome.Trim();
        experiment.Status = Enum.Parse<ExperimentStatus>(request.Status);
        experiment.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(experiment));
    }

    private static ExperimentResponse ToResponse(Experiment x) => new(
        x.Id, x.WorkspaceId, x.VideoProjectId, x.Variable, x.Hypothesis, x.SuccessMetric,
        x.BaselineValue, x.DecisionRule, x.Status.ToString(), x.ResultValue, x.Outcome, x.CreatedAtUtc);
}

public sealed record CreateExperimentRequest(
    Guid? VideoProjectId, string Variable, string Hypothesis, string SuccessMetric, double BaselineValue, string? DecisionRule);

public sealed record RecordExperimentResultRequest(double ResultValue, string Status, string? Outcome);

public sealed record ExperimentResponse(
    Guid Id, Guid WorkspaceId, Guid? VideoProjectId, string Variable, string Hypothesis, string SuccessMetric,
    double BaselineValue, string? DecisionRule, string Status, double? ResultValue, string? Outcome, DateTime CreatedAtUtc);
