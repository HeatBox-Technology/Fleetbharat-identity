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
            @"
            CREATE TABLE IF NOT EXISTS ""ExternalApiLogs"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""ServiceName"" VARCHAR(150) NOT NULL,
                ""Payload"" TEXT NOT NULL,
                ""Response"" TEXT NULL,
                ""Status"" VARCHAR(20) NOT NULL,
                ""RetryCount"" INT NOT NULL DEFAULT 0,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""LastRetryAt"" TIMESTAMP WITHOUT TIME ZONE NULL
            );

            CREATE TABLE IF NOT EXISTS external_sync_config (
                id BIGSERIAL PRIMARY KEY,
                module_name VARCHAR(100) NOT NULL,
                service_interface VARCHAR(200) NOT NULL,
                service_method VARCHAR(100) NOT NULL,
                max_retry_count INT NOT NULL DEFAULT 5,
                retry_interval_minutes INT NOT NULL DEFAULT 5,
                retry_enabled BOOLEAN NOT NULL DEFAULT TRUE,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_external_sync_config_module_name""
                ON external_sync_config (module_name);

            CREATE TABLE IF NOT EXISTS external_sync_queue (
                id BIGSERIAL PRIMARY KEY,
                module_name VARCHAR(100) NOT NULL,
                entity_id VARCHAR(100) NOT NULL,
                payload_json jsonb NOT NULL,
                status VARCHAR(20) NOT NULL,
                retry_count INT NOT NULL DEFAULT 0,
                next_retry_time TIMESTAMP WITHOUT TIME ZONE NULL,
                error_message VARCHAR(2000) NULL,
                created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                last_attempt_at TIMESTAMP WITHOUT TIME ZONE NULL
            );

            CREATE INDEX IF NOT EXISTS ""IX_external_sync_queue_status_retry""
                ON external_sync_queue (status, next_retry_time);

            CREATE INDEX IF NOT EXISTS ""IX_external_sync_queue_created""
                ON external_sync_queue (created_at);

            CREATE INDEX IF NOT EXISTS ""IX_external_sync_queue_module""
                ON external_sync_queue (module_name);

            CREATE TABLE IF NOT EXISTS external_sync_dead_letter (
                id BIGSERIAL PRIMARY KEY,
                module_name VARCHAR(100) NOT NULL,
                entity_id VARCHAR(100) NOT NULL,
                payload_json jsonb NOT NULL,
                error_message VARCHAR(2000) NOT NULL,
                retry_count INT NOT NULL DEFAULT 0,
                created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                moved_to_dlq_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS ""IX_external_sync_dead_letter_module_name""
                ON external_sync_dead_letter (module_name);
            ", ct);

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
