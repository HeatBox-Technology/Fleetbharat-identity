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

    public async Task<List<mst_city>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _context.Cities.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.CityName ?? "").ToLower().Contains(s) ||
                (x.StateId.ToString() ?? "").ToLower().Contains(s));
        }

        return await query
            .OrderByDescending(x => x.CityId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new mst_city
            {
                CityId = x.CityId,
                StateId = x.StateId,
                CityName = x.CityName,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
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
