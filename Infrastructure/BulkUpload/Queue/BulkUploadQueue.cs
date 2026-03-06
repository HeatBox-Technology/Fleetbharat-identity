using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public class BulkUploadQueue : IBulkUploadQueue
{
    private readonly Channel<BulkUploadWorkItem> _channel;

    public BulkUploadQueue()
    {
        _channel = Channel.CreateBounded<BulkUploadWorkItem>(new BoundedChannelOptions(200)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask EnqueueAsync(BulkUploadWorkItem item, CancellationToken ct = default)
    {
        return _channel.Writer.WriteAsync(item, ct);
    }

    public IAsyncEnumerable<BulkUploadWorkItem> DequeueAllAsync(CancellationToken ct = default)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}
