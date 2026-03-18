using System;

public class ExternalApiLog
{
    public long Id { get; set; }
    public string ServiceName { get; set; } = "";
    public string Payload { get; set; } = "";
    public string? Response { get; set; }
    public string Status { get; set; } = ExternalSyncStatus.Pending;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRetryAt { get; set; }
}
