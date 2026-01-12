using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class CityService : ICityService
{
    private readonly IdentityDbContext _context;

    public CityService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<mst_city> CreateAsync(mst_city city)
    {
        city.CreatedAt = DateTime.UtcNow;
        city.UpdatedAt = DateTime.UtcNow;

        _context.Cities.Add(city);
        await _context.SaveChangesAsync();
        return city;
    }

    public async Task<List<mst_city>> GetAllAsync()
    {
        return await _context.Cities.ToListAsync();
    }

    public async Task<List<mst_city>> GetByStateAsync(int stateId)
    {
        return await _context.Cities
            .Where(c => c.StateId == stateId)
            .ToListAsync();
    }

    public async Task<mst_city?> GetByIdAsync(int id)
    {
        return await _context.Cities.FindAsync(id);
    }

    public async Task UpdateAsync(int id, mst_city city)
    {
        var existing = await _context.Cities.FindAsync(id);
        if (existing == null) return;

        existing.StateId = city.StateId;
        existing.CityName = city.CityName;
        existing.IsActive = city.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int id, bool isActive)
    {
        var city = await _context.Cities.FindAsync(id);
        if (city == null) return;

        city.IsActive = isActive;
        city.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var city = await _context.Cities.FindAsync(id);
        if (city == null) return;

        _context.Cities.Remove(city);
        await _context.SaveChangesAsync();
    }
}
