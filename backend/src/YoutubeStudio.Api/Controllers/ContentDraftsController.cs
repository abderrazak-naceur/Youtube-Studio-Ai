using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Controllers;

[ApiController]
[Route("api/v1/research-projects/{researchProjectId:guid}/content-draft")]
public sealed class ContentDraftsController(YoutubeStudioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ContentDraftResponse>> Get(
        Guid researchProjectId,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(researchProjectId, workspaceId, cancellationToken))
            return NotFound("Research project does not exist in the specified workspace.");

        var draft = await db.ContentDrafts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ResearchProjectId == researchProjectId && x.WorkspaceId == workspaceId, cancellationToken);
        if (draft is null) return NotFound("Content draft has not been generated for this project.");
        return Ok(ToResponse(draft));
    }

    [HttpPost]
    public async Task<ActionResult<ContentDraftResponse>> Generate(
        Guid researchProjectId,
        GenerateContentDraftRequest request,
        CancellationToken cancellationToken)
    {
        var project = await db.ResearchProjects
            .Include(x => x.Opportunity)
            .Include(x => x.Sources)
            .SingleOrDefaultAsync(x => x.Id == researchProjectId && x.WorkspaceId == request.WorkspaceId, cancellationToken);
        if (project is null) return NotFound("Research project does not exist in the specified workspace.");

        var factCheck = await db.FactCheckReports.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ResearchProjectId == researchProjectId && x.WorkspaceId == request.WorkspaceId, cancellationToken);
        if (factCheck is null) return BadRequest("Run a fact check before generating content.");
        if (factCheck.Verdict == "failed") return BadRequest("Content cannot be generated while the fact check verdict is failed.");

        var verifiedClaims = await db.ResearchClaims.AsNoTracking()
            .Where(x => x.ResearchProjectId == researchProjectId && x.WorkspaceId == request.WorkspaceId && x.VerificationStatus == "verified")
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => x.Text)
            .ToListAsync(cancellationToken);
        if (verifiedClaims.Count == 0) return BadRequest("At least one verified claim is required before content can be generated.");

        var topic = string.IsNullOrWhiteSpace(project.Opportunity.Title) ? "this topic" : project.Opportunity.Title.Trim();
        var sources = project.Sources.OrderBy(s => s.Title).ToList();
        var status = factCheck.Verdict == "passed" && !factCheck.RequiresHumanReview ? "ready" : "draft";

        var draft = await db.ContentDrafts.SingleOrDefaultAsync(x => x.ResearchProjectId == researchProjectId && x.WorkspaceId == request.WorkspaceId, cancellationToken);
        if (draft is null)
        {
            draft = new ContentDraft { WorkspaceId = request.WorkspaceId, ResearchProjectId = researchProjectId };
            db.ContentDrafts.Add(draft);
        }

        draft.Angle = $"A clear, evidence-based explanation of {topic} that turns {verifiedClaims.Count} verified finding{(verifiedClaims.Count == 1 ? "" : "s")} into a story the viewer can act on.";
        draft.Hook = $"What most people get wrong about {topic} — and what the evidence actually says.";
        draft.Outline = BuildOutline(topic, verifiedClaims);
        draft.Script = BuildScript(topic, verifiedClaims);
        draft.TitleCandidatesJson = Serialize(BuildTitles(topic));
        draft.ThumbnailConceptsJson = Serialize(BuildThumbnails(topic));
        draft.Description = BuildDescription(topic, sources);
        draft.ChaptersJson = Serialize(BuildChapters(verifiedClaims));
        draft.TagsJson = Serialize(BuildTags(topic));
        draft.Status = status;
        draft.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { researchProjectId, workspaceId = draft.WorkspaceId }, ToResponse(draft));
    }

    private static string BuildOutline(string topic, IReadOnlyList<string> claims)
    {
        var lines = new List<string> { $"1. Hook — the surprising truth about {topic}", "2. Why this matters to the viewer" };
        for (var i = 0; i < claims.Count; i++) lines.Add($"{i + 3}. Key point: {Summarize(claims[i])}");
        lines.Add($"{claims.Count + 3}. Recap and call to action");
        return string.Join("\n", lines);
    }

    private static string BuildScript(string topic, IReadOnlyList<string> claims)
    {
        var script = new StringBuilder();
        script.AppendLine($"Here's what the evidence really says about {topic}.");
        script.AppendLine();
        foreach (var claim in claims)
        {
            script.AppendLine(claim.Trim());
            script.AppendLine();
        }
        script.AppendLine("If that was useful, subscribe for more evidence-based breakdowns.");
        return script.ToString().TrimEnd() + "\n";
    }

    private static IReadOnlyList<string> BuildTitles(string topic) =>
    [
        $"The Truth About {topic}",
        $"{topic}: What the Evidence Actually Says",
        $"Everything You Got Wrong About {topic}",
        $"{topic}, Explained (Backed by Sources)"
    ];

    private static IReadOnlyList<string> BuildThumbnails(string topic) =>
    [
        $"Bold text \"{topic}?\" over a surprised face with a red circle on the key detail.",
        $"Split screen: myth vs. evidence, with \"{topic}\" as the headline.",
        "Clean single-word thumbnail with a checkmark to signal a verified, trustworthy take."
    ];

    private static string BuildDescription(string topic, IReadOnlyList<ResearchSource> sources)
    {
        var description = new StringBuilder();
        description.AppendLine($"In this video we break down {topic} using verified research and cite every source.");
        description.AppendLine();
        if (sources.Count > 0)
        {
            description.AppendLine("Sources:");
            foreach (var source in sources) description.AppendLine($"- {source.Title}: {source.Url}");
        }
        return description.ToString().TrimEnd() + "\n";
    }

    private static IReadOnlyList<ChapterMarker> BuildChapters(IReadOnlyList<string> claims)
    {
        var chapters = new List<ChapterMarker> { new("0:00", "Intro") };
        var seconds = 30;
        foreach (var claim in claims)
        {
            chapters.Add(new ChapterMarker($"{seconds / 60}:{seconds % 60:D2}", Summarize(claim)));
            seconds += 60;
        }
        chapters.Add(new ChapterMarker($"{seconds / 60}:{seconds % 60:D2}", "Recap & CTA"));
        return chapters;
    }

    private static IReadOnlyList<string> BuildTags(string topic)
    {
        var words = topic.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var tags = new List<string> { topic.ToLowerInvariant(), "explained", "evidence based", "research" };
        tags.AddRange(words.Where(w => w.Length > 3));
        return tags.Distinct().ToList();
    }

    private static string Summarize(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length <= 60 ? trimmed : trimmed[..57].TrimEnd() + "...";
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value);

    private Task<bool> ProjectExistsAsync(Guid projectId, Guid workspaceId, CancellationToken cancellationToken) =>
        db.ResearchProjects.AnyAsync(x => x.Id == projectId && x.WorkspaceId == workspaceId, cancellationToken);

    private static ContentDraftResponse ToResponse(ContentDraft draft) => new(
        draft.Id, draft.WorkspaceId, draft.ResearchProjectId, draft.Angle, draft.Hook, draft.Outline, draft.Script,
        Deserialize(draft.TitleCandidatesJson), Deserialize(draft.ThumbnailConceptsJson), draft.Description,
        DeserializeChapters(draft.ChaptersJson), Deserialize(draft.TagsJson), draft.Status, draft.CreatedAtUtc, draft.UpdatedAtUtc);

    private static IReadOnlyList<string> Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static IReadOnlyList<ChapterMarker> DeserializeChapters(string json)
    {
        try { return JsonSerializer.Deserialize<List<ChapterMarker>>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}

public sealed record ChapterMarker(string Timestamp, string Title);
public sealed record GenerateContentDraftRequest(Guid WorkspaceId);
public sealed record ContentDraftResponse(
    Guid Id, Guid WorkspaceId, Guid ResearchProjectId, string Angle, string Hook, string Outline, string Script,
    IReadOnlyList<string> TitleCandidates, IReadOnlyList<string> ThumbnailConcepts, string Description,
    IReadOnlyList<ChapterMarker> Chapters, IReadOnlyList<string> Tags, string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
