using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Services.Ai;

public interface IProviderUsageLedger
{
    /// <summary>
    /// Records a provider invocation and its cost. Persists immediately so usage is captured
    /// even if the surrounding operation later fails (the money was already spent).
    /// </summary>
    Task RecordAsync(
        string provider, string model, string taskType,
        decimal inputUnits, decimal outputUnits, decimal unitPriceUsd,
        Guid? workspaceId = null, Guid? videoProjectId = null,
        CancellationToken cancellationToken = default);
}

public sealed class ProviderUsageLedger(YoutubeStudioDbContext db) : IProviderUsageLedger
{
    public async Task RecordAsync(
        string provider, string model, string taskType,
        decimal inputUnits, decimal outputUnits, decimal unitPriceUsd,
        Guid? workspaceId = null, Guid? videoProjectId = null,
        CancellationToken cancellationToken = default)
    {
        var units = Math.Max(0, inputUnits) + Math.Max(0, outputUnits);
        var total = Math.Round(units * Math.Max(0, unitPriceUsd), 6);

        db.ProviderUsages.Add(new ProviderUsage
        {
            WorkspaceId = workspaceId,
            VideoProjectId = videoProjectId,
            Provider = provider,
            Model = model,
            TaskType = taskType,
            InputUnits = inputUnits,
            OutputUnits = outputUnits,
            UnitPriceUsd = unitPriceUsd,
            TotalCostUsd = total,
            Currency = "USD"
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
