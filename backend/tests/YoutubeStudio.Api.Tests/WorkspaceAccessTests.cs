using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Tests;

public sealed class WorkspaceAccessTests
{
    [Fact]
    public void GetUserId_reads_name_identifier_claim()
    {
        var access = new WorkspaceAccess(CreateDb());
        var userId = Guid.NewGuid();
        var principal = PrincipalFor(userId);

        Assert.Equal(userId, access.GetUserId(principal));
    }

    [Fact]
    public void GetUserId_returns_null_for_anonymous()
    {
        var access = new WorkspaceAccess(CreateDb());
        Assert.Null(access.GetUserId(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [Fact]
    public async Task GetRole_returns_role_for_member()
    {
        await using var db = CreateDb();
        var (userId, workspaceId) = await AddMembership(db, WorkspaceRole.Reviewer);
        var access = new WorkspaceAccess(db);

        var role = await access.GetRoleAsync(PrincipalFor(userId), workspaceId, CancellationToken.None);

        Assert.Equal(WorkspaceRole.Reviewer, role);
    }

    [Fact]
    public async Task GetRole_returns_null_for_non_member()
    {
        await using var db = CreateDb();
        var (_, workspaceId) = await AddMembership(db, WorkspaceRole.Owner);
        var access = new WorkspaceAccess(db);

        var role = await access.GetRoleAsync(PrincipalFor(Guid.NewGuid()), workspaceId, CancellationToken.None);

        Assert.Null(role);
    }

    [Fact]
    public async Task HasWorkspaceRole_respects_minimum_role()
    {
        await using var db = CreateDb();
        var (userId, workspaceId) = await AddMembership(db, WorkspaceRole.Editor);
        var access = new WorkspaceAccess(db);
        var principal = PrincipalFor(userId);

        Assert.True(await access.HasWorkspaceRoleAsync(principal, workspaceId, WorkspaceRole.Editor, CancellationToken.None));
        Assert.False(await access.HasWorkspaceRoleAsync(principal, workspaceId, WorkspaceRole.Reviewer, CancellationToken.None));
    }

    [Fact]
    public async Task HasWorkspaceRole_true_when_role_exceeds_minimum()
    {
        await using var db = CreateDb();
        var (userId, workspaceId) = await AddMembership(db, WorkspaceRole.Owner);
        var access = new WorkspaceAccess(db);

        Assert.True(await access.HasWorkspaceRoleAsync(PrincipalFor(userId), workspaceId, WorkspaceRole.Reviewer, CancellationToken.None));
    }

    private static ClaimsPrincipal PrincipalFor(Guid userId) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));

    private static async Task<(Guid UserId, Guid WorkspaceId)> AddMembership(YoutubeStudioDbContext db, WorkspaceRole role)
    {
        var user = new User { Email = "u@example.com", PasswordHash = "h" };
        var workspace = new Workspace { Name = "Studio" };
        db.Users.Add(user);
        db.Workspaces.Add(workspace);
        db.Memberships.Add(new Membership { UserId = user.Id, WorkspaceId = workspace.Id, Role = role });
        await db.SaveChangesAsync();
        return (user.Id, workspace.Id);
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(opts);
    }
}
