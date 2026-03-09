using System.Threading;
using System.Threading.Tasks;

public interface IBillingRetryService
{
    Task<int> ProcessPendingRetriesAsync(int take, CancellationToken ct = default);
}
