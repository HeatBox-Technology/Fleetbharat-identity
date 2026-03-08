using System;

public class external_sync_dead_letter
{
    public long Id { get; set; }
    public string ModuleName { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public int RetryCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime MovedToDLQAt { get; set; } = DateTime.UtcNow;
}
