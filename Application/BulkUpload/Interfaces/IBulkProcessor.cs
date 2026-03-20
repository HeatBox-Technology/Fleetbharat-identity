using System.Threading;
using System.Threading.Tasks;

public interface IBulkProcessor
{
    Task ProcessAsync(BulkUploadWorkItem workItem, CancellationToken ct = default);
}
