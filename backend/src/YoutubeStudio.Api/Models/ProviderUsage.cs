namespace YoutubeStudio.Api.Models;

/// <summary>
/// A granular, per-call record of an external provider invocation and its cost
/// (AI-COST-MODEL cost ledger, README usage/cost tracking). Distinct from
/// <see cref="ProductionCost"/>, which aggregates per production stage. Append-only.
/// </summary>
public sealed class ProviderUsage : Entity
{
    public Guid? WorkspaceId { get; set; }
    public Guid? VideoProjectId { get; set; }

    public required string Provider { get; set; }
    public required string Model { get; set; }

    /// <summary>Task category (e.g. "Script", "Image", "Tts").</summary>
    public required string TaskType { get; set; }

    public decimal InputUnits { get; set; }
    public decimal OutputUnits { get; set; }
    public decimal UnitPriceUsd { get; set; }
    public decimal TotalCostUsd { get; set; }
    public string Currency { get; set; } = "USD";
}
