namespace YoutubeStudio.Api.Models;

public enum ProductionJobStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Cancelled
}

public sealed class ProductionJob : Entity
{
    public Guid VideoProjectId { get; set; }
    public ProductionJobStatus Status { get; set; } = ProductionJobStatus.Queued;
    public int Attempt { get; set; } = 1;
    public string? Error { get; set; }
    public string? LastCompletedStage { get; set; }

    /// <summary>
    /// Optional client-supplied key that makes enqueue idempotent, so a retried enqueue
    /// request does not create a duplicate job (API-DESIGN idempotency for expensive mutations).
    /// </summary>
    public string? IdempotencyKey { get; set; }

    public VideoProject VideoProject { get; set; } = null!;
}
