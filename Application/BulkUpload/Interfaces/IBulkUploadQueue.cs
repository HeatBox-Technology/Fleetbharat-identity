using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IBulkUploadQueue
{
    ValueTask EnqueueAsync(BulkUploadWorkItem item, CancellationToken ct = default);
    IAsyncEnumerable<BulkUploadWorkItem> DequeueAllAsync(CancellationToken ct = default);
}
