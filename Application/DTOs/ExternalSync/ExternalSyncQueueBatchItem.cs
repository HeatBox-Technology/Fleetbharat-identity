using System;

public class ExternalSyncQueueBatchItem
{
    public long Id { get; set; }
    public string ModuleName { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
