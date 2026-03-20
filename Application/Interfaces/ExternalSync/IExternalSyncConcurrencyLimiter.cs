using System;
using System.Threading;
using System.Threading.Tasks;

public interface IExternalSyncConcurrencyLimiter
{
    Task<IDisposable> WaitAsync(CancellationToken ct = default);
}
