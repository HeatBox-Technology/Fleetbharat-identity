# FleetBharat.IdentityService

ASP.NET Core (`net8.0`) Identity/Account service for FleetBharat.

## Requirements

- .NET SDK 8.0+
- PostgreSQL (database for EF Core)
  -postGIS for geofencing need to install in postgress
- Redis (used by live tracking/subscriber features)
- SMTP credentials (for OTP/reset/onboarding emails)

## Configuration Required

Set these in `appsettings.json` / `appsettings.{Environment}.json` or environment variables.

### `ConnectionStrings`

- `ConnectionStrings:Default`

### `Jwt`

- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:AccessTokenExpiryMinutes`

### `Redis`

- `Redis:ConnectionString`

### `Email`

- `Email:SmtpHost`
- `Email:SmtpPort`
- `Email:Username`
- `Email:Password`
- `Email:FromEmail`
- `Email:FromName`

### `Frontend`

- `Frontend:ResetPasswordUrl`

### Optional

- `Cors:AllowedOrigins` (array)

## First-Time Setup

1. Restore packages:

```bash
dotnet restore
```

2. Apply migrations:

```bash
dotnet ef database update
```

If `dotnet-ef` is missing:

```bash
dotnet tool install --global dotnet-ef
```

## Run Locally

```bash
dotnet run
```

API starts using `Program.cs` settings and exposes Swagger UI (enabled in current setup).

## Helpful Commands

- Build only:

```bash
dotnet build
```

- Run with environment:

```bash
set ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

## Notes

- Email templates are loaded from `docs/email-templates/`.
- Do not commit real secrets to source control. Use environment variables or secret manager for production.

## Generic Bulk + External Sync (Implementation Guide)

This service has two different concepts:

- `Bulk Upload Engine`: Parses Excel/CSV and inserts data using `bulk_upload_config`.
- `External Sync Engine`: Queues and retries external integration calls using:
  - `external_sync_config`
  - `external_sync_queue`
  - `external_sync_dead_letter`

### Code Locations

- Bulk upload entry/API: `Controllers/BulkUploadController.cs`
- Bulk processing: `Application/BulkUpload/Services/BulkProcessor.cs`
- External sync worker: `Infrastructure/ExternalSync/Workers/ExternalSyncWorker.cs`
- External sync queue service: `Application/Services/ExternalSync/ExternalSyncQueueService.cs`
- External sync repository: `Infrastructure/Data/Repositories/ExternalSyncRepository.cs`
- External sync API/dashboard: `Controllers/ExternalSyncController.cs`

## Required Table Seed Entries (Examples)

### 1) Bulk config for Geofence (`bulk_upload_config`)

Use this to enable Excel template generation + processing for Geofence:

```sql
INSERT INTO bulk_upload_config
("ModuleKey","DtoName","ServiceInterface","ServiceMethod","ColumnsJson","ExternalSync","IsActive","CreatedAt","UpdatedAt")
VALUES
(
  'GEOFENCE',
  'CreateGeofenceDto',
  'IGeofenceService',
  'CreateAsync',
  '[
    "AccountId",
    "UniqueCode",
    "DisplayName",
    "Description",
    "ClassificationCode",
    "ClassificationLabel",
    "GeometryType",
    "RadiusM",
    "CoordinatesJson",
    "ColorTheme",
    "Opacity",
    "IsEnabled",
    "CreatedBy"
  ]',
  false,
  true,
  NOW(),
  NOW()
);
```

### 2) External sync config (`external_sync_config`)

Use this to enable queue-based sync for a module:

```sql
INSERT INTO external_sync_config
(module_name, service_interface, service_method, retry_enabled, max_retry_count, retry_interval_minutes, is_active, created_at)
VALUES
('GEOFENCE', 'IExampleExternalSyncService', 'SyncAsync', true, 5, 5, true, NOW());
```

### 3) Optional test queue row (`external_sync_queue`)

```sql
INSERT INTO external_sync_queue
(module_name, entity_id, payload_json, status, retry_count, next_retry_time, created_at)
VALUES
('GEOFENCE', 'TEST-001', '{"sample":"payload"}', 'Pending', 0, NOW(), NOW());
```

## Full Test Cases

### Test Case A: Geofence Bulk Upload (Excel)

1. Download template:
   - `GET /api/bulk-upload/template/GEOFENCE?format=excel`
2. Fill template rows with valid values.
3. Upload file:
   - `POST /api/bulk-upload/GEOFENCE` (form-data key: `file`)
4. Capture `jobId` from response.
5. Check status:
   - `GET /api/bulk-upload/status/{jobId}`
6. If failed rows exist, download report:
   - `GET /api/bulk-upload/error-report/{jobId}`

Expected:
- `bulk_jobs` row created and moved from `PENDING` -> `PROCESSING` -> `COMPLETED` or `COMPLETED_WITH_ERRORS`.
- Success/failed row counters are updated.

### Test Case B: External Sync Worker (Queue Processing)

1. Ensure `external_sync_config` has active row for module (example above).
2. Queue an item using API:
   - `POST /api/external-sync/enqueue`
   - Body:

