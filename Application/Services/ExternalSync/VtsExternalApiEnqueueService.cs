using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class VtsExternalApiEnqueueService : IVtsExternalApiEnqueueService
{
    private readonly IdentityDbContext _db;
    private readonly IExternalApiLogRepository _logRepository;
    private readonly IExternalSyncQueueService _queueService;

    public VtsExternalApiEnqueueService(
        IdentityDbContext db,
        IExternalApiLogRepository logRepository,
        IExternalSyncQueueService queueService)
    {
        _db = db;
        _logRepository = logRepository;
        _queueService = queueService;
    }

    public async Task EnqueueVehicleDeviceMappingAsync(map_vehicle_device entity, CancellationToken ct = default)
    {
        var payload = await BuildVehicleDevicePayloadAsync(entity, ct);
        if (payload == null)
            return;

        await EnqueueAsync(VtsExternalSyncModules.VehicleDeviceMap, entity.Id.ToString(), HttpMethod.Post, payload, ct);
    }

    public async Task EnqueueVehicleDeviceMappingsAsync(IEnumerable<map_vehicle_device> entities, CancellationToken ct = default)
    {
        foreach (var entity in entities)
            await EnqueueVehicleDeviceMappingAsync(entity, ct);
    }

    public Task EnqueueGeofenceAsync(mst_Geofence entity, List<CoordinateDto> coordinates, HttpMethod method, CancellationToken ct = default)
    {
        var payload = new List<ExternalGeofenceRequest>
        {
            BuildGeofencePayload(entity, coordinates)
        };

        return EnqueueAsync(VtsExternalSyncModules.Geofence, entity.Id.ToString(), method, payload, ct);
    }

    public async Task EnqueueGeofencesAsync(IEnumerable<(mst_Geofence Entity, List<CoordinateDto> Coordinates)> items, CancellationToken ct = default)
    {
        foreach (var item in items)
            await EnqueueGeofenceAsync(item.Entity, item.Coordinates, HttpMethod.Post, ct);
    }

    public async Task EnqueueVehicleGeofenceAsync(map_vehicle_geofence entity, HttpMethod method, CancellationToken ct = default)
    {
        var payload = await BuildVehicleGeofencePayloadAsync(entity, ct);
        if (payload == null)
            return;

        await EnqueueAsync(VtsExternalSyncModules.VehicleGeofenceMap, entity.Id.ToString(), method, payload, ct);
    }

    private async Task EnqueueAsync(string moduleName, string entityId, HttpMethod method, object payload, CancellationToken ct)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var log = new ExternalApiLog
        {
            ServiceName = moduleName,
            Payload = payloadJson,
            Status = ExternalApiLogStatus.Pending,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _logRepository.AddAsync(log, ct);
        await _logRepository.SaveChangesAsync(ct);

        var envelope = new VtsExternalSyncEnvelope
        {
            ExternalApiLogId = log.Id,
            Operation = method.Method,
            PayloadJson = payloadJson
        };

        await _queueService.EnqueueAsync(new ExternalSyncQueueCreateRequest
        {
            ModuleName = moduleName,
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(envelope),
            PreservePayload = true
        }, ct);
    }

    private async Task<List<ExternalVehicleMappingRequest>?> BuildVehicleDevicePayloadAsync(map_vehicle_device entity, CancellationToken ct)
    {
        var vehicle = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.Fk_VehicleId, ct);
        var device = await _db.Devices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.Fk_DeviceId, ct);
        var account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.AccountId == entity.AccountId, ct);

        if (vehicle == null || device == null || account == null)
            return null;

        return new List<ExternalVehicleMappingRequest>
        {
            new()
            {
                VehicleId = vehicle.Id.ToString(),
                VehicleNo = vehicle.VehicleNumber,
                DeviceNo = device.DeviceNo,
                Imei = device.DeviceImeiOrSerial,
                DeviceType = entity.fk_devicetypeid.ToString(),
                OrgName = account.AccountName,
                OrgId = entity.AccountId
            }
        };
    }

    private static ExternalGeofenceRequest BuildGeofencePayload(mst_Geofence entity, List<CoordinateDto> coordinates)
    {
        var center = coordinates.First();

        return new ExternalGeofenceRequest
        {
            GeoId = entity.Id.ToString(),
            GeoName = entity.DisplayName,
            OrgId = entity.AccountId,
            OrgName = $"Org-{entity.AccountId}",
            Latitude = center.Latitude,
            Longitude = center.Longitude,
            Radius = entity.RadiusM ?? 0,
            GeoType = entity.GeometryType,
            GeoPoints = coordinates.Select(x => new ExternalGeoPoint
            {
                Latitude = x.Latitude,
                Longitude = x.Longitude
            }).ToList()
        };
    }

    private async Task<List<ExternalGeofenceMappingRequest>?> BuildVehicleGeofencePayloadAsync(map_vehicle_geofence entity, CancellationToken ct)
    {
        var vehicle = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.VehicleId, ct);
        var geofence = await _db.GeofenceZones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.GeofenceId, ct);
        var deviceMap = await _db.VehicleDeviceMaps
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Fk_VehicleId == entity.VehicleId && x.IsActive && !x.IsDeleted, ct);

        // if (vehicle == null || geofence == null)
        //     return null;

        var device = await _db.Devices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.Id, ct);
        if (device == null)
            return null;

        return new List<ExternalGeofenceMappingRequest>
        {
            new()
            {
                vehicleId = vehicle.Id.ToString(),
                vehicleNo = vehicle.VehicleNumber,
                deviceNo = device.DeviceNo,
                geofence = new List<ExternalGeofenceItem>
                {
                    new()
                    {
                        geoId = geofence.Id,
                        tripNo = "0",
                        geoPoint = "START"
                    }
                }
            }
        };
    }
}
