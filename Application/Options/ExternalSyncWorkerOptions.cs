public class ExternalSyncWorkerOptions
{
    public const string SectionName = "ExternalSyncWorker";

    public int BatchSize { get; set; } = 25;
    public int MaxConcurrency { get; set; } = 5;
    public int ActiveDelaySeconds { get; set; } = 10;
    public int IdleDelaySeconds { get; set; } = 60;
    public int SuccessRetentionDays { get; set; } = 7;
    public int CleanupBatchSize { get; set; } = 250;
}
