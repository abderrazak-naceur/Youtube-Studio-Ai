using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services;

namespace YoutubeStudio.Api.Tests;

public sealed class DomainEventsTests
{
    [Fact]
    public async Task Publisher_appends_event_on_save()
    {
        await using var db = CreateDb();
        var workspaceId = Guid.NewGuid();
        var publisher = new DomainEventPublisher(db);

        publisher.Publish("opportunity.created", workspaceId, Guid.NewGuid(), new { title = "AI" });
        await db.SaveChangesAsync();

        var evt = await db.DomainEvents.SingleAsync();
        Assert.Equal("opportunity.created", evt.Type);
        Assert.Equal(workspaceId, evt.WorkspaceId);
        Assert.Contains("AI", evt.Payload);
    }

    [Fact]
    public async Task Endpoint_returns_workspace_events_filtered_by_type()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Studio" };
        db.Workspaces.Add(workspace);
        db.DomainEvents.AddRange(
            new DomainEvent { Type = "opportunity.created", WorkspaceId = workspace.Id, OccurredAtUtc = DateTime.UtcNow.AddMinutes(-1) },
            new DomainEvent { Type = "video.approved", WorkspaceId = workspace.Id, OccurredAtUtc = DateTime.UtcNow },
            new DomainEvent { Type = "video.approved", WorkspaceId = Guid.NewGuid(), OccurredAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = new DomainEventsController(db, new StubWorkspaceAccess()).WithUser();

        var all = await controller.GetForWorkspace(workspace.Id, null, CancellationToken.None);
        var allEvents = Assert.IsAssignableFrom<IReadOnlyList<DomainEventResponse>>(Assert.IsType<OkObjectResult>(all.Result).Value);
        Assert.Equal(2, allEvents.Count);

        var filtered = await controller.GetForWorkspace(workspace.Id, "video.approved", CancellationToken.None);
        var filteredEvents = Assert.IsAssignableFrom<IReadOnlyList<DomainEventResponse>>(Assert.IsType<OkObjectResult>(filtered.Result).Value);
        Assert.Single(filteredEvents);
    }

    [Fact]
    public async Task Endpoint_forbids_non_member()
    {
        await using var db = CreateDb();
        var workspace = new Workspace { Name = "Studio" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var controller = new DomainEventsController(db, new StubWorkspaceAccess(null)).WithUser();
        var result = await controller.GetForWorkspace(workspace.Id, null, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(opts);
    }
}
