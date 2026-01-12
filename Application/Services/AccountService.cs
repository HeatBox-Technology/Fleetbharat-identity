
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class AccountService : IAccountService
{
    private readonly IdentityDbContext _context;

    public AccountService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<mst_account> CreateAsync(mst_account account)
    {
        account.CreatedOn = DateTime.UtcNow;
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<List<mst_account>> GetAllAsync()
    {
        return await _context.Accounts.ToListAsync();
    }

    public async Task<mst_account> GetByIdAsync(int id)
    {
        return await _context.Accounts.FindAsync(id);
    }

    public async Task UpdateAsync(int id, mst_account account)
    {
        var existing = await GetByIdAsync(id);
        if (existing == null) return;

        existing.AccountName = account.AccountName;
        existing.AccountType = account.AccountType;
        existing.PrimaryDomain = account.PrimaryDomain;
        existing.Status = account.Status;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int id, string status)
    {
        var account = await GetByIdAsync(id);
        if (account == null) return;

        account.Status = status;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var account = await GetByIdAsync(id);
        if (account == null) return;

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
    }
}
