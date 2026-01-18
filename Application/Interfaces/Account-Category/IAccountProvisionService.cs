using System.Threading.Tasks;

public interface IAccountProvisionService
{
    Task<int> CreateAsync(CreateAccountRequest req);

    Task<PagedResultDto<AccountResponseDto>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        bool? status);

    Task<AccountResponseDto?> GetByIdAsync(int accountId);

    Task<bool> UpdateAsync(int accountId, UpdateAccountRequest req);

    Task<bool> UpdateStatusAsync(int accountId, bool status);

    Task<bool> DeleteAsync(int accountId);
}
