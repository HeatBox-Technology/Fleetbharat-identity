using System.Threading.Channels;
using System.Threading.Tasks;

public class BulkQueue
{
    private readonly Channel<int> _queue = Channel.CreateUnbounded<int>();

    public async Task EnqueueAsync(int jobId)
    {
        await _queue.Writer.WriteAsync(jobId);
    }

    public ChannelReader<int> Reader => _queue.Reader;
}