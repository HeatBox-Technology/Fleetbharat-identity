using System.Threading;
using System.Threading.Tasks;

public class AuditLogger : IAuditLogger
{
    private readonly AuditQueue _queue;

    public AuditLogger(AuditQueue queue)
    {
        _queue = queue;
    }

    public ValueTask LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        _queue.TryEnqueue(auditLog);
        return ValueTask.CompletedTask;
    }
}
