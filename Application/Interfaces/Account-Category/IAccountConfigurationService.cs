using System.Threading.Tasks;

public interface IAccountConfigurationService
{
    Task<int> CreateAsync(CreateAccountConfigurationRequest req);
    Task<bool> UpdateAsync(int id, UpdateAccountConfigurationRequest req);
    Task<bool> DeleteAsync(int id);

    Task<AccountConfigurationResponseDto?> GetByIdAsync(int id);

    Task<PagedResultDto<AccountConfigurationResponseDto>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        int? accountId);
}
