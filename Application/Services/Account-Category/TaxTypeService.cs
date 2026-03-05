using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class TaxTypeService : ITaxTypeService
{
    private readonly IdentityDbContext _db;

    public TaxTypeService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateTaxTypeRequest req)
    {
        var code = req.TaxTypeCode.Trim().ToUpper();
        var name = req.TaxTypeName.Trim();

        var exists = await _db.TaxTypes.AnyAsync(x =>
            x.CountryId == req.CountryId &&
            x.TaxTypeCode.ToUpper() == code);

        if (exists)
            throw new InvalidOperationException("TaxType already exists for this country");

        var entity = new mst_tax_type
        {
            CountryId = req.CountryId,
            TaxTypeCode = code,
            TaxTypeName = name,
            IsActive = req.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.TaxTypes.Add(entity);
        await _db.SaveChangesAsync();

        return entity.TaxTypeId;
    }

    public async Task<bool> UpdateAsync(int taxTypeId, UpdateTaxTypeRequest req)
    {
        var entity = await _db.TaxTypes.FirstOrDefaultAsync(x => x.TaxTypeId == taxTypeId);
        if (entity == null) return false;

        entity.CountryId = req.CountryId;
        entity.TaxTypeCode = req.TaxTypeCode.Trim().ToUpper();
        entity.TaxTypeName = req.TaxTypeName.Trim();
        entity.IsActive = req.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int taxTypeId)
    {
        var entity = await _db.TaxTypes.FirstOrDefaultAsync(x => x.TaxTypeId == taxTypeId);
        if (entity == null) return false;

        _db.TaxTypes.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<TaxTypeResponseDto>> GetAllAsync(string? search, int? countryId, bool? isActive, int page = 1, int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.TaxTypes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.TaxTypeCode.ToLower().Contains(s) ||
                x.TaxTypeName.ToLower().Contains(s));
        }

        if (countryId.HasValue)
            query = query.Where(x => x.CountryId == countryId.Value);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        return await query
            .OrderBy(x => x.TaxTypeName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TaxTypeResponseDto
            {
                TaxTypeId = x.TaxTypeId,
                CountryId = x.CountryId,
                TaxTypeCode = x.TaxTypeCode,
                TaxTypeName = x.TaxTypeName,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<List<TaxTypeResponseDto>> GetByCountryAsync(int countryId)
    {
        return await _db.TaxTypes
            .Where(x => x.CountryId == countryId && x.IsActive)
            .OrderBy(x => x.TaxTypeName)
            .Select(x => new TaxTypeResponseDto
            {
                TaxTypeId = x.TaxTypeId,
                CountryId = x.CountryId,
                TaxTypeCode = x.TaxTypeCode,
                TaxTypeName = x.TaxTypeName,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<TaxTypeResponseDto?> GetByIdAsync(int taxTypeId)
    {
        return await _db.TaxTypes
            .Where(x => x.TaxTypeId == taxTypeId)
            .Select(x => new TaxTypeResponseDto
            {
                TaxTypeId = x.TaxTypeId,
                CountryId = x.CountryId,
                TaxTypeCode = x.TaxTypeCode,
                TaxTypeName = x.TaxTypeName,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateStatusAsync(int taxTypeId, bool isActive)
    {
        var entity = await _db.TaxTypes.FirstOrDefaultAsync(x => x.TaxTypeId == taxTypeId);
        if (entity == null) return false;

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
}
