using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class RoleService : IRoleService
{
    private readonly IdentityDbContext _context;

    public RoleService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<mst_role> CreateAsync(mst_role role)
    {
        role.CreatedAt = DateTime.UtcNow;
        role.UpdatedAt = DateTime.UtcNow;

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return role;
    }

    public async Task<List<mst_role>> GetAllAsync()
    {
        return await _context.Roles.ToListAsync();
    }

    public async Task<mst_role?> GetByIdAsync(int id)
    {
        return await _context.Roles.FindAsync(id);
    }

    public async Task UpdateAsync(int id, mst_role role)
    {
        var existing = await _context.Roles.FindAsync(id);
        if (existing == null) return;

        existing.RoleCode = role.RoleCode;
        existing.RoleName = role.RoleName;
        existing.IsActive = role.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int id, bool isActive)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null) return;

        role.IsActive = isActive;
        role.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null) return;

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
    }
}