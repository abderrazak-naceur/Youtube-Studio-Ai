using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Records and lists performance snapshots for a video project so outcomes can be
/// connected back to content decisions (backlog US-024).
/// </summary>
[ApiController]
[Route("api/v1/video-projects/{videoProjectId:guid}/outcomes")]
public sealed class VideoOutcomesController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VideoOutcomeResponse>>> GetAll(Guid videoProjectId, CancellationToken cancellationToken)
    {
        if (!await db.VideoProjects.AnyAsync(x => x.Id == videoProjectId, cancellationToken))
            return NotFound();

        var outcomes = await db.VideoOutcomes.AsNoTracking()
            .Where(x => x.VideoProjectId == videoProjectId)
            .OrderByDescending(x => x.MeasuredAtUtc)
            .Select(x => new VideoOutcomeResponse(x.Id, x.Source, x.MeasuredAtUtc, x.Views, x.Likes, x.Comments, x.AverageViewDurationSeconds, x.EstimatedRevenueUsd))
            .ToListAsync(cancellationToken);

        return Ok(outcomes);
    }

    [HttpPost]
    public async Task<ActionResult<VideoOutcomeResponse>> Create(Guid videoProjectId, RecordOutcomeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Source))
            return ValidationProblem("An outcome source is required.");
        if (request.Views < 0 || request.Likes < 0 || request.Comments < 0 || request.AverageViewDurationSeconds < 0 || request.EstimatedRevenueUsd < 0)
            return ValidationProblem("Outcome metrics cannot be negative.");

        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == videoProjectId, cancellationToken);
        if (project is null) return NotFound();
        if (project.Status != VideoProjectStatus.Completed)
            return Conflict("Outcomes can only be recorded for completed projects.");

        var outcome = new VideoOutcome
        {
            VideoProjectId = videoProjectId,
            Source = request.Source.Trim(),
            MeasuredAtUtc = request.MeasuredAtUtc == default ? DateTime.UtcNow : request.MeasuredAtUtc,
            Views = request.Views,
            Likes = request.Likes,
            Comments = request.Comments,
            AverageViewDurationSeconds = request.AverageViewDurationSeconds,
            EstimatedRevenueUsd = request.EstimatedRevenueUsd
        };

        db.VideoOutcomes.Add(outcome);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { videoProjectId }, new VideoOutcomeResponse(
            outcome.Id, outcome.Source, outcome.MeasuredAtUtc, outcome.Views, outcome.Likes, outcome.Comments, outcome.AverageViewDurationSeconds, outcome.EstimatedRevenueUsd));
    }
}

public sealed record RecordOutcomeRequest(
    string Source,
    DateTime MeasuredAtUtc,
    long Views,
    long Likes,
    long Comments,
    double AverageViewDurationSeconds,
    decimal EstimatedRevenueUsd);

public sealed record VideoOutcomeResponse(
    Guid Id,
    string Source,
    DateTime MeasuredAtUtc,
    long Views,
    long Likes,
    long Comments,
    double AverageViewDurationSeconds,
    decimal EstimatedRevenueUsd);
