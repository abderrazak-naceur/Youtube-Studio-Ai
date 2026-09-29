using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Auth;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/video-projects")]
public sealed class ThumbnailsController(
    YoutubeStudioDbContext db,
    IThumbnailProvider thumbnailProvider,
    IWorkspaceAccess access) : ControllerBase
{
    private const int CandidateCount = 3;

    [HttpPost("{id:guid}/thumbnails")]
    public async Task<ActionResult<ThumbnailGenerationResponse>> GenerateThumbnails(
        Guid id,
        CancellationToken cancellationToken)
    {
        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();
        if (await access.GetRoleAsync(User, project.WorkspaceId, cancellationToken) is null) return NotFound();
        if (project.Status != VideoProjectStatus.Producing)
            return Conflict("The video project must be producing before generating thumbnails.");

        var scriptArtifact = await db.ProductionArtifacts.AsNoTracking()
            .Where(x => x.VideoProjectId == id && x.Type == ProductionArtifactType.Script)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (scriptArtifact is null || string.IsNullOrWhiteSpace(scriptArtifact.Content))
            return ValidationProblem("A persisted script is required to generate thumbnails.");

        var title = string.IsNullOrWhiteSpace(project.Title) ? project.Prompt.Trim() : project.Title.Trim();
        var script = scriptArtifact.Content.Trim();

        var result = await thumbnailProvider.GenerateThumbnailsAsync(
            new ThumbnailRequest(title, script, CandidateCount),
            cancellationToken);

        if (!AreValidCandidates(result.Candidates))
            return Problem(
                "The thumbnail provider returned no valid candidates.",
                statusCode: StatusCodes.Status502BadGateway);

        var candidates = result.Candidates
            .Select(candidate => new
            {
                providerAssetId = candidate.ProviderAssetId.Trim(),
                headline = candidate.Headline.Trim(),
                mediaType = candidate.MediaType.Trim()
            })
            .ToArray();

        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Thumbnail,
            ProviderAssetId = candidates[0].providerAssetId,
            MetadataJson = JsonSerializer.Serialize(new { candidates })
        });

        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new ThumbnailGenerationResponse(
            project.Id,
            project.Status.ToString(),
            candidates.Length,
            candidates[0].providerAssetId));
    }

    private static bool AreValidCandidates(IReadOnlyList<ThumbnailCandidate>? candidates)
    {
        if (candidates is null || candidates.Count == 0) return false;

        return candidates.All(candidate =>
            !string.IsNullOrWhiteSpace(candidate.ProviderAssetId) &&
            !string.IsNullOrWhiteSpace(candidate.Headline) &&
            !string.IsNullOrWhiteSpace(candidate.MediaType));
    }
}

public sealed record ThumbnailGenerationResponse(
    Guid VideoProjectId,
    string Status,
    int CandidateCount,
    string PrimaryProviderAssetId);
