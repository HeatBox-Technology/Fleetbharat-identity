using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class ExternalSyncBootstrapper
{
    private readonly IdentityDbContext _db;

    public ExternalSyncBootstrapper(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlRawAsync(
            @"CREATE TABLE IF NOT EXISTS ""ExternalApiLogs"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""ServiceName"" VARCHAR(150) NOT NULL,
                ""Payload"" TEXT NOT NULL,
                ""Response"" TEXT NULL,
                ""Status"" VARCHAR(20) NOT NULL,
                ""RetryCount"" INT NOT NULL DEFAULT 0,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""LastRetryAt"" TIMESTAMP WITHOUT TIME ZONE NULL
            );", ct);

        await UpsertConfigAsync(VtsExternalSyncModules.Geofence, "IVtsExternalApiSyncDispatcher", nameof(IVtsExternalApiSyncDispatcher.SyncGeofenceAsync), ct);
        await UpsertConfigAsync(VtsExternalSyncModules.VehicleDeviceMap, "IVtsExternalApiSyncDispatcher", nameof(IVtsExternalApiSyncDispatcher.SyncVehicleDeviceMappingAsync), ct);
        await UpsertConfigAsync(VtsExternalSyncModules.VehicleGeofenceMap, "IVtsExternalApiSyncDispatcher", nameof(IVtsExternalApiSyncDispatcher.SyncVehicleGeofenceMappingAsync), ct);
    }

    private async Task UpsertConfigAsync(string moduleName, string serviceInterface, string serviceMethod, CancellationToken ct)
    {
        var row = await _db.external_sync_configs.FirstOrDefaultAsync(x => x.ModuleName == moduleName, ct);
        if (row == null)
        {
            await _db.external_sync_configs.AddAsync(new external_sync_config
            {
                ModuleName = moduleName,
                ServiceInterface = serviceInterface,
                ServiceMethod = serviceMethod,
                RetryEnabled = false,
                MaxRetryCount = 1,
                RetryIntervalMinutes = 0,
                IsActive = true,
                CreatedAt = System.DateTime.UtcNow
            }, ct);
        }
        else
        {
            row.ServiceInterface = serviceInterface;
            row.ServiceMethod = serviceMethod;
            row.RetryEnabled = false;
            row.MaxRetryCount = 1;
            row.RetryIntervalMinutes = 0;
            row.IsActive = true;
        }

        await _db.SaveChangesAsync(ct);
    }
}