```json
{
  "moduleName": "GEOFENCE",
  "entityId": "GF-1001",
  "payloadJson": "{\"id\":1001,\"name\":\"Zone-A\"}"
}
```

3. Wait for worker cycle (runs every ~10 seconds).
4. Validate queue row status in DB or dashboard.

Expected:
- `Pending` -> `Processing` -> `Success` if invocation is successful.
- On exception, retry count increments and `next_retry_time` is set.

### Test Case C: Retry + DLQ

1. Configure a failing sync target (wrong method/interface) in `external_sync_config`.
2. Enqueue one item.
3. Let worker retry until `max_retry_count` reached.
4. Verify item moved to `external_sync_dead_letter`.

Expected:
- Queue item removed from `external_sync_queue`.
- DLQ entry present with error message and retry count.

### Test Case D: Dashboard + Manual Recovery

1. Get module stats:
   - `GET /api/external-sync/dashboard`
2. View failed queue records:
   - `GET /api/external-sync/failed?take=100`
3. Retry failed item:
   - `POST /api/external-sync/retry/{queueId}`
4. View DLQ:
   - `GET /api/external-sync/dlq?take=100`
5. Reprocess DLQ item:
   - `POST /api/external-sync/dlq/{dlqId}/reprocess`

Expected:
- Failed rows can be re-queued.
- DLQ rows can be moved back to queue for reprocessing.

## Verification SQL (Quick Checks)

```sql
-- Active external sync configs
SELECT * FROM external_sync_config WHERE is_active = true;

-- Queue status by module
SELECT module_name, status, COUNT(*)
FROM external_sync_queue
GROUP BY module_name, status
ORDER BY module_name, status;

-- DLQ count by module
SELECT module_name, COUNT(*) AS dlq_count
FROM external_sync_dead_letter
GROUP BY module_name
ORDER BY module_name;
```

## Important Note for Geofence Excel Mapping

`CreateGeofenceDto` includes a complex `Coordinates` list. Generic row mapping handles simple scalar fields directly from column values. If coordinates are not mapped correctly from the uploaded file, geofence creation can fail during geometry build. In that case, check the error report and align file format/conversion logic for coordinate fields.

## Recent Changes Test Cases (March 2026)

This section covers the newest API and storage changes added in the latest update.

### Test Case E: Account Hierarchy API

1. Call:
   - `GET /api/accounts/hierarchy`
2. Validate response shape contains hierarchy nodes for the current logged-in user scope.

Expected:
- HTTP `200`.
- Response returns account hierarchy data (not paged account list).

### Test Case F: Solutions and Modules Lookup

1. Get active solutions:
   - `GET /api/solutions`
2. Pick one solution id and fetch modules:
   - `GET /api/modules?solutionId={solutionId}`

Expected:
- HTTP `200` on both endpoints.
- Only active solutions/modules are returned.
- Module records include `formModuleId`, `solutionId`, `moduleCode`, and `moduleName`.

### Test Case G: Forms Filtered by Module

1. Call without module filter:
   - `GET /api/forms?page=1&pageSize=10`
2. Call with module filter:
   - `GET /api/forms?page=1&pageSize=10&moduleId={formModuleId}`
3. Optionally test no-pagination mode:
   - `GET /api/forms?pageSize=0&moduleId={formModuleId}`

Expected:
- HTTP `200`.
- With `moduleId`, all returned rows belong to the selected module.
- `FormResponseDto` includes `formModuleId`.

### Test Case H: Form Filter Config Lookup

1. Ensure one `mst_form` record has valid `FilterConfigJson`, for example:

```json
{
  "filters": [
    { "name": "AccountId", "type": "dropdown" }
  ]
}
```

2. Call:
   - `GET /api/common/filter-config?formName={formNameOrCode}`

Expected:
- HTTP `200` when config exists.
- HTTP `404` when form/config is missing.
- Response contains `formName` and `filters`.

### Test Case I: User Profile Image Upload + Static Access

1. Upload profile image:
   - `PATCH /api/users/{userId}/profile-image` with multipart `file` (`image/jpeg` or `image/png`, size < 2MB).
2. Capture `profileImageUrl` from response.
3. Open returned URL path:
   - `GET {profileImageUrl}` (example `/uploads/profiles/{userId}.png`)

Expected:
- Upload API returns HTTP `200` with `profileImageUrl`.
- File is reachable through static files middleware under `/uploads`.
- Invalid type/size is rejected with validation error.

### Test Case J: WhiteLabel Logo Upload

1. Upload logo:
   - `POST /api/whitelabel/{accountId}/logo` with multipart `file` (`image/png`, size < 2MB).
2. Validate response fields:
   - `whiteLabelId`, `accountId`, `brandName`, `logoName`, `logoPath`, `fileUrl`
3. Open logo URL:
   - `GET {fileUrl}` (example `/uploads/whitelabel/{accountId}.png`)

Expected:
- HTTP `200` with logo metadata.
- White label row is created if missing for that account.
- Non-PNG upload is rejected.
