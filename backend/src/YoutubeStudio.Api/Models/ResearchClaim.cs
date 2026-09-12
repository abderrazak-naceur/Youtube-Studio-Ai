namespace YoutubeStudio.Api.Models;

public sealed class ResearchClaim : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid ResearchProjectId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "unverified";
    public string MetadataJson { get; set; } = "{}";
    public ResearchProject ResearchProject { get; set; } = null!;
    public ICollection<ResearchClaimEvidence> EvidenceLinks { get; set; } = [];
}

public sealed class ResearchClaimEvidence : Entity
{
    public Guid ResearchClaimId { get; set; }
    public Guid ResearchEvidenceId { get; set; }
    public ResearchClaim ResearchClaim { get; set; } = null!;
    public ResearchEvidence ResearchEvidence { get; set; } = null!;
}
