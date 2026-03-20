using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class BillingAnalyticsService : IBillingAnalyticsService
{
    private readonly IBillingRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public BillingAnalyticsService(IBillingRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<BillingRevenueDto>> GetRevenueProjectionAsync(CancellationToken ct = default)
    {
        return await _repo.Query<BillingInvoice>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.Status == "Paid" || x.Status == "Pending")
            .GroupBy(x => new { x.InvoiceDate.Year, x.InvoiceDate.Month })
            .OrderBy(x => x.Key.Year).ThenBy(x => x.Key.Month)
            .Select(g => new BillingRevenueDto
            {
                Month = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                RevenueAmount = g.Sum(x => x.Amount)
            })
            .ToListAsync(ct);
    }

    public async Task<List<BillingMarketPenetrationDto>> GetMarketPenetrationAsync(CancellationToken ct = default)
    {
        var accountCounts = await _repo.Query<AccountSubscription>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .GroupBy(x => x.AccountId)
            .Select(g => new { Region = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);

        var total = accountCounts.Sum(x => x.Count);
        if (total == 0)
        {
            return new List<BillingMarketPenetrationDto>();
        }

        return accountCounts
            .Select(x => new BillingMarketPenetrationDto
            {
                Region = x.Region,
                Percentage = Math.Round((decimal)x.Count * 100m / total, 2, MidpointRounding.AwayFromZero)
            })
            .OrderByDescending(x => x.Percentage)
            .ToList();
    }
}
