using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Services.Auth;

/// <summary>
/// Enforces tenant isolation: resolves the authenticated user and checks workspace
/// membership and role (DATA-MODEL §6, SECURITY §1). Authorization is enforced at the
/// application layer through this service before any tenant-scoped data is touched.
/// </summary>
public interface IWorkspaceAccess
{
    /// <summary>The authenticated user id, or null if the principal is not authenticated.</summary>
    Guid? GetUserId(ClaimsPrincipal principal);

    /// <summary>The caller's role in the workspace, or null if they are not a member.</summary>
    Task<WorkspaceRole?> GetRoleAsync(ClaimsPrincipal principal, Guid workspaceId, CancellationToken cancellationToken);

    /// <summary>True when the caller is a member of the workspace with at least the given role.</summary>
    Task<bool> HasWorkspaceRoleAsync(ClaimsPrincipal principal, Guid workspaceId, WorkspaceRole minimumRole, CancellationToken cancellationToken);
}

public sealed class WorkspaceAccess(YoutubeStudioDbContext db) : IWorkspaceAccess
{
    public Guid? GetUserId(ClaimsPrincipal principal)
    {
        // JwtSecurityTokenHandler maps "sub" to NameIdentifier by default; check both.
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public async Task<WorkspaceRole?> GetRoleAsync(ClaimsPrincipal principal, Guid workspaceId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null) return null;

        var membership = await db.Memberships.AsNoTracking()
            .Where(x => x.UserId == userId.Value && x.WorkspaceId == workspaceId)
            .Select(x => (WorkspaceRole?)x.Role)
            .SingleOrDefaultAsync(cancellationToken);

        return membership;
    }

    public async Task<bool> HasWorkspaceRoleAsync(ClaimsPrincipal principal, Guid workspaceId, WorkspaceRole minimumRole, CancellationToken cancellationToken)
    {
        var role = await GetRoleAsync(principal, workspaceId, cancellationToken);
        return role is not null && role.Value >= minimumRole;
    }
}
