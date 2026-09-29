using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Providers;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Generates publish-ready metadata (title, description, tags, category, language) for a
/// video project. Metadata is a mandatory MVP artifact (PRODUCT-REQUIREMENTS P0).
/// </summary>
[ApiController]
[Route("api/v1/video-projects")]
public sealed class MetadataController(
    YoutubeStudioDbContext db,
    IMetadataProvider metadataProvider) : ControllerBase
{
    [HttpPost("{id:guid}/metadata")]
    public async Task<ActionResult<MetadataGenerationResponse>> GenerateMetadata(
        Guid id,
        CancellationToken cancellationToken)
    {
        var project = await db.VideoProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null) return NotFound();
        if (project.Status != VideoProjectStatus.Producing)
            return Conflict("The video project must be producing before generating metadata.");

        var scriptArtifact = await db.ProductionArtifacts.AsNoTracking()
            .Where(x => x.VideoProjectId == id && x.Type == ProductionArtifactType.Script)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (scriptArtifact is null || string.IsNullOrWhiteSpace(scriptArtifact.Content))
            return ValidationProblem("A persisted script is required to generate metadata.");

        var title = string.IsNullOrWhiteSpace(project.Title) ? project.Prompt.Trim() : project.Title.Trim();

        var result = await metadataProvider.GenerateMetadataAsync(
            new MetadataRequest(title, scriptArtifact.Content.Trim(), "en"),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result.Title) || string.IsNullOrWhiteSpace(result.Description))
            return Problem(
                "The metadata provider returned incomplete metadata.",
                statusCode: StatusCodes.Status502BadGateway);

        db.ProductionArtifacts.Add(new ProductionArtifact
        {
            VideoProjectId = project.Id,
            Type = ProductionArtifactType.Metadata,
            ProviderAssetId = "metadata",
            Content = result.Title.Trim(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                title = result.Title.Trim(),
                description = result.Description.Trim(),
                tags = result.Tags,
                category = result.Category,
                language = result.Language
            })
        });

        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new MetadataGenerationResponse(
            project.Id,
            project.Status.ToString(),
            result.Title.Trim(),
            result.Tags.Count));
    }
}

public sealed record MetadataGenerationResponse(
    Guid VideoProjectId,
    string Status,
    string Title,
    int TagCount);
