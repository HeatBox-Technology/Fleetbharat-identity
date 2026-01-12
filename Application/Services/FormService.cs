using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
public class FormService : IFormService
{
    private readonly IdentityDbContext _context;

    public FormService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<mst_form> CreateAsync(mst_form form)
    {
        form.CreatedAt = DateTime.UtcNow;
        form.UpdatedAt = DateTime.UtcNow;

        _context.Forms.Add(form);
        await _context.SaveChangesAsync();
        return form;
    }

    public async Task<List<mst_form>> GetAllAsync()
    {
        return await _context.Forms.ToListAsync();
    }

    public async Task<mst_form?> GetByIdAsync(int id)
    {
        return await _context.Forms.FindAsync(id);
    }

    public async Task UpdateAsync(int id, mst_form form)
    {
        var existing = await _context.Forms.FindAsync(id);
        if (existing == null) return;

        existing.FormCode = form.FormCode;
        existing.FormName = form.FormName;
        existing.ModuleName = form.ModuleName;
        existing.PageUrl = form.PageUrl;
        existing.PageComponent = form.PageComponent;
        existing.IconName = form.IconName;
        existing.SortOrder = form.SortOrder;
        existing.IsMenu = form.IsMenu;
        existing.IsVisible = form.IsVisible;
        existing.ParentFormId = form.ParentFormId;
        existing.IsActive = form.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int id, bool isActive)
    {
        var form = await _context.Forms.FindAsync(id);
        if (form == null) return;

        form.IsActive = isActive;
        form.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var form = await _context.Forms.FindAsync(id);
        if (form == null) return;

        _context.Forms.Remove(form);
        await _context.SaveChangesAsync();
    }
}
