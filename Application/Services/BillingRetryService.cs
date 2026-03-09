using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class BillingRetryService : IBillingRetryService
{
    private readonly IBillingRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public BillingRetryService(IBillingRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<int> ProcessPendingRetriesAsync(int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        var now = DateTime.UtcNow.Date;
        var actor = _currentUser.AccountId > 0 ? _currentUser.AccountId : (int?)null;

        var pending = await _repo.Query<BillingInvoice>()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.Status == "Pending"
                        && x.NextRetryDate.HasValue
                        && x.NextRetryDate.Value.Date <= now)
            .OrderBy(x => x.NextRetryDate)
            .Take(take)
            .ToListAsync(ct);

        foreach (var invoice in pending)
        {
            invoice.RetryCount++;
            if (invoice.RetryCount >= 3)
            {
                invoice.Status = "Overdue";
                invoice.NextRetryDate = null;
            }
            else
            {
                invoice.NextRetryDate = invoice.RetryCount switch
                {
                    1 => now.AddDays(1),
                    2 => now.AddDays(3),
                    _ => now.AddDays(7)
                };
            }

            invoice.UpdatedBy = actor;
            invoice.UpdatedDate = DateTime.UtcNow;
        }

        if (pending.Count > 0)
        {
            await _repo.SaveChangesAsync(ct);
        }

        return pending.Count;
    }
}
