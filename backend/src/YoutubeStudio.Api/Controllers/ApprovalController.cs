using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services;
using YoutubeStudio.Api.Services.Auth;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Human quality gate. No final video can reach <see cref="VideoProjectStatus.Completed"/>
/// (final export) without an explicit approval decision, and no project can be approved
/// unless the automated QA artifact recorded a passing result.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/video-projects")]
public sealed class ApprovalController(YoutubeStudioDbContext db, IAuditService audit, IWorkspaceAccess access) : ControllerBase
{
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApprovalResponse>> Approve(Guid id, ApprovalDecisionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reviewer))
            return ValidationProblem("A reviewer identity is required to record an approval decision.");

        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();
        if (await access.GetRoleAsync(User, project.WorkspaceId, cancellationToken) is null) return NotFound();
        if (!await access.HasWorkspaceRoleAsync(User, project.WorkspaceId, WorkspaceRole.Reviewer, cancellationToken))
            return Forbid();
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

        // AI disclosure gate (SECURITY-COMPLIANCE §5): synthetic content cannot be approved
        // for export until the reviewer acknowledges the required AI disclosure.
        if (project.RequiresAiDisclosure && !request.AiDisclosureAcknowledged)
            return Conflict("This project contains AI-generated content and requires the AI disclosure to be acknowledged before approval.");

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
                aiDisclosureAcknowledged = request.AiDisclosureAcknowledged,
                requiresAiDisclosure = project.RequiresAiDisclosure,
                decidedAtUtc = DateTime.UtcNow
            })
        });

        await ExtractContentGenomeAsync(project, artifacts, cancellationToken);

        audit.Record("video.approved", request.Reviewer.Trim(),
            workspaceId: project.WorkspaceId, videoProjectId: project.Id,
            details: new { renderAssetId = renderArtifact.ProviderAssetId, notes = request.Notes?.Trim(), aiDisclosureAcknowledged = request.AiDisclosureAcknowledged });

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
        if (await access.GetRoleAsync(User, project.WorkspaceId, cancellationToken) is null) return NotFound();
        if (!await access.HasWorkspaceRoleAsync(User, project.WorkspaceId, WorkspaceRole.Reviewer, cancellationToken))
            return Forbid();
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

        audit.Record("video.rejected", request.Reviewer.Trim(),
            workspaceId: project.WorkspaceId, videoProjectId: project.Id,
            details: new { notes = request.Notes.Trim() });

        project.Status = VideoProjectStatus.Rejected;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new ApprovalResponse(project.Id, project.Status.ToString(), "rejected", request.Reviewer.Trim()));
    }

    private async Task ExtractContentGenomeAsync(VideoProject project, IReadOnlyList<ProductionArtifact> artifacts, CancellationToken cancellationToken)
    {
        // One genome per project. Re-approval after a prior extraction is a no-op.
        if (await db.ContentGenomes.AnyAsync(x => x.VideoProjectId == project.Id, cancellationToken))
            return;

        var scriptArtifact = artifacts.FirstOrDefault(x => x.Type == ProductionArtifactType.Script);
        var script = scriptArtifact?.Content ?? project.Script ?? string.Empty;

        var scenePlanArtifact = artifacts.FirstOrDefault(x => x.Type == ProductionArtifactType.ScenePlan);
        var scenes = ParseScenes(scenePlanArtifact?.Content);

        var wordCount = string.IsNullOrWhiteSpace(script)
            ? 0
            : script.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        var attributes = ExtractKeywords(script);

        db.ContentGenomes.Add(new ContentGenome
        {
            VideoProjectId = project.Id,
            Title = string.IsNullOrWhiteSpace(project.Title) ? project.Prompt.Trim() : project.Title.Trim(),
            DurationSeconds = scenes.Sum(x => x.DurationSeconds),
            SceneCount = scenes.Count,
            WordCount = wordCount,
            AttributesJson = JsonSerializer.Serialize(attributes)
        });
    }

    private static List<ScenePlanItem> ParseScenes(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<ScenePlanItem>>(content) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> ExtractKeywords(string script)
    {
        if (string.IsNullOrWhiteSpace(script)) return [];

        return script
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => new string(word.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant())
            .Where(word => word.Length >= 5)
            .GroupBy(word => word)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(10)
            .Select(group => group.Key)
            .ToList();
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

public sealed record ApprovalDecisionRequest(string Reviewer, string? Notes, bool AiDisclosureAcknowledged = false);
public sealed record ApprovalResponse(Guid VideoProjectId, string Status, string Decision, string Reviewer);
