using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

public interface IBillingRepository
{
    IQueryable<T> Query<T>() where T : class;
    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default) where T : class;
    Task AddAsync<T>(T entity, CancellationToken ct = default) where T : class;
    void Remove<T>(T entity) where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
