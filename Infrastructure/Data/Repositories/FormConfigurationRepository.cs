using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class FormConfigurationRepository : IFormConfigurationRepository
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FormConfigurationRepository(
        IdentityDbContext db,
        ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<List<FormPage>> GetActiveFormPagesAsync()
    {
        return _db.FormPages
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.PageName)
            .ToListAsync();
    }

    public Task<FormPage?> GetActiveFormPageByKeyAsync(string pageKey)
    {
        return _db.FormPages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && x.PageKey == pageKey);
    }

    public Task<List<FormField>> GetActiveFormFieldsByPageKeyAsync(string pageKey)
    {
        return _db.FormFields
            .AsNoTracking()
            .Where(x => x.IsActive && x.PageKey == pageKey)
            .OrderBy(x => x.Id)
            .ToListAsync();
    }

    public Task<bool> FormFieldKeyExistsAsync(string pageKey, string fieldKey)
    {
        return _db.FormFields
            .AsNoTracking()
            .AnyAsync(x => x.PageKey == pageKey && x.FieldKey == fieldKey);
    }

    public Task AddFormFieldAsync(FormField entity)
    {
        return _db.FormFields.AddAsync(entity).AsTask();
    }

    public Task<List<FormField>> GetActiveFormFieldsByIdsAsync(string pageKey, IReadOnlyCollection<int> fieldIds)
    {
        return _db.FormFields
            .AsNoTracking()
            .Where(x => x.IsActive && x.PageKey == pageKey && fieldIds.Contains(x.Id))
            .ToListAsync();
    }

    public Task<List<AccountFormConfiguration>> GetConfigurationsByAccountAndPageAsync(int accountId, string pageKey)
    {
        return _db.AccountFormConfigurations
            .Where(x => x.AccountId == accountId && x.PageKey == pageKey)
            .ToListAsync();
    }

    public Task<mst_account?> GetAccessibleAccountByIdAsync(int accountId)
    {
        var query = _db.Accounts
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (_currentUser.IsAuthenticated && !_currentUser.IsSystem)
        {
            query = query.ApplyAccountHierarchyFilter(_currentUser);
        }

        return query.FirstOrDefaultAsync(x => x.AccountId == accountId);
    }

    public Task AddAccountFormConfigurationsAsync(IEnumerable<AccountFormConfiguration> entities)
    {
        return _db.AccountFormConfigurations.AddRangeAsync(entities);
    }

    public Task<int> SaveChangesAsync()
    {
        return _db.SaveChangesAsync();
    }
}
