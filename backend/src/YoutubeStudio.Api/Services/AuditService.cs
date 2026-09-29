using System.Text.Json;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Services;

public interface IAuditService
{
    /// <summary>
    /// Appends an immutable audit record. Does not call SaveChanges; the caller persists
    /// the record within its own unit of work so the audit entry is atomic with the action.
    /// </summary>
    void Record(string action, string actor, Guid? workspaceId = null, Guid? videoProjectId = null, object? details = null);
}

public sealed class AuditService(YoutubeStudioDbContext db) : IAuditService
{
    public void Record(string action, string actor, Guid? workspaceId = null, Guid? videoProjectId = null, object? details = null)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            Action = action,
            Actor = string.IsNullOrWhiteSpace(actor) ? "unknown" : actor.Trim(),
            WorkspaceId = workspaceId,
            VideoProjectId = videoProjectId,
            DetailsJson = details is null ? null : JsonSerializer.Serialize(details)
        });
    }
}
