using System;

public class ExternalSyncRetryPolicyService : IExternalSyncRetryPolicy
{
    public DateTime CalculateNextRetryUtc(int retryCount, DateTime nowUtc, int configuredIntervalMinutes)
    {
        var baselineMinutes = retryCount switch
        {
            1 => 5,
            2 => 15,
            3 => 30,
            4 => 60,
            _ => Math.Max(1, configuredIntervalMinutes)
        };

        // Add low jitter to avoid synchronized retry spikes on small instances.
        var jitterSeconds = Random.Shared.Next(1, 16);
        return nowUtc.AddMinutes(baselineMinutes).AddSeconds(jitterSeconds);
    }
}
