using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


public class CountryService : ICountryService
{
    private readonly IdentityDbContext _context;

    public CountryService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<mst_country> CreateAsync(mst_country country)
    {
        country.CreatedAt = DateTime.UtcNow;
        country.UpdatedAt = DateTime.UtcNow;

        _context.Countries.Add(country);
        await _context.SaveChangesAsync();
        return country;
    }

    public async Task<List<mst_country>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _context.Countries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.CountryName ?? "").ToLower().Contains(s) ||
                (x.Iso2Code ?? "").ToLower().Contains(s) ||
                (x.Iso3Code ?? "").ToLower().Contains(s));
        }

        return await query
            .OrderByDescending(x => x.CountryId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new mst_country
            {
                CountryId = x.CountryId,
                Iso2Code = x.Iso2Code,
                Iso3Code = x.Iso3Code,
                CountryName = x.CountryName,
                MobileDialCode = x.MobileDialCode,
                CurrencyCode = x.CurrencyCode,
                CurrencySymbol = x.CurrencySymbol,
                TimezoneName = x.TimezoneName,
                UtcOffset = x.UtcOffset,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<mst_country?> GetByIdAsync(int id)
    {
        return await _context.Countries.FindAsync(id);
    }

    public async Task UpdateAsync(int id, mst_country country)
    {
        var existing = await _context.Countries.FindAsync(id);
        if (existing == null) return;

        existing.Iso2Code = country.Iso2Code;
        existing.Iso3Code = country.Iso3Code;
        existing.CountryName = country.CountryName;
        existing.MobileDialCode = country.MobileDialCode;
        existing.CurrencyCode = country.CurrencyCode;
        existing.CurrencySymbol = country.CurrencySymbol;
        existing.TimezoneName = country.TimezoneName;
        existing.UtcOffset = country.UtcOffset;
        existing.IsActive = country.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int id, bool isActive)
    {
        var country = await _context.Countries.FindAsync(id);
        if (country == null) return;

        country.IsActive = isActive;
        country.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var country = await _context.Countries.FindAsync(id);
        if (country == null) return;

        _context.Countries.Remove(country);
        await _context.SaveChangesAsync();
    }
}
