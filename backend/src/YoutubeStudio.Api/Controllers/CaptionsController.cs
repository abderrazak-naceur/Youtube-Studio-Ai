using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/video-projects")]
public sealed class CaptionsController(
    YoutubeStudioDbContext db,
    ICaptionProvider captionProvider) : ControllerBase
{
    [HttpPost("{id:guid}/captions")]
    public async Task<ActionResult<CaptionGenerationResponse>> GenerateCaptions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();
        if (project.Status != VideoProjectStatus.Producing)
            return Conflict("The video project must be producing before generating captions.");

        var artifacts = await db.ProductionArtifacts.AsNoTracking()
            .Where(x => x.VideoProjectId == id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var scriptArtifact = artifacts.FirstOrDefault(x => x.Type == ProductionArtifactType.Script);
        if (scriptArtifact is null || string.IsNullOrWhiteSpace(scriptArtifact.Content))
            return ValidationProblem("A persisted script is required to generate captions.");

        var voiceArtifact = artifacts.FirstOrDefault(x => x.Type == ProductionArtifactType.Voice);
        if (voiceArtifact is null || string.IsNullOrWhiteSpace(voiceArtifact.ProviderAssetId))
            return ValidationProblem("A persisted voice artifact is required to generate captions.");

        var musicArtifact = artifacts.FirstOrDefault(x => x.Type == ProductionArtifactType.MusicSfx);
        if (musicArtifact is null || string.IsNullOrWhiteSpace(musicArtifact.ProviderAssetId))
            return ValidationProblem("A completed Music/SFX artifact is required to generate captions.");

        if (!TryGetVoiceDurationSeconds(voiceArtifact.MetadataJson, out var durationSeconds) || durationSeconds <= 0)
            return ValidationProblem("The persisted voice artifact does not contain a valid production duration.");

        var result = await captionProvider.GenerateCaptionsAsync(
            new CaptionRequest(scriptArtifact.Content.Trim()),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result.ProviderAssetId) || !AreValidEntries(result.Entries, durationSeconds))
            return Problem(
                "The caption provider returned invalid timed caption output.",
                statusCode: StatusCodes.Status502BadGateway);

        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Captions,
            ProviderAssetId = result.ProviderAssetId.Trim(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                durationSeconds,
                entries = result.Entries
            })
        });

        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new CaptionGenerationResponse(
            project.Id,
            project.Status.ToString(),
            result.ProviderAssetId.Trim(),
            result.Entries.Count));
    }

    private static bool TryGetVoiceDurationSeconds(string? metadataJson, out double durationSeconds)
    {
        durationSeconds = 0;
        if (string.IsNullOrWhiteSpace(metadataJson)) return false;

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (!document.RootElement.TryGetProperty("durationSeconds", out var duration)) return false;
            durationSeconds = duration.GetDouble();
            return double.IsFinite(durationSeconds);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool AreValidEntries(IReadOnlyList<CaptionEntry>? entries, double durationSeconds)
    {
        if (entries is null || entries.Count == 0) return false;

        var previousEnd = 0d;
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Text) ||
                !double.IsFinite(entry.StartSeconds) ||
                !double.IsFinite(entry.EndSeconds) ||
                entry.StartSeconds < 0 ||
                entry.EndSeconds <= entry.StartSeconds ||
                entry.StartSeconds < previousEnd ||
                entry.EndSeconds > durationSeconds)
                return false;

            previousEnd = entry.EndSeconds;
        }

        return true;
    }
}

public sealed record CaptionGenerationResponse(
    Guid VideoProjectId,
    string Status,
    string ProviderAssetId,
    int EntryCount);
