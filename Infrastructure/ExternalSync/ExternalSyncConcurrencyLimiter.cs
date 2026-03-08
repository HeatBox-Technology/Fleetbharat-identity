using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class ExternalSyncConcurrencyLimiter : IExternalSyncConcurrencyLimiter, IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;

    public ExternalSyncConcurrencyLimiter(int maxConcurrency)
    {
        _semaphore = new SemaphoreSlim(Math.Max(1, maxConcurrency), Math.Max(1, maxConcurrency));
    }

    public async Task<IDisposable> WaitAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        return new Releaser(_semaphore);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _semaphore.Dispose();
        _disposed = true;
    }

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _released;

        public Releaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (_released)
            {
                return;
            }

            _semaphore.Release();
            _released = true;
        }
    }
}
