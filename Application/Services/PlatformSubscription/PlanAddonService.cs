using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class PlanAddonService : IPlanAddonService
{
    private readonly IdentityDbContext _db;

    public PlanAddonService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<bool> AssignAsync(Guid planId, List<Guid> addonIds)
    {
        var planExists = await _db.MarketPlans.AnyAsync(x => x.PlanId == planId);
        if (!planExists) return false;

        // Remove old addons
        var old = await _db.PlanAddons.Where(x => x.PlanId == planId).ToListAsync();
        _db.PlanAddons.RemoveRange(old);

        // Add new addons
        foreach (var aid in addonIds.Distinct())
        {
            _db.PlanAddons.Add(new PlanAddon
            {
                PlanId = planId,
                AddonId = aid
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Guid>> GetAddonIdsAsync(Guid planId)
    {
        return await _db.PlanAddons
            .Where(x => x.PlanId == planId)
            .Select(x => x.AddonId)
            .ToListAsync();
    }
}