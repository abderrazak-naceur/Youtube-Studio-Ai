namespace YoutubeStudio.Api.Models;

public enum ProductionJobStatus
{
    Queued,
    Running,
    Succeeded,
    Failed
}

public sealed class ProductionJob : Entity
{
    public Guid VideoProjectId { get; set; }
    public ProductionJobStatus Status { get; set; } = ProductionJobStatus.Queued;
    public int Attempt { get; set; } = 1;
    public string? Error { get; set; }
    public string? LastCompletedStage { get; set; }
    public VideoProject VideoProject { get; set; } = null!;
}
