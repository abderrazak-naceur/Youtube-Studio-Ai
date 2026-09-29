namespace YoutubeStudio.Api.Models;

public sealed class FactCheckReport : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid ResearchProjectId { get; set; }

    /// <summary>Overall verdict: passed, needs_review or failed.</summary>
    public string Verdict { get; set; } = "needs_review";

    public int ClaimCount { get; set; }
    public int VerifiedClaimCount { get; set; }
    public int DisputedClaimCount { get; set; }
    public int UnverifiedClaimCount { get; set; }
    public int UnsupportedClaimCount { get; set; }

    /// <summary>True when at least one claim touches a high-risk domain (finance, legal, health, political).</summary>
    public bool RequiresHumanReview { get; set; }

    public ResearchProject ResearchProject { get; set; } = null!;
    public ICollection<FactCheckFinding> Findings { get; set; } = [];
}

public sealed class FactCheckFinding : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid FactCheckReportId { get; set; }
    public Guid ResearchClaimId { get; set; }

    /// <summary>Per-claim outcome: supported, unsupported, disputed or pending.</summary>
    public string Status { get; set; } = "pending";

    /// <summary>Risk level: low, medium or high.</summary>
    public string RiskLevel { get; set; } = "low";

    public int EvidenceCount { get; set; }
    public bool RequiresHumanReview { get; set; }
    public string Rationale { get; set; } = string.Empty;

    public FactCheckReport FactCheckReport { get; set; } = null!;
    public ResearchClaim ResearchClaim { get; set; } = null!;
}
