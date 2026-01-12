using System;
using System.Collections.Generic;
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

    public async Task<List<mst_country>> GetAllAsync()
    {
        return await _context.Countries.ToListAsync();
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
