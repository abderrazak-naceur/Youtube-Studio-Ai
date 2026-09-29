using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Services.Ai;

namespace YoutubeStudio.Api.Tests;

public sealed class ProviderUsageLedgerTests
{
    [Fact]
    public async Task Record_persists_usage_with_computed_total()
    {
        await using var db = CreateDb();
        var ledger = new ProviderUsageLedger(db);
        var projectId = Guid.NewGuid();

        await ledger.RecordAsync("openai", "gpt-x", "Script",
            inputUnits: 1000, outputUnits: 500, unitPriceUsd: 0.00001m,
            videoProjectId: projectId, cancellationToken: CancellationToken.None);

        var usage = await db.ProviderUsages.SingleAsync();
        Assert.Equal("openai", usage.Provider);
        Assert.Equal("Script", usage.TaskType);
        Assert.Equal(projectId, usage.VideoProjectId);
        Assert.Equal(0.015m, usage.TotalCostUsd); // (1000+500) * 0.00001
        Assert.Equal("USD", usage.Currency);
    }

    [Fact]
    public async Task Record_clamps_negative_units_and_price()
    {
        await using var db = CreateDb();
        var ledger = new ProviderUsageLedger(db);

        await ledger.RecordAsync("p", "m", "Tts", inputUnits: -5, outputUnits: 10, unitPriceUsd: -1, cancellationToken: CancellationToken.None);

        var usage = await db.ProviderUsages.SingleAsync();
        Assert.Equal(0m, usage.TotalCostUsd);
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(opts);
    }
}
