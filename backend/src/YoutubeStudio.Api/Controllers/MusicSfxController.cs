using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/video-projects")]
public sealed class MusicSfxController(
    YoutubeStudioDbContext db,
    IMusicSfxProvider musicSfxProvider) : ControllerBase
{
    [HttpPost("{id:guid}/music-sfx")]
    public async Task<ActionResult<MusicSfxGenerationResponse>> GenerateMusicSfx(
        Guid id,
        CancellationToken cancellationToken)
    {
        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();
        if (project.Status != VideoProjectStatus.Producing)
            return Conflict("The video project must be producing before generating music/SFX.");

        var scriptArtifact = await db.ProductionArtifacts.AsNoTracking()
            .Where(x => x.VideoProjectId == id && x.Type == ProductionArtifactType.Script)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (scriptArtifact is null || string.IsNullOrWhiteSpace(scriptArtifact.Content))
            return ValidationProblem("A persisted script is required to generate music/SFX.");

        var scenePlanArtifact = await db.ProductionArtifacts.AsNoTracking()
            .Where(x => x.VideoProjectId == id && x.Type == ProductionArtifactType.ScenePlan)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (scenePlanArtifact is null || string.IsNullOrWhiteSpace(scenePlanArtifact.Content))
            return ValidationProblem("A persisted scene plan is required to determine the music/SFX duration.");

        List<ScenePlanItem>? scenes;
        try
        {
            scenes = JsonSerializer.Deserialize<List<ScenePlanItem>>(scenePlanArtifact.Content);
        }
        catch (JsonException)
        {
            return Problem("The persisted scene plan is invalid.");
        }

        if (scenes is not { Count: > 0 } || scenes.Any(scene => scene.Number <= 0 || scene.DurationSeconds <= 0))
            return Problem("The persisted scene plan is invalid.");

        var durationSeconds = scenes.Sum(scene => scene.DurationSeconds);
        var title = string.IsNullOrWhiteSpace(project.Title) ? project.Prompt.Trim() : project.Title.Trim();
        var script = scriptArtifact.Content.Trim();

        var result = await musicSfxProvider.GenerateMusicSfxAsync(
            new MusicSfxRequest(title, script, durationSeconds),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result.ProviderAssetId) || string.IsNullOrWhiteSpace(result.MediaType))
            return Problem(
                "The music/SFX provider returned an invalid asset.",
                statusCode: StatusCodes.Status502BadGateway);

        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.MusicSfx,
            ProviderAssetId = result.ProviderAssetId.Trim(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                mediaType = result.MediaType.Trim(),
                durationSeconds
            })
        });

        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new MusicSfxGenerationResponse(
            project.Id,
            project.Status.ToString(),
            result.ProviderAssetId.Trim()));
    }
}

public sealed record MusicSfxGenerationResponse(Guid VideoProjectId, string Status, string ProviderAssetId);
