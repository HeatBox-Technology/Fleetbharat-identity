using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class DatabaseAuditLogStore : IAuditLogStore
{
    private readonly IdentityDbContext _db;

    public DatabaseAuditLogStore(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task SaveBatchAsync(IReadOnlyCollection<AuditLog> logs, CancellationToken cancellationToken)
    {
        if (logs.Count == 0)
            return;

        var previous = _db.ChangeTracker.AutoDetectChangesEnabled;
        _db.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            await _db.AuditLogs.AddRangeAsync(logs, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _db.ChangeTracker.AutoDetectChangesEnabled = previous;
        }
    }
}
