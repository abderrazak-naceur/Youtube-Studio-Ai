using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Human quality gate. No final video can reach <see cref="VideoProjectStatus.Completed"/>
/// (final export) without an explicit approval decision, and no project can be approved
/// unless the automated QA artifact recorded a passing result.
/// </summary>
[ApiController]
[Route("api/v1/video-projects")]
public sealed class ApprovalController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApprovalResponse>> Approve(Guid id, ApprovalDecisionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reviewer))
            return ValidationProblem("A reviewer identity is required to record an approval decision.");

        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();
        if (project.Status != VideoProjectStatus.AwaitingApproval)
            return Conflict("The video project is not awaiting approval.");

        var artifacts = await db.ProductionArtifacts.AsNoTracking()
            .Where(x => x.VideoProjectId == id)
            .ToListAsync(cancellationToken);

        var renderArtifact = artifacts.FirstOrDefault(x => x.Type == ProductionArtifactType.Render);
        if (renderArtifact is null || string.IsNullOrWhiteSpace(renderArtifact.ProviderAssetId))
            return Conflict("A rendered video artifact is required before approval.");

        if (!QaPassed(artifacts))
            return Conflict("The project cannot be approved because automated QA did not pass.");

        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Approval,
            ProviderAssetId = renderArtifact.ProviderAssetId,
            Content = request.Notes?.Trim(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                decision = "approved",
                reviewer = request.Reviewer.Trim(),
                decidedAtUtc = DateTime.UtcNow
            })
        });

        project.Status = VideoProjectStatus.Completed;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new ApprovalResponse(project.Id, project.Status.ToString(), "approved", request.Reviewer.Trim()));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApprovalResponse>> Reject(Guid id, ApprovalDecisionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reviewer))
            return ValidationProblem("A reviewer identity is required to record a rejection decision.");
        if (string.IsNullOrWhiteSpace(request.Notes))
            return ValidationProblem("Rejection notes are required so the failure reason is recorded.");

        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();
        if (project.Status != VideoProjectStatus.AwaitingApproval)
            return Conflict("The video project is not awaiting approval.");

        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Approval,
            ProviderAssetId = "rejected",
            Content = request.Notes.Trim(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                decision = "rejected",
                reviewer = request.Reviewer.Trim(),
                decidedAtUtc = DateTime.UtcNow
            })
        });

        project.Status = VideoProjectStatus.Rejected;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new ApprovalResponse(project.Id, project.Status.ToString(), "rejected", request.Reviewer.Trim()));
    }

    private static bool QaPassed(IReadOnlyList<ProductionArtifact> artifacts)
    {
        var qaArtifact = artifacts.FirstOrDefault(x => x.Type == ProductionArtifactType.Qa);
        if (qaArtifact is null || string.IsNullOrWhiteSpace(qaArtifact.Content)) return false;

        try
        {
            using var document = JsonDocument.Parse(qaArtifact.Content);
            return document.RootElement.TryGetProperty("Passed", out var passed) && passed.GetBoolean();
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed record ApprovalDecisionRequest(string Reviewer, string? Notes);
public sealed record ApprovalResponse(Guid VideoProjectId, string Status, string Decision, string Reviewer);
