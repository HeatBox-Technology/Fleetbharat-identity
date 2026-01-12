using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


public class StateService : IStateService
{
    private readonly IdentityDbContext _context;

    public StateService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<mst_state> CreateAsync(mst_state state)
    {
        state.CreatedAt = DateTime.UtcNow;
        state.UpdatedAt = DateTime.UtcNow;

        _context.States.Add(state);
        await _context.SaveChangesAsync();
        return state;
    }

    public async Task<List<mst_state>> GetAllAsync()
    {
        return await _context.States.ToListAsync();
    }

    public async Task<List<mst_state>> GetByCountryAsync(int countryId)
    {
        return await _context.States
            .Where(s => s.CountryId == countryId)
            .ToListAsync();
    }

    public async Task<mst_state?> GetByIdAsync(int id)
    {
        return await _context.States.FindAsync(id);
    }

    public async Task UpdateAsync(int id, mst_state state)
    {
        var existing = await _context.States.FindAsync(id);
        if (existing == null) return;

        existing.CountryId = state.CountryId;
        existing.StateCode = state.StateCode;
        existing.StateName = state.StateName;
        existing.IsActive = state.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int id, bool isActive)
    {
        var state = await _context.States.FindAsync(id);
        if (state == null) return;

        state.IsActive = isActive;
        state.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var state = await _context.States.FindAsync(id);
        if (state == null) return;

        _context.States.Remove(state);
        await _context.SaveChangesAsync();
    }
}
