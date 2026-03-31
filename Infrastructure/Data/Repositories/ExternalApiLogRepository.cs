using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class ExternalApiLogRepository : IExternalApiLogRepository
{
    private readonly IdentityDbContext _db;

    public ExternalApiLogRepository(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<ExternalApiLog> AddAsync(ExternalApiLog log, CancellationToken ct = default)
    {
        await _db.Set<ExternalApiLog>().AddAsync(log, ct);
        return log;
    }

    public Task<ExternalApiLog?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return _db.Set<ExternalApiLog>().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }
}
