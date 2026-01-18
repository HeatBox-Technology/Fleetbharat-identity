using System.Collections.Generic;
using System.Threading.Tasks;

public interface ICategoryService
{
    Task<int> CreateAsync(CreateCategoryRequest req);
    Task<List<CategoryResponseDto>> GetAllAsync(string? search, bool? isActive);
    Task<CategoryResponseDto?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(int id, UpdateCategoryRequest req);
    Task<bool> UpdateStatusAsync(int id, bool isActive);
    Task<bool> DeleteAsync(int id);
}
