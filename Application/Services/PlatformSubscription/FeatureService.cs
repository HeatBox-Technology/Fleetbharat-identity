
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class FeatureService : IFeatureService
{
    private readonly IdentityDbContext _db;

    public FeatureService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> CreateAsync(FeatureMaster feature)
    {
        _db.Features.Add(feature);
        await _db.SaveChangesAsync();
        return feature.FeatureId;
    }

    public async Task<List<FeatureMaster>> GetAllAsync()
    {
        return await _db.Features.OrderBy(x => x.FeatureName).ToListAsync();
    }
}
