using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
public class PlanEntitlementService : IPlanEntitlementService
{
    private readonly IdentityDbContext _db;

    public PlanEntitlementService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<bool> AssignAsync(Guid planId, List<Guid> featureIds)
    {
        var planExists = await _db.MarketPlans.AnyAsync(x => x.PlanId == planId);
        if (!planExists) return false;

        // Remove old entitlements
        var old = await _db.PlanEntitlements.Where(x => x.PlanId == planId).ToListAsync();
        _db.PlanEntitlements.RemoveRange(old);

        // Add new entitlements
        foreach (var fid in featureIds.Distinct())
        {
            _db.PlanEntitlements.Add(new PlanEntitlement
            {
                PlanId = planId,
                FeatureId = fid
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Guid>> GetFeatureIdsAsync(Guid planId)
    {
        return await _db.PlanEntitlements
            .Where(x => x.PlanId == planId)
            .Select(x => x.FeatureId)
            .ToListAsync();
    }
}
