using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IBillingSubscriptionService
{
    Task<int> MapPlanAsync(AccountSubscriptionMapPlanDto dto, CancellationToken ct = default);
    Task<List<AccountSubscriptionResponseDto>> GetSubscriptionsAsync(int skip, int take, CancellationToken ct = default);
    Task<List<AccountSubscriptionResponseDto>> GetSubscriptionsByAccountAsync(int accountId, int skip, int take, CancellationToken ct = default);
    Task<bool> DeleteSubscriptionAsync(int id, CancellationToken ct = default);
}
