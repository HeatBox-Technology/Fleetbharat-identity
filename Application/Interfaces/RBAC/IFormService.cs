using System.Threading.Tasks;

public interface IFormService
{
    Task<int> CreateAsync(CreateFormRequest req);
    Task<bool> UpdateAsync(int id, UpdateFormRequest req);
    Task<bool> DeleteAsync(int id);

    Task<PagedResultDto<FormResponseDto>> GetAllAsync(int page, int pageSize, string? search, bool? isActive);
    Task<FormResponseDto?> GetByIdAsync(int id);
}
