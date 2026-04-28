using System.Collections.Generic;
using System.Threading.Tasks;

public interface IFormConfigurationRepository
{
    Task<List<FormPage>> GetActiveFormPagesAsync();
    Task<FormPage?> GetActiveFormPageByKeyAsync(string pageKey);
    Task<List<FormField>> GetActiveFormFieldsByPageKeyAsync(string pageKey);
    Task<bool> FormFieldKeyExistsAsync(string pageKey, string fieldKey);
    Task AddFormFieldAsync(FormField entity);
    Task<List<FormField>> GetActiveFormFieldsByIdsAsync(string pageKey, IReadOnlyCollection<int> fieldIds);
    Task<List<AccountFormConfiguration>> GetConfigurationsByAccountAndPageAsync(int accountId, string pageKey);
    Task<mst_account?> GetAccessibleAccountByIdAsync(int accountId);
    Task AddAccountFormConfigurationsAsync(IEnumerable<AccountFormConfiguration> entities);
    Task<int> SaveChangesAsync();
}
