using System;

public class external_sync_queue
{
    public long Id { get; set; }
    public string ModuleName { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public string Status { get; set; } = ExternalSyncStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public DateTime? NextRetryTime { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAt { get; set; }
}
