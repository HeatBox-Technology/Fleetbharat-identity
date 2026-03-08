using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class FormService : IFormService
{
    private readonly IdentityDbContext _db;
    private readonly IHierarchyRepository _hierarchyRepository;

    public FormService(IdentityDbContext db, IHierarchyRepository hierarchyRepository)
    {
        _db = db;
        _hierarchyRepository = hierarchyRepository;
    }

    public async Task<int> CreateAsync(CreateFormRequest req)
    {
        var code = req.FormCode.Trim().ToUpper();

        var exists = await _db.Forms.AnyAsync(x => x.FormCode == code);
        if (exists) throw new InvalidOperationException("FormCode already exists");

        var form = new mst_form
        {
            FormCode = code,
            FormName = req.FormName.Trim(),
            FormModuleId = req.FormModuleId,
            ModuleName = req.ModuleName.Trim(),
            PageUrl = req.PageUrl.Trim(),
            IconName = req.IconName,
            SortOrder = req.SortOrder,
            IsMenu = req.IsMenu,
            IsVisible = req.IsVisible,
            IsActive = req.IsActive,
            FilterConfigJson = req.FilterConfigJson,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow
        };

        _db.Forms.Add(form);
        await _db.SaveChangesAsync();

        return form.FormId;
    }

    public async Task<PagedResultDto<FormResponseDto>> GetAllAsync(int page, int pageSize, string? search, bool? isActive, int? moduleId)
    {
        return await _hierarchyRepository.GetFormsAsync(page, pageSize, search, isActive, moduleId);
    }

    public async Task<FormResponseDto?> GetByIdAsync(int id)
    {
        return await _db.Forms
            .Where(x => x.FormId == id)
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
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(int id, UpdateFormRequest req)
    {
        var form = await _db.Forms.FirstOrDefaultAsync(x => x.FormId == id);
        if (form == null) return false;

        var code = req.FormCode.Trim().ToUpper();

        var duplicate = await _db.Forms.AnyAsync(x => x.FormCode == code && x.FormId != id);
        if (duplicate) throw new InvalidOperationException("FormCode already exists");

        form.FormCode = code;
        form.FormName = req.FormName.Trim();
        form.FormModuleId = req.FormModuleId;
        form.ModuleName = req.ModuleName.Trim();
        form.PageUrl = req.PageUrl.Trim();
        form.IconName = req.IconName;
        form.SortOrder = req.SortOrder;
        form.IsMenu = req.IsMenu;
        form.IsVisible = req.IsVisible;
        form.IsActive = req.IsActive;
        form.FilterConfigJson = req.FilterConfigJson;
        form.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var form = await _db.Forms.FirstOrDefaultAsync(x => x.FormId == id);
        if (form == null) return false;

        _db.Forms.Remove(form);
        await _db.SaveChangesAsync();
        return true;
    }
}
