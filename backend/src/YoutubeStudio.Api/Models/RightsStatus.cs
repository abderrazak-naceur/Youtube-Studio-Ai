namespace YoutubeStudio.Api.Models;

/// <summary>
/// Rights/provenance status for a media asset (SECURITY-COMPLIANCE §4).
/// <see cref="Unknown"/> must never be treated as safe by default.
/// </summary>
public enum RightsStatus
{
    Unknown = 0,
    Licensed,
    Owned,
    Generated,
    PublicDomain
}

/// <summary>
/// Provenance/rights metadata attached to every generated or acquired media asset,
/// mirroring the asset-manifest fields in VIDEO-PRODUCTION-PIPELINE.
/// </summary>
public sealed record AssetProvenance(
    string Provider,
    string? Model,
    RightsStatus RightsStatus,
    string ContentHash,
    string? PromptReference,
    DateTime GeneratedAtUtc);
