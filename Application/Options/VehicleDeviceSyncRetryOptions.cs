public sealed class VehicleDeviceSyncRetryOptions
{
    public const string SectionName = "VehicleDeviceSyncRetry";

    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 20;
    public int MaxRetryCount { get; set; } = 5;
    public int RetryIntervalMinutes { get; set; } = 5;
    public int ActiveDelaySeconds { get; set; } = 30;
    public int IdleDelaySeconds { get; set; } = 60;
}
