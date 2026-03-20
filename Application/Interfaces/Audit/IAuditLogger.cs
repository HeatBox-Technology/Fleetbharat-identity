using System.Threading;
using System.Threading.Tasks;

public interface IAuditLogger
{
    ValueTask LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}
