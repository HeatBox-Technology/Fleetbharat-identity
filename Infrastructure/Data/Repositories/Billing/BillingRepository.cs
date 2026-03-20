using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class BillingRepository : IBillingRepository
{
    private readonly IdentityDbContext _db;

    public BillingRepository(IdentityDbContext db)
    {
        _db = db;
    }

    public IQueryable<T> Query<T>() where T : class => _db.Set<T>();

    public Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : class
    {
        return _db.Set<T>().FirstOrDefaultAsync(predicate, ct);
    }

    public Task AddAsync<T>(T entity, CancellationToken ct = default) where T : class
    {
        return _db.Set<T>().AddAsync(entity, ct).AsTask();
    }

    public void Remove<T>(T entity) where T : class
    {
        _db.Set<T>().Remove(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }
}
