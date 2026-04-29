using System.Threading;
using System.Threading.Tasks;

public interface IKafkaAlertPublisher
{
    Task PublishAsync(VehicleAlertMessage message, CancellationToken ct = default);
}
