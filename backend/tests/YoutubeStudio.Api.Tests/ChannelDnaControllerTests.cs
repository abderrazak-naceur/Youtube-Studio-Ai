using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class ChannelDnaControllerTests
{
    [Fact]
    public async Task Create_appends_versions()
    {
        await using var db = CreateDb();
        var channel = await AddChannel(db);
        var controller = new ChannelDnaController(db, new StubWorkspaceAccess()).WithUser();

        var first = await controller.Create(channel.Id, Request("v1 audience"), CancellationToken.None);
        var firstDna = Assert.IsType<ChannelDnaResponse>(Assert.IsType<CreatedAtActionResult>(first.Result).Value);
        Assert.Equal(1, firstDna.Version);

        var second = await controller.Create(channel.Id, Request("v2 audience"), CancellationToken.None);
        var secondDna = Assert.IsType<ChannelDnaResponse>(Assert.IsType<CreatedAtActionResult>(second.Result).Value);
        Assert.Equal(2, secondDna.Version);

        Assert.Equal(2, await db.ChannelDnas.CountAsync(x => x.ChannelId == channel.Id));
    }

    [Fact]
    public async Task GetLatest_returns_highest_version()
    {
        await using var db = CreateDb();
        var channel = await AddChannel(db);
        db.ChannelDnas.AddRange(
            new ChannelDna { WorkspaceId = channel.WorkspaceId, ChannelId = channel.Id, Version = 1, Audience = "old" },
            new ChannelDna { WorkspaceId = channel.WorkspaceId, ChannelId = channel.Id, Version = 2, Audience = "new" });
        await db.SaveChangesAsync();

        var result = await new ChannelDnaController(db, new StubWorkspaceAccess()).WithUser().GetLatest(channel.Id, CancellationToken.None);

        var dna = Assert.IsType<ChannelDnaResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(2, dna.Version);
        Assert.Equal("new", dna.Audience);
    }

    [Fact]
    public async Task GetLatest_returns_not_found_without_dna()
    {
        await using var db = CreateDb();
        var channel = await AddChannel(db);

        var result = await new ChannelDnaController(db, new StubWorkspaceAccess()).WithUser().GetLatest(channel.Id, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_returns_not_found_for_non_member()
    {
        await using var db = CreateDb();
        var channel = await AddChannel(db);

        var result = await new ChannelDnaController(db, new StubWorkspaceAccess(null)).WithUser()
            .Create(channel.Id, Request("x"), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static SaveChannelDnaRequest Request(string audience) =>
        new(audience, "Positioning", "Tone", "Visual", "Formats", "Forbidden", "Goals");

    private static async Task<Channel> AddChannel(YoutubeStudioDbContext db)
    {
        var workspace = new Workspace { Name = "Studio" };
        var channel = new Channel { WorkspaceId = workspace.Id, Name = "AI Channel", Platform = "youtube" };
        db.Workspaces.Add(workspace);
        db.Channels.Add(channel);
        await db.SaveChangesAsync();
        return channel;
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(opts);
    }
}
