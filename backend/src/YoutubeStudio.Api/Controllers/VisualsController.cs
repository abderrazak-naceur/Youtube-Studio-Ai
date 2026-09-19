using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/video-projects")]
public sealed class VisualsController(
    YoutubeStudioDbContext db,
    IVisualProvider visualProvider) : ControllerBase
{
    [HttpPost("{id:guid}/visuals")]
    public async Task<ActionResult<VisualGenerationResponse>> GenerateVisuals(
        Guid id,
        CancellationToken cancellationToken)
    {
        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();
        if (project.Status != VideoProjectStatus.Planned)
            return Conflict("The video project must be planned before generating visuals.");

        var scenePlanArtifact = await db.ProductionArtifacts.AsNoTracking()
            .Where(x => x.VideoProjectId == id && x.Type == ProductionArtifactType.ScenePlan)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (scenePlanArtifact is null || string.IsNullOrWhiteSpace(scenePlanArtifact.Content))
            return ValidationProblem("A persisted scene plan is required to generate visuals.");

        List<ScenePlanItem>? scenes;
        try
        {
            scenes = JsonSerializer.Deserialize<List<ScenePlanItem>>(scenePlanArtifact.Content);
        }
        catch (JsonException)
        {
            return Problem("The persisted scene plan is invalid.");
        }

        if (scenes is not { Count: > 0 } || scenes.Any(scene =>
                scene.Number <= 0 ||
                scene.DurationSeconds <= 0 ||
                string.IsNullOrWhiteSpace(scene.VisualDirection)))
            return Problem("The persisted scene plan is invalid.");

        var generatedArtifacts = new List<ProductionArtifact>(scenes.Count);
        foreach (var scene in scenes.OrderBy(x => x.Number))
        {
            var result = await visualProvider.GenerateVisualAsync(
                new VisualRequest(scene.VisualDirection.Trim(), scene.DurationSeconds),
                cancellationToken);

            if (string.IsNullOrWhiteSpace(result.ProviderAssetId) || string.IsNullOrWhiteSpace(result.MediaType))
                return Problem(
                    "The visual provider returned an invalid asset.",
                    statusCode: StatusCodes.Status502BadGateway);

            generatedArtifacts.Add(new ProductionArtifact
            {
                VideoProjectId = project.Id,
                Type = ProductionArtifactType.Visual,
                ProviderAssetId = result.ProviderAssetId.Trim(),
                MetadataJson = JsonSerializer.Serialize(new
                {
                    sceneNumber = scene.Number,
                    mediaType = result.MediaType.Trim(),
                    durationSeconds = scene.DurationSeconds
                })
            });
        }

        db.ProductionArtifacts.AddRange(generatedArtifacts);
        project.Status = VideoProjectStatus.Producing;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new VisualGenerationResponse(
            project.Id,
            project.Status.ToString(),
            generatedArtifacts.Count));
    }
}

public sealed record VisualGenerationResponse(Guid VideoProjectId, string Status, int VisualCount);
