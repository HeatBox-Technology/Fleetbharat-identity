using System.Threading.Channels;

public class AuditQueue
{
    private readonly Channel<AuditLog> _channel = Channel.CreateUnbounded<AuditLog>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    public ChannelReader<AuditLog> Reader => _channel.Reader;

    public bool TryEnqueue(AuditLog log) => _channel.Writer.TryWrite(log);
}
