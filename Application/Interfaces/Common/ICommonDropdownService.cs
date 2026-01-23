using System.Collections.Generic;
using System.Threading.Tasks;

public interface ICommonDropdownService
{
    Task<List<DropdownDto>> GetAccountsAsync(string? search, int limit);
    Task<List<DropdownDto>> GetCategoriesAsync(string? search, int limit);
    Task<List<DropdownDto>> GetRolesAsync(int accountId, string? search, int limit);
    Task<List<DropdownGuidDto>> GetUsersAsync(int accountId, string? search, int limit);
}
