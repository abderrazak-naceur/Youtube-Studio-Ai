using System.Text.Json;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Services;

public interface IDomainEventPublisher
{
    /// <summary>
    /// Appends an immutable domain event to the current unit of work. The caller persists it
    /// within its own transaction so the event is atomic with the state change it records.
    /// </summary>
    void Publish(string type, Guid? workspaceId = null, Guid? aggregateId = null, object? payload = null, int schemaVersion = 1);
}

public sealed class DomainEventPublisher(YoutubeStudioDbContext db) : IDomainEventPublisher
{
    public void Publish(string type, Guid? workspaceId = null, Guid? aggregateId = null, object? payload = null, int schemaVersion = 1)
    {
        db.DomainEvents.Add(new DomainEvent
        {
            Type = type,
            WorkspaceId = workspaceId,
            AggregateId = aggregateId,
            OccurredAtUtc = DateTime.UtcNow,
            SchemaVersion = schemaVersion,
            Payload = payload is null ? null : JsonSerializer.Serialize(payload)
        });
    }
}
