using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class CategoryService : ICategoryService
{
    private readonly IdentityDbContext _db;

    public CategoryService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateCategoryRequest req)
    {
        var name = req.LabelName.Trim();

        var exists = await _db.Categories
            .AnyAsync(x => x.LabelName == name);

        if (exists)
            throw new InvalidOperationException("Category already exists");

        var category = new mst_category
        {
            LabelName = name,
            Description = req.Description?.Trim(),
            IsActive = req.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return category.CategoryId;
    }

    public async Task<List<CategoryResponseDto>> GetAllAsync(string? search, bool? isActive, int page = 1, int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.LabelName ?? "").ToLower().Contains(s) ||
                (x.Description ?? "").ToLower().Contains(s));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CategoryResponseDto
            {
                CategoryId = x.CategoryId,
                LabelName = x.LabelName,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(int id)
    {
        return await _db.Categories
            .Where(x => x.CategoryId == id)
            .Select(x => new CategoryResponseDto
            {
                CategoryId = x.CategoryId,
                LabelName = x.LabelName,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(int id, UpdateCategoryRequest req)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(x => x.CategoryId == id);
        if (entity == null) return false;

        entity.LabelName = req.LabelName.Trim();
        entity.Description = req.Description?.Trim();
        entity.IsActive = req.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, bool isActive)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(x => x.CategoryId == id);
        if (entity == null) return false;

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(x => x.CategoryId == id);
        if (entity == null) return false;

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
    private static string GenerateRoleCode(string categoryName)
    {
        var baseCode = categoryName
            .Trim()
            .ToUpper()
            .Replace(" ", "_");

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpper();

        return $"{baseCode}_{suffix}";
    }

}
