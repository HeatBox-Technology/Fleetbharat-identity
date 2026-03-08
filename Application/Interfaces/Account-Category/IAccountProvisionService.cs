using System.Collections.Generic;
using System.Threading.Tasks;

public interface IAccountProvisionService
{
    Task<int> CreateAsync(CreateAccountRequest req);

    Task<AccountListWithCardDto> GetAllAsync(
      int page,
      int pageSize,
      string? search,
      bool? status);

    Task<AccountResponseDto?> GetByIdAsync(int accountId);
    Task<List<AccountHierarchyDto>> GetHierarchyAsync();

    Task<bool> UpdateAsync(int accountId, UpdateAccountRequest req);

    Task<bool> UpdateStatusAsync(int accountId, bool status);

    Task<bool> DeleteAsync(int accountId);
}
