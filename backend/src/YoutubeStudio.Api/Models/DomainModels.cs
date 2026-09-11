namespace YoutubeStudio.Api.Models;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Workspace : Entity
{
    public required string Name { get; set; }
    public ICollection<Channel> Channels { get; set; } = [];
    public ICollection<Opportunity> Opportunities { get; set; } = [];
}

public sealed class Channel : Entity
{
    public Guid WorkspaceId { get; set; }
    public required string Name { get; set; }
    public string Platform { get; set; } = "youtube";
    public string? ExternalChannelId { get; set; }
    public Workspace Workspace { get; set; } = null!;
}

public sealed class Opportunity : Entity
{
    public Guid WorkspaceId { get; set; }
    public required string Title { get; set; }
    public string Status { get; set; } = "new";
    public decimal OpportunityScore { get; set; }
    public decimal RevenueScore { get; set; }
    public string? AudienceProblem { get; set; }
    public string? Rationale { get; set; }
    public Workspace Workspace { get; set; } = null!;
}
