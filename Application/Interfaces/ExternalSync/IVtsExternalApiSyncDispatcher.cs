using System.Threading;
using System.Threading.Tasks;

public interface IVtsExternalApiSyncDispatcher
{
    Task SyncGeofenceAsync(string payloadJson, CancellationToken ct = default);
    Task SyncVehicleDeviceMappingAsync(string payloadJson, CancellationToken ct = default);
    Task SyncVehicleGeofenceMappingAsync(string payloadJson, CancellationToken ct = default);
}
