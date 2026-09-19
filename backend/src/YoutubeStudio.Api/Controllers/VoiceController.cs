using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/video-projects")]
public sealed class VoiceController(
    YoutubeStudioDbContext db,
    IVoiceProvider voiceProvider) : ControllerBase
{
    [HttpPost("{id:guid}/voice")]
    public async Task<ActionResult<VoiceGenerationResponse>> GenerateVoice(
        Guid id,
        GenerateVoiceRequest request,
        CancellationToken cancellationToken)
    {
        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();
        if (project.Status != VideoProjectStatus.Producing)
            return Conflict("The video project must be producing before generating voice.");

        var scriptArtifact = await db.ProductionArtifacts.AsNoTracking()
            .Where(x => x.VideoProjectId == id && x.Type == ProductionArtifactType.Script)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (scriptArtifact is null || string.IsNullOrWhiteSpace(scriptArtifact.Content))
            return ValidationProblem("A persisted script is required to generate voice.");

        var script = scriptArtifact.Content.Trim();
        var result = await voiceProvider.GenerateVoiceAsync(
            new VoiceRequest(script, string.IsNullOrWhiteSpace(request.VoiceId) ? null : request.VoiceId.Trim()),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result.ProviderAssetId) || string.IsNullOrWhiteSpace(result.MediaType))
            return Problem(
                "The voice provider returned an invalid asset.",
                statusCode: StatusCodes.Status502BadGateway);

        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Voice,
            ProviderAssetId = result.ProviderAssetId.Trim(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                mediaType = result.MediaType.Trim(),
                durationSeconds = result.Duration.TotalSeconds,
                voiceId = request.VoiceId?.Trim()
            })
        });

        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new VoiceGenerationResponse(project.Id, project.Status.ToString(), result.ProviderAssetId.Trim()));
    }
}

public sealed record GenerateVoiceRequest(string? VoiceId);
public sealed record VoiceGenerationResponse(Guid VideoProjectId, string Status, string ProviderAssetId);
