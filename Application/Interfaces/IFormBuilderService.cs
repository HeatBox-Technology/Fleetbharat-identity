using System.Threading.Tasks;

public interface IFormBuilderService
{
    Task<int> CreateAsync(CreateFormBuilderRequest request);
    Task<bool> UpdateAsync(int id, UpdateFormBuilderRequest request);
    Task<FormBuilderPagedResponseDto> GetAllAsync(
        int? accountId,
        int? fkFormId,
        string? search,
        int pageNumber,
        int pageSize);
    Task<FormBuilderResponseDto?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id, DeleteFormBuilderRequest request);
    Task<FormBuilderResponseDto?> GetByAccountAndFormAsync(int accountId, int fkFormId);
}
