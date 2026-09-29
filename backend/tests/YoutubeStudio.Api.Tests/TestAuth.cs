using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Tests;

/// <summary>
/// Test doubles for authentication/authorization so controller unit tests can exercise
/// tenant-scoped controllers without a real identity provider.
/// </summary>
public static class TestAuth
{
    public static readonly Guid UserId = Guid.NewGuid();

    public static ClaimsPrincipal Principal(Guid? userId = null) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, (userId ?? UserId).ToString())], "test"));

    /// <summary>Attaches an authenticated principal to a controller.</summary>
    public static T WithUser<T>(this T controller, Guid? userId = null) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = Principal(userId) }
        };
        return controller;
    }
}

/// <summary>Grants a fixed role for every workspace, or none when <c>role</c> is null.</summary>
public sealed class StubWorkspaceAccess(WorkspaceRole? role = WorkspaceRole.Owner) : IWorkspaceAccess
{
    public Guid? GetUserId(ClaimsPrincipal principal) => TestAuth.UserId;

    public Task<WorkspaceRole?> GetRoleAsync(ClaimsPrincipal principal, Guid workspaceId, CancellationToken cancellationToken) =>
        Task.FromResult(role);

    public Task<bool> HasWorkspaceRoleAsync(ClaimsPrincipal principal, Guid workspaceId, WorkspaceRole minimumRole, CancellationToken cancellationToken) =>
        Task.FromResult(role is not null && role.Value >= minimumRole);
}
