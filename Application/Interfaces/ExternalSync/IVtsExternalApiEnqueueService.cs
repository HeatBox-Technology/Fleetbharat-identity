using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

public interface IVtsExternalApiEnqueueService
{
    Task EnqueueVehicleDeviceMappingAsync(map_vehicle_device entity, CancellationToken ct = default);
    Task EnqueueVehicleDeviceMappingsAsync(IEnumerable<map_vehicle_device> entities, CancellationToken ct = default);
    Task EnqueueGeofenceAsync(mst_Geofence entity, List<CoordinateDto> coordinates, HttpMethod method, CancellationToken ct = default);
    Task EnqueueGeofencesAsync(IEnumerable<(mst_Geofence Entity, List<CoordinateDto> Coordinates)> items, CancellationToken ct = default);
    Task EnqueueVehicleGeofenceAsync(map_vehicle_geofence entity, HttpMethod method, CancellationToken ct = default);
}
