public sealed class AuditLoggingOptions
{
    public bool Enabled { get; set; } = true;
    public bool LogReadRequests { get; set; }
    public int BatchSize { get; set; } = 50;
    public int FlushIntervalMs { get; set; } = 2000;
    public int MaxRetryCount { get; set; } = 3;
}
