using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class BillingCalculationService : IBillingCalculationService
{
    private readonly IBillingRepository _repo;

    public BillingCalculationService(IBillingRepository repo)
    {
        _repo = repo;
    }

    public async Task<decimal> CalculateProratedAmountAsync(
        int subscriptionId,
        int oldPlanId,
        int newPlanId,
        DateTime effectiveDateUtc,
        CancellationToken ct = default)
    {
        var subscription = await _repo.FirstOrDefaultAsync<AccountSubscription>(x => x.Id == subscriptionId, ct)
            ?? throw new InvalidOperationException("Subscription not found.");
        var oldPlan = await _repo.FirstOrDefaultAsync<BillingPlan>(x => x.Id == oldPlanId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Old plan not found.");
        var newPlan = await _repo.FirstOrDefaultAsync<BillingPlan>(x => x.Id == newPlanId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("New plan not found.");

        var totalDays = Math.Max(1, (subscription.EndDate.Date - subscription.StartDate.Date).Days);
        var remainingDays = Math.Max(0, (subscription.EndDate.Date - effectiveDateUtc.Date).Days);

        var oldDaily = oldPlan.BaseRate / totalDays;
        var newDaily = newPlan.BaseRate / totalDays;

        return Math.Round((newDaily - oldDaily) * remainingDays, 2, MidpointRounding.AwayFromZero);
    }

    public async Task<decimal> CalculateSubscriptionAmountAsync(
        AccountSubscription subscription,
        DateTime billDateUtc,
        CancellationToken ct = default)
    {
        var plan = await _repo.FirstOrDefaultAsync<BillingPlan>(x => x.Id == subscription.PlanId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Plan not found.");

        var amount = plan.BaseRate + plan.RecurringPlatformFee + plan.RecurringAmcFee;

        if (string.Equals(plan.PricingModel, "LicenseBased", StringComparison.OrdinalIgnoreCase))
        {
            amount += subscription.Units * plan.LicensePricePerUnit;
        }

        var usageAmount = await _repo.Query<UsageRecord>()
            .Where(x => x.SubscriptionId == subscription.Id
                        && x.UsageDate >= billDateUtc.Date.AddDays(-30)
                        && x.UsageDate < billDateUtc.Date.AddDays(1))
            .SumAsync(x => x.UnitsConsumed, ct);

        amount += usageAmount;
        amount -= amount * (plan.DiscountPercentage / 100m);
        return Math.Round(Math.Max(0, amount), 2, MidpointRounding.AwayFromZero);
    }
}
