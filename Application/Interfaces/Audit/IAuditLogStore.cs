using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IAuditLogStore
{
    Task SaveBatchAsync(IReadOnlyCollection<AuditLog> logs, CancellationToken cancellationToken);
}
