using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class FormService : IFormService
{
    private readonly IdentityDbContext _db;

    public FormService(IdentityDbContext db)
    {
        _db = db;
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
            ModuleName = req.ModuleName.Trim(),
            PageUrl = req.PageUrl.Trim(),
            IconName = req.IconName,
            SortOrder = req.SortOrder,
            IsMenu = req.IsMenu,
            IsVisible = req.IsVisible,
            IsActive = req.IsActive,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow
        };

        _db.Forms.Add(form);
        await _db.SaveChangesAsync();

        return form.FormId;
    }

    public async Task<PagedResultDto<FormResponseDto>> GetAllAsync(int page, int pageSize, string? search, bool? isActive)
    {
        var query = _db.Forms.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x => x.FormName.ToLower().Contains(s) || x.FormCode.ToLower().Contains(s));
        }

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var total = await query.CountAsync();

        // If pageSize <= 0 → return all data
        if (pageSize <= 0)
        {
            var allItems = await query
                .OrderBy(x => x.SortOrder)
                .Select(x => new FormResponseDto
                {
                    FormId = x.FormId,
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
    public async Task<FormResponseDto?> GetByIdAsync(int id)
    {
        return await _db.Forms
            .Where(x => x.FormId == id)
            .Select(x => new FormResponseDto
            {
                FormId = x.FormId,
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
        form.ModuleName = req.ModuleName.Trim();
        form.PageUrl = req.PageUrl.Trim();
        form.IconName = req.IconName;
        form.SortOrder = req.SortOrder;
        form.IsMenu = req.IsMenu;
        form.IsVisible = req.IsVisible;
        form.IsActive = req.IsActive;
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
