using System;
using System.Threading;
using System.Threading.Tasks;

public interface IBillingCalculationService
{
    Task<decimal> CalculateProratedAmountAsync(
        int subscriptionId,
        int oldPlanId,
        int newPlanId,
        DateTime effectiveDateUtc,
        CancellationToken ct = default);

    Task<decimal> CalculateSubscriptionAmountAsync(
        AccountSubscription subscription,
        DateTime billDateUtc,
        CancellationToken ct = default);
}
