using System.Threading;
using System.Threading.Tasks;

public interface IExternalApiLogRepository
{
    Task<ExternalApiLog> AddAsync(ExternalApiLog log, CancellationToken ct = default);
    Task<ExternalApiLog?> GetByIdAsync(long id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
