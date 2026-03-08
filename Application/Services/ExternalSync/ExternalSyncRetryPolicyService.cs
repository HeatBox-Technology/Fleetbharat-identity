using System;

public class ExternalSyncRetryPolicyService : IExternalSyncRetryPolicy
{
    public DateTime CalculateNextRetryUtc(int retryCount, DateTime nowUtc, int configuredIntervalMinutes)
    {
        // Exponential steps as requested.
        var minutes = retryCount switch
        {
            1 => 5,
            2 => 15,
            3 => 30,
            4 => 60,
            _ => Math.Max(1, configuredIntervalMinutes)
        };

        return nowUtc.AddMinutes(minutes);
    }
}
