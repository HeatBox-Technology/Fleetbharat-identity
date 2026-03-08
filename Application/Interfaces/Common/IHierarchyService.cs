using System.Collections.Generic;
using System.Threading.Tasks;

public interface IHierarchyService
{
    Task<List<SolutionListItemDto>> GetSolutionsAsync();
    Task<List<ModuleListItemDto>> GetModulesBySolutionAsync(int solutionId);
    Task<PagedResultDto<FormResponseDto>> GetFormsAsync(int page, int pageSize, string? search, bool? isActive, int? moduleId);
    Task<FormFilterConfigResponseDto?> GetFilterConfigByFormNameAsync(string formName);
}

