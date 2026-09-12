using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/research-projects/{researchProjectId:guid}/sources")]
public sealed class ResearchSourcesController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResearchSourceResponse>>> GetAll(
        Guid researchProjectId,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(researchProjectId, workspaceId, cancellationToken))
            return NotFound("Research project does not exist in the specified workspace.");

        var sources = await db.ResearchSources
            .AsNoTracking()
            .Where(x => x.ResearchProjectId == researchProjectId && x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ResearchSourceResponse(
                x.Id,
                x.WorkspaceId,
                x.ResearchProjectId,
                x.Url,
                x.Title,
                x.MetadataJson,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(sources);
    }

    [HttpPost]
    public async Task<ActionResult<ResearchSourceResponse>> Create(
        Guid researchProjectId,
        CreateResearchSourceRequest request,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(researchProjectId, request.WorkspaceId, cancellationToken))
            return NotFound("Research project does not exist in the specified workspace.");

        if (!TryValidate(request.Url, request.Title, request.MetadataJson, out var error))
            return BadRequest(error);

        var source = new ResearchSource
        {
            WorkspaceId = request.WorkspaceId,
            ResearchProjectId = researchProjectId,
            Url = request.Url.Trim(),
            Title = request.Title.Trim(),
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson.Trim()
        };

        db.ResearchSources.Add(source);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { researchProjectId, workspaceId = source.WorkspaceId }, ToResponse(source));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ResearchSourceResponse>> Update(
        Guid researchProjectId,
        Guid id,
        UpdateResearchSourceRequest request,
        CancellationToken cancellationToken)
    {
        var source = await db.ResearchSources.SingleOrDefaultAsync(
            x => x.Id == id && x.ResearchProjectId == researchProjectId && x.WorkspaceId == request.WorkspaceId,
            cancellationToken);

        if (source is null)
            return NotFound("Research source does not exist in the specified workspace and project.");

        if (!TryValidate(request.Url, request.Title, request.MetadataJson, out var error))
            return BadRequest(error);

        source.Url = request.Url.Trim();
        source.Title = request.Title.Trim();
        source.MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson.Trim();
        source.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(source));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid researchProjectId,
        Guid id,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var source = await db.ResearchSources.SingleOrDefaultAsync(
            x => x.Id == id && x.ResearchProjectId == researchProjectId && x.WorkspaceId == workspaceId,
            cancellationToken);

        if (source is null)
            return NotFound("Research source does not exist in the specified workspace and project.");

        db.ResearchSources.Remove(source);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Task<bool> ProjectExistsAsync(Guid researchProjectId, Guid workspaceId, CancellationToken cancellationToken) =>
        db.ResearchProjects.AnyAsync(x => x.Id == researchProjectId && x.WorkspaceId == workspaceId, cancellationToken);

    private static bool TryValidate(string url, string title, string? metadataJson, out string error)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            error = "Research source title is required.";
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "Research source URL must be an absolute HTTP or HTTPS URL.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(metadataJson))
        {
            try
            {
                using var _ = System.Text.Json.JsonDocument.Parse(metadataJson);
            }
            catch (System.Text.Json.JsonException)
            {
                error = "Research source metadata must be valid JSON.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static ResearchSourceResponse ToResponse(ResearchSource source) => new(
        source.Id,
        source.WorkspaceId,
        source.ResearchProjectId,
        source.Url,
        source.Title,
        source.MetadataJson,
        source.CreatedAtUtc,
        source.UpdatedAtUtc);
}

public sealed record CreateResearchSourceRequest(Guid WorkspaceId, string Url, string Title, string? MetadataJson = null);

public sealed record UpdateResearchSourceRequest(Guid WorkspaceId, string Url, string Title, string? MetadataJson = null);

public sealed record ResearchSourceResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ResearchProjectId,
    string Url,
    string Title,
    string MetadataJson,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
