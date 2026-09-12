namespace YoutubeStudio.Api.Models;

public sealed class ResearchProject : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid OpportunityId { get; set; }
    public string Status { get; set; } = "draft";
    public Opportunity Opportunity { get; set; } = null!;
    public ICollection<ResearchSource> Sources { get; set; } = [];
}
