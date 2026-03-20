using System.Collections.Generic;
using System.Threading.Tasks;

public class HierarchyService : IHierarchyService
{
    private readonly IHierarchyRepository _repository;

    public HierarchyService(IHierarchyRepository repository)
    {
        _repository = repository;
    }

    public Task<List<SolutionListItemDto>> GetSolutionsAsync() =>
        _repository.GetSolutionsAsync();

    public Task<List<ModuleListItemDto>> GetModulesBySolutionAsync(int solutionId) =>
        _repository.GetModulesBySolutionAsync(solutionId);

    public Task<PagedResultDto<FormResponseDto>> GetFormsAsync(int page, int pageSize, string? search, bool? isActive, int? moduleId) =>
        _repository.GetFormsAsync(page, pageSize, search, isActive, moduleId);

    public Task<FormFilterConfigResponseDto?> GetFilterConfigByFormNameAsync(string formName) =>
        _repository.GetFilterConfigByFormNameAsync(formName);
}

