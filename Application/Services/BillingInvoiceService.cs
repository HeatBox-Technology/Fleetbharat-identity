using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class BillingInvoiceService : IBillingInvoiceService
{
    private readonly IBillingRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IBillingCalculationService _calculationService;

    public BillingInvoiceService(
        IBillingRepository repo,
        ICurrentUserService currentUser,
        IBillingCalculationService calculationService)
    {
        _repo = repo;
        _currentUser = currentUser;
        _calculationService = calculationService;
    }

    public async Task<List<InvoiceResponseDto>> GetInvoicesAsync(int skip, int take, CancellationToken ct = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 200);

        return await _repo.Query<BillingInvoice>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .OrderByDescending(x => x.InvoiceDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new InvoiceResponseDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                InvoiceNumber = x.InvoiceNumber,
                SubscriptionId = x.SubscriptionId,
                Amount = x.Amount,
                Currency = x.Currency,
                Status = x.Status,
                InvoiceDate = x.InvoiceDate,
                DueDate = x.DueDate,
                CreatedBy = x.CreatedBy,
                UpdatedBy = x.UpdatedBy,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate
            })
            .ToListAsync(ct);
    }

    public async Task<List<InvoiceResponseDto>> GetInvoicesByAccountAsync(int accountId, int skip, int take, CancellationToken ct = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 200);

        return await _repo.Query<BillingInvoice>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.InvoiceDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new InvoiceResponseDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                InvoiceNumber = x.InvoiceNumber,
                SubscriptionId = x.SubscriptionId,
                Amount = x.Amount,
                Currency = x.Currency,
                Status = x.Status,
                InvoiceDate = x.InvoiceDate,
                DueDate = x.DueDate,
                CreatedBy = x.CreatedBy,
                UpdatedBy = x.UpdatedBy,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate
            })
            .ToListAsync(ct);
    }

    public async Task<InvoiceResponseDto> CreateManualInvoiceAsync(InvoiceManualCreateDto dto, CancellationToken ct = default)
    {
        var accountId = ResolveAccount(dto.AccountId);
        var actor = _currentUser.AccountId > 0 ? _currentUser.AccountId : (int?)null;
        var invoiceDate = dto.InvoiceDate?.Date ?? DateTime.UtcNow.Date;
        var dueDate = dto.DueDate?.Date ?? invoiceDate.AddDays(15);

        var invoice = new BillingInvoice
        {
            AccountId = accountId,
            InvoiceNumber = GenerateInvoiceNumber(accountId, invoiceDate),
            SubscriptionId = dto.SubscriptionId,
            Amount = dto.Amount,
            Currency = dto.Currency,
            Status = "Pending",
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            CreatedBy = actor,
            UpdatedBy = actor,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        await _repo.AddAsync(invoice, ct);
        await _repo.SaveChangesAsync(ct);
        return new InvoiceResponseDto
        {
            Id = invoice.Id,
            AccountId = invoice.AccountId,
            InvoiceNumber = invoice.InvoiceNumber,
            SubscriptionId = invoice.SubscriptionId,
            Amount = invoice.Amount,
            Currency = invoice.Currency,
            Status = invoice.Status,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            CreatedBy = invoice.CreatedBy,
            UpdatedBy = invoice.UpdatedBy,
            CreatedDate = invoice.CreatedDate,
            UpdatedDate = invoice.UpdatedDate
        };
    }

    public async Task<string> ExportInvoicesCsvAsync(int skip, int take, CancellationToken ct = default)
    {
        var rows = await GetInvoicesAsync(skip, take, ct);
        var sb = new StringBuilder();
        sb.AppendLine("InvoiceNumber,AccountId,SubscriptionId,Amount,Currency,Status,InvoiceDate,DueDate");
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                row.InvoiceNumber,
                row.AccountId.ToString(CultureInfo.InvariantCulture),
                row.SubscriptionId.ToString(CultureInfo.InvariantCulture),
                row.Amount.ToString(CultureInfo.InvariantCulture),
                row.Currency,
                row.Status,
                row.InvoiceDate.ToString("yyyy-MM-dd"),
                row.DueDate.ToString("yyyy-MM-dd")));
        }

        return sb.ToString();
    }

    public async Task<int> GenerateDueInvoicesBatchAsync(int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        var now = DateTime.UtcNow.Date;
        var actor = _currentUser.AccountId > 0 ? _currentUser.AccountId : (int?)null;

        var dueSubscriptions = await _repo.Query<AccountSubscription>()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.Status == "Active" && x.NextBillingDate.Date <= now)
            .OrderBy(x => x.NextBillingDate)
            .Take(take)
            .ToListAsync(ct);

        var generated = 0;
        foreach (var subscription in dueSubscriptions)
        {
            var duplicateExists = await _repo.Query<BillingInvoice>()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.InvoiceDate.Date == now, ct);

            if (duplicateExists)
            {
                continue;
            }

            var amount = await _calculationService.CalculateSubscriptionAmountAsync(subscription, now, ct);
            var currencyId = await _repo.Query<BillingPlan>()
                .Where(x => x.Id == subscription.PlanId)
                .Select(x => x.CurrencyId)
                .FirstOrDefaultAsync(ct);

            var invoice = new BillingInvoice
            {
                AccountId = subscription.AccountId,
                InvoiceNumber = GenerateInvoiceNumber(subscription.AccountId, now),
                SubscriptionId = subscription.Id,
                Amount = amount,
                Currency = currencyId == 0 ? "INR" : currencyId.ToString(CultureInfo.InvariantCulture),
                Status = "Pending",
                InvoiceDate = now,
                DueDate = now.AddDays(15),
                RetryCount = 0,
                NextRetryDate = now.AddDays(1),
                CreatedBy = actor,
                UpdatedBy = actor,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            await _repo.AddAsync(invoice, ct);
            subscription.NextBillingDate = CalculateNextBillingDate(subscription.NextBillingDate);
            subscription.UpdatedBy = actor;
            subscription.UpdatedDate = DateTime.UtcNow;
            generated++;
        }

        if (generated > 0)
        {
            await _repo.SaveChangesAsync(ct);
        }

        return generated;
    }

    private int ResolveAccount(int requestAccountId)
    {
        if (_currentUser.IsSystemRole && requestAccountId > 0)
        {
            return requestAccountId;
        }

        return _currentUser.AccountId;
    }

    private static DateTime CalculateNextBillingDate(DateTime current) => current.Date.AddMonths(1);

    private static string GenerateInvoiceNumber(int accountId, DateTime date) =>
        $"INV-{accountId}-{date:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8]}";
}
