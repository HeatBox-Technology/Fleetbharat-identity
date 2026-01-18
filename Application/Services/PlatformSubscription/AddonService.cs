using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class AddonService : IAddonService
{
    private readonly IdentityDbContext _db;

    public AddonService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> CreateAsync(AddonMaster addon)
    {
        _db.Addons.Add(addon);
        await _db.SaveChangesAsync();
        return addon.AddonId;
    }

    public async Task<List<AddonMaster>> GetAllAsync()
    {
        return await _db.Addons.OrderBy(x => x.AddonName).ToListAsync();
    }
}
