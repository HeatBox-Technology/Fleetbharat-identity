using System.Threading;
using System.Threading.Tasks;

public interface IRealtimeNotificationBroadcaster
{
    Task BroadcastAsync(RealtimeEventMessage message, CancellationToken ct = default);
}
