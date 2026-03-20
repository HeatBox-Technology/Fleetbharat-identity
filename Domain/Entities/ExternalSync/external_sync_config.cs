using System;

public class external_sync_config
{
    public long Id { get; set; }
    public string ModuleName { get; set; } = "";
    public string ServiceInterface { get; set; } = "";
    public string ServiceMethod { get; set; } = "";
    public bool RetryEnabled { get; set; } = true;
    public int MaxRetryCount { get; set; } = 5;
    public int RetryIntervalMinutes { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
