using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Data;

/// <summary>
/// Helper for computing the next artifact version so regenerating an artifact appends a
/// new version instead of silently overwriting the previous one (DATA-MODEL §3).
/// </summary>
public static class ProductionArtifactVersioning
{
    public static async Task<int> NextVersionAsync(
        this YoutubeStudioDbContext db, Guid videoProjectId, ProductionArtifactType type, CancellationToken cancellationToken)
    {
        var current = await db.ProductionArtifacts
            .Where(x => x.VideoProjectId == videoProjectId && x.Type == type)
            .Select(x => (int?)x.Version)
            .MaxAsync(cancellationToken);
        return (current ?? 0) + 1;
    }
}
