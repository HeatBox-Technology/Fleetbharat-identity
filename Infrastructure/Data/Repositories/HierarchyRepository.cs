using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class HierarchyRepository : IHierarchyRepository
{
    private readonly IdentityDbContext _db;

    public HierarchyRepository(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<List<SolutionListItemDto>> GetSolutionsAsync()
    {
        return await _db.Solutions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SolutionListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description
            })
            .ToListAsync();
    }

    public async Task<List<ModuleListItemDto>> GetModulesBySolutionAsync(int solutionId)
    {
        return await _db.FormModules
            .AsNoTracking()
            .Where(x => x.IsActive && x.SolutionId == solutionId)
            .OrderBy(x => x.ModuleName)
            .Select(x => new ModuleListItemDto
            {
                FormModuleId = x.FormModuleId,
                SolutionId = x.SolutionId,
                ModuleCode = x.ModuleCode,
                ModuleName = x.ModuleName,
                Description = x.Description
            })
            .ToListAsync();
    }

    public async Task<PagedResultDto<FormResponseDto>> GetFormsAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        int? moduleId)
    {
        var query = _db.Forms.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x => x.FormName.ToLower().Contains(s) || x.FormCode.ToLower().Contains(s));
        }

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (moduleId.HasValue)
            query = query.Where(x => x.FormModuleId == moduleId.Value);

        var total = await query.CountAsync();

        if (pageSize <= 0)
        {
            var allItems = await query
                .OrderBy(x => x.SortOrder)
                .Select(x => new FormResponseDto
                {
                    FormId = x.FormId,
                    FormModuleId = x.FormModuleId,
                    FormCode = x.FormCode,
                    FormName = x.FormName,
                    ModuleName = x.ModuleName,
                    PageUrl = x.PageUrl,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            return new PagedResultDto<FormResponseDto>
            {
                Page = 1,
                PageSize = total,
                TotalRecords = total,
                Items = allItems
            };
        }

        if (page <= 0) page = 1;

        var items = await query
            .OrderBy(x => x.SortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FormResponseDto
            {
                FormId = x.FormId,
                FormModuleId = x.FormModuleId,
                FormCode = x.FormCode,
                FormName = x.FormName,
                ModuleName = x.ModuleName,
                PageUrl = x.PageUrl,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return new PagedResultDto<FormResponseDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = total,
            Items = items
        };
    }

    public async Task<FormFilterConfigResponseDto?> GetFilterConfigByFormNameAsync(string formName)
    {
        var form = await _db.Forms
            .AsNoTracking()
            .Where(x => x.FormName.ToLower() == formName.ToLower() || x.FormCode.ToLower() == formName.ToLower())
            .Select(x => new { x.FormName, x.FilterConfigJson })
            .FirstOrDefaultAsync();

        if (form == null || string.IsNullOrWhiteSpace(form.FilterConfigJson))
            return null;

        try
        {
            using var jsonDoc = JsonDocument.Parse(form.FilterConfigJson);
            var filters = jsonDoc.RootElement.TryGetProperty("filters", out var filtersNode)
                ? filtersNode.Clone()
                : JsonDocument.Parse("[]").RootElement.Clone();

            return new FormFilterConfigResponseDto
            {
                FormName = form.FormName,
                Filters = filters
            };
        }
        catch (JsonException)
        {
            return new FormFilterConfigResponseDto
            {
                FormName = form.FormName,
                Filters = JsonDocument.Parse("[]").RootElement.Clone()
            };
        }
    }
}

