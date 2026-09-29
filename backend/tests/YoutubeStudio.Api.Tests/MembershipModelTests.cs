using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Tests;

public sealed class MembershipModelTests
{
    [Fact]
    public async Task User_can_be_a_member_of_a_workspace_with_a_role()
    {
        await using var db = CreateDb();
        var user = new User { Email = "creator@example.com", PasswordHash = "hash", DisplayName = "Creator" };
        var workspace = new Workspace { Name = "Studio" };
        db.Users.Add(user);
        db.Workspaces.Add(workspace);
        db.Memberships.Add(new Membership { UserId = user.Id, WorkspaceId = workspace.Id, Role = WorkspaceRole.Owner });
        await db.SaveChangesAsync();

        var membership = await db.Memberships.SingleAsync(x => x.UserId == user.Id && x.WorkspaceId == workspace.Id);
        Assert.Equal(WorkspaceRole.Owner, membership.Role);
    }

    [Fact]
    public async Task Editor_is_the_default_role()
    {
        await using var db = CreateDb();
        var user = new User { Email = "editor@example.com", PasswordHash = "hash" };
        var workspace = new Workspace { Name = "Studio" };
        db.Users.Add(user);
        db.Workspaces.Add(workspace);
        db.Memberships.Add(new Membership { UserId = user.Id, WorkspaceId = workspace.Id });
        await db.SaveChangesAsync();

        var membership = await db.Memberships.SingleAsync();
        Assert.Equal(WorkspaceRole.Editor, membership.Role);
    }

    [Fact]
    public void Role_ordering_allows_capability_comparison()
    {
        Assert.True(WorkspaceRole.Owner > WorkspaceRole.Reviewer);
        Assert.True(WorkspaceRole.Reviewer > WorkspaceRole.Editor);
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(options);
    }
}
