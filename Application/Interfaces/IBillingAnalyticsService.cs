using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IBillingAnalyticsService
{
    Task<List<BillingRevenueDto>> GetRevenueProjectionAsync(CancellationToken ct = default);
    Task<List<BillingMarketPenetrationDto>> GetMarketPenetrationAsync(CancellationToken ct = default);
}
