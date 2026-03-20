using System;

public interface IExternalSyncRetryPolicy
{
    DateTime CalculateNextRetryUtc(int retryCount, DateTime nowUtc, int configuredIntervalMinutes);
}
