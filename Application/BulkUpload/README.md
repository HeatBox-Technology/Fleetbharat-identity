# Bulk Upload Process

This module provides a generic bulk upload pipeline for Excel and CSV files. Each upload is driven by a `BulkUploadConfig` row, so the same engine can be reused across multiple modules without writing a separate importer each time.

## Main Components

- API entry: [Controllers/BulkUploadController.cs](/d:/FleetBharat/FleetBharat.Platform/Services/FleetBharat.IdentityService/Controllers/BulkUploadController.cs)
- Upload orchestration: [Application/BulkUpload/Services/BulkUploadService.cs](/d:/FleetBharat/FleetBharat.Platform/Services/FleetBharat.IdentityService/Application/BulkUpload/Services/BulkUploadService.cs)
- Background worker: [Infrastructure/BulkUpload/Workers/BulkUploadWorker.cs](/d:/FleetBharat/FleetBharat.Platform/Services/FleetBharat.IdentityService/Infrastructure/BulkUpload/Workers/BulkUploadWorker.cs)
- Queue: [Infrastructure/BulkUpload/Queue/BulkUploadQueue.cs](/d:/FleetBharat/FleetBharat.Platform/Services/FleetBharat.IdentityService/Infrastructure/BulkUpload/Queue/BulkUploadQueue.cs)
- Legacy processor: [Application/BulkUpload/Services/BulkProcessor.cs](/d:/FleetBharat/FleetBharat.Platform/Services/FleetBharat.IdentityService/Application/BulkUpload/Services/BulkProcessor.cs)
- Active processor: [Application/BulkUpload/Services/ConfigurableBulkProcessor.cs](/d:/FleetBharat/FleetBharat.Platform/Services/FleetBharat.IdentityService/Application/BulkUpload/Services/ConfigurableBulkProcessor.cs)
- Template generation: [Application/BulkUpload/Services/TemplateService.cs](/d:/FleetBharat/FleetBharat.Platform/Services/FleetBharat.IdentityService/Application/BulkUpload/Services/TemplateService.cs)
- Column config parsing: [Application/BulkUpload/DTOs/BulkUploadColumnDefinition.cs](/d:/FleetBharat/FleetBharat.Platform/Services/FleetBharat.IdentityService/Application/BulkUpload/DTOs/BulkUploadColumnDefinition.cs)
- Lookup resolvers: [Application/BulkUpload/Services/BulkLookupResolvers.cs](/d:/FleetBharat/FleetBharat.Platform/Services/FleetBharat.IdentityService/Application/BulkUpload/Services/BulkLookupResolvers.cs)
- Lookup dispatcher: [Application/BulkUpload/Services/LookupResolverService.cs](/d:/FleetBharat/FleetBharat.Platform/Services/FleetBharat.IdentityService/Application/BulkUpload/Services/LookupResolverService.cs)
- Uniqueness rules: [Application/BulkUpload/Services/BulkUniqueRules.cs](/d:/FleetBharat/FleetBharat.Platform/Services/FleetBharat.IdentityService/Application/BulkUpload/Services/BulkUniqueRules.cs)
- Uniqueness dispatcher: [Application/BulkUpload/Services/UniqueFieldValidator.cs](/d:/FleetBharat/FleetBharat.Platform/Services/FleetBharat.IdentityService/Application/BulkUpload/Services/UniqueFieldValidator.cs)

## Supported APIs

- `POST /api/bulk-upload/{moduleKey}`
  Uploads a `.xlsx` or `.csv` file using multipart form-data key `file`.
- `GET /api/bulk-upload/status/{jobId}`
  Returns current job progress and counters.
- `GET /api/bulk-upload/template/{moduleKey}?format=excel|csv`
  Generates a template from the configured columns.
- `GET /api/bulk-upload/error-report/{jobId}`
  Downloads the generated Excel error report if any rows failed.

## End-to-End Flow

1. The client downloads a template for a module.
2. The client fills rows and uploads the file to `POST /api/bulk-upload/{moduleKey}`.
3. `BulkUploadService` validates the module config and file extension, then parses the file into `List<Dictionary<string,string>>`.
4. A `bulk_job` row is created with status `PENDING`.
5. A `BulkUploadWorkItem` is pushed to the in-memory bounded channel queue.
6. `BulkUploadWorker` reads from the queue and calls `IBulkProcessor.ProcessAsync`.
7. `ConfigurableBulkProcessor` sets the job to `PROCESSING`, resolves the configured DTO and service, and preloads all configured lookup resolvers into memory.
8. Each row is mapped into a DTO using header aliases, lookup resolution, type conversion, JSON-driven validation metadata, and optional module custom validators.
9. Duplicate validation runs in two stages: within the uploaded file and against the database using pluggable uniqueness rules.
10. Each valid batch is sent to the configured service method.
11. If `ExternalSync = true`, the same batch is POSTed to `{BulkUpload:ExternalSyncBaseUrl}/bulk-sync/{moduleKey}`.
12. Job counters are updated and the final status becomes `COMPLETED` or `COMPLETED_WITH_ERRORS`.
13. Failed rows are saved in `bulk_job_rows`, and an Excel error file is written under `uploads/bulk-errors/`.

## Configuration Model

Each module must have an active `BulkUploadConfig` row.

Key fields:

- `ModuleKey`: route key used by the API, such as `GEOFENCE`.
- `DtoName`: DTO type name to instantiate for each row.
- `ServiceInterface`: registered DI service to resolve.
- `ServiceMethod`: method to call on the resolved service.
- `ColumnsJson`: column metadata used for template generation and row mapping.
- `LookupType`: resolves friendly uploaded values into backend ids.
- `Unique`: enables uniqueness validation.
- `UniqueWith`: scope columns used for composite uniqueness.
- `MaxLength`, `MinLength`, `Regex`, `AllowedValues`: config-driven validation metadata.
- `ExternalSync`: whether processed batches should also be sent to the external sync endpoint.
- `IsActive`: only active configs are used.

The target service method may have one of these signatures:

- `Task Method(List<TDto> items)`
- `Task Method(TDto item)`
- synchronous versions of the same shapes also work

## `ColumnsJson` Format

The parser accepts either simple strings or objects.

Simple form:

```json
[
  "AccountId",
  "DisplayName",
  "CreatedBy"
]
```

Expanded form:

```json
[
  {
    "propertyName": "AccountId",
    "header": "Account Name",
    "lookupType": "account",
    "required": true,
    "includeInTemplate": true,
    "aliases": ["Account", "Account Name", "AccountCode"]
  },
  {
    "propertyName": "DisplayName",
    "header": "Display Name",
    "required": true
  }
]
```

Recognized object keys include `property`, `propertyName`, `field`, `dtoProperty`, `key`, `name`, and `header`.

Extended metadata example:

```json
[
  {
    "propertyName": "AccountId",
    "header": "AccountName",
    "lookupType": "account",
    "required": true,
    "aliases": ["AccountName", "Account Name", "Account"]
  },
  {
    "propertyName": "VehicleNumber",
    "header": "VehicleNumber",
    "required": true,
    "unique": true,
    "uniqueWith": ["AccountId"],
    "maxLength": 50,
    "regex": "^[A-Za-z0-9-]+$"
  },
  {
    "propertyName": "VinOrChassisNumber",
    "header": "VinOrChassisNumber",
    "maxLength": 100
  },
  {
    "propertyName": "VehicleTypeId",
    "header": "VehicleTypeName",
    "lookupType": "vehicleType",
    "required": true,
    "aliases": ["VehicleTypeName", "Vehicle Type", "Vehicle Type Name"]
  }
]
```

## Mapping Rules

- Rows are matched by DTO property name first, then configured aliases, then case-insensitive property name.
- Blank values fail only when the column is marked `required`.
- `CreatedBy` and `UpdatedBy` are treated as system-managed fields and are excluded from generated templates by default.
- If `CreatedBy` or `UpdatedBy` exist on the DTO and are writable `int`/`int?`, the processor fills them from `ICurrentUserService.AccountId`.
- Row numbers in errors are Excel-style data row numbers, so the first data row is row `2`.
- Friendly uploaded values such as `AccountName` or `VehicleTypeName` are resolved into backend ids before the DTO is sent to the module service.

## Lookup Resolution

String values can be converted into numeric ids before DTO assignment when a lookup type is configured or inferred.

Built-in lookups:

- `account`
- `vehicleType`
- `deviceType`
- `manufacturer`
- `geofence`

Examples:

- `AccountId` can be uploaded as account name, account code, or `Account Name (AccountCode)`.
- `VehicleTypeId`, `DeviceTypeId`, and `ManufacturerId` can be uploaded using friendly names instead of ids.

If a lookup value is not found, or matches multiple records, that row fails validation.

Lookup resolvers are pluggable through `IBulkLookupResolver`, and `LookupResolverService` dispatches them case-insensitively by `LookupType`.

## Validation and Conversion

- DTO validation uses data annotations via `ValidationService`.
- Config-driven validation supports `required`, `minLength`, `maxLength`, `regex`, `allowedValues`, and `lookupType`.
- Primitive conversions are handled centrally in `BulkProcessor`, including `int`, `long`, `decimal`, `double`, `float`, `bool`, `DateTime`, `Guid`, enums, and nullable variants.
- Boolean values support `true/false` and `1/0`.
- Invalid conversions are reported as row-level errors.

## Duplicate Validation

The engine validates duplicates in two places.

- Inside the uploaded file using `unique` and `uniqueWith`.
- Against the database using `IUniqueFieldValidator` and pluggable `IBulkUniqueRule` implementations.

Current database uniqueness rules are registered for:

- `VEHICLE`
- `DEVICE`
- `DRIVER`
- `GEOFENCE`

## Custom Validators

Optional module-specific validation hooks can be added by implementing `IBulkCustomValidator`.

Use custom validators only for rules that cannot be handled through `ColumnsJson`, such as:

- IMEI to manufacturer consistency
- geofence geometry business checks
- driver license expiry rules

## Batching and Parallelism

- Row mapping and validation run in parallel.
- Maximum parallelism is capped at `8` threads, with a lower cap on small machines.
- Valid rows are processed in batches of `100`.
- If one batch call throws, every row in that batch is marked failed with the same error.

## Job Status and Error Handling

Job status progression:

- `PENDING`
- `PROCESSING`
- `COMPLETED`
- `COMPLETED_WITH_ERRORS`

Failure outputs:

- `bulk_job_rows` stores each failed row payload and error message.
- `bulk_jobs.ErrorFilePath` points to the generated Excel error file.
- Error files are saved to `uploads/bulk-errors/bulk_errors_{jobId}_{timestamp}.xlsx`.

## Template Behavior

Templates are generated from `ColumnsJson`.

- Excel templates contain one worksheet named `Template`.
- CSV templates contain only the header row.
- Only columns with `IncludeInTemplate = true` are included.
- System-managed fields such as `Id`, `CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt`, `DeletedBy`, `DeletedAt`, `IsDeleted`, and `Status` are excluded.
- Friendly headers are auto-generated for known properties such as `AccountId`, `VehicleTypeId`, `DeviceTypeId`, and `ManufacturerId`.

Suggested vehicle template columns:

- `AccountName`
- `VehicleNumber`
- `VinOrChassisNumber`
- `VehicleTypeName`

## Operational Notes

- The queue is in-memory and bounded to `200` items. It is single-reader and multi-writer.
- Because the queue is in-memory, queued items do not survive application restarts.
- Accepted file types are only `.xlsx` and `.csv`.
- Upload request size is limited to `200_000_000` bytes on the controller action.
- External sync is skipped with a warning if `BulkUpload:ExternalSyncBaseUrl` is not configured.
- Lookup data is preloaded once per job to avoid querying the database for every row.
- Future modules should extend the engine through `BulkUploadConfig`, `IBulkLookupResolver`, `IBulkUniqueRule`, and optional `IBulkCustomValidator` rather than adding module-specific `if/else` logic.

## Minimal Example

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
    {"propertyName":"AccountId","header":"Account Name","lookupType":"account","required":true},
    {"propertyName":"DisplayName","header":"Display Name","required":true},
    {"propertyName":"CreatedBy","header":"Created By","includeInTemplate":false}
  ]',
  false,
  true,
  NOW(),
  NOW()
);
```

## Quick Test Flow

1. `GET /api/bulk-upload/template/GEOFENCE?format=excel`
2. Fill sample rows in the downloaded file.
3. `POST /api/bulk-upload/GEOFENCE` with multipart form-data key `file`
4. Capture the returned `jobId`.
5. `GET /api/bulk-upload/status/{jobId}`
6. If `FailedRows > 0`, call `GET /api/bulk-upload/error-report/{jobId}`.

## Example Vehicle Row

Uploaded row:

```json
{
  "AccountName": "IoTH",
  "VehicleNumber": "RJ12345",
  "VinOrChassisNumber": "VIN001",
  "VehicleTypeName": "Van"
}
```

Resolved DTO:

```csharp
new CreateVehicleDto
{
    AccountId = 12,
    VehicleNumber = "RJ12345",
    VinOrChassisNumber = "VIN001",
    VehicleTypeId = 3,
    CreatedBy = currentUserId
}
```

## Example Errors

- `Account Name 'Unknown Account' was not found for lookup 'account'.`
- `VehicleNumber cannot exceed 50 characters.`
- `VehicleNumber format is invalid.`
- `VehicleNumber 'RJ12345' is duplicated inside the uploaded file for scope [AccountId=12].`
- `VehicleNumber 'RJ12345' already exists in the database for scope [AccountId=12].`
- `Lookup resolver 'vendor' is not registered.`

## DI Registration

```csharp
builder.Services.AddScoped<ILookupResolverService, LookupResolverService>();
builder.Services.AddScoped<IUniqueFieldValidator, UniqueFieldValidator>();

builder.Services.AddScoped<IBulkLookupResolver, AccountBulkLookupResolver>();
builder.Services.AddScoped<IBulkLookupResolver, VehicleTypeBulkLookupResolver>();
builder.Services.AddScoped<IBulkLookupResolver, ManufacturerBulkLookupResolver>();
builder.Services.AddScoped<IBulkLookupResolver, DeviceTypeBulkLookupResolver>();
builder.Services.AddScoped<IBulkLookupResolver, GeofenceBulkLookupResolver>();

builder.Services.AddScoped<IBulkUniqueRule, VehicleBulkUniqueRule>();
builder.Services.AddScoped<IBulkUniqueRule, DeviceBulkUniqueRule>();
builder.Services.AddScoped<IBulkUniqueRule, DriverBulkUniqueRule>();
builder.Services.AddScoped<IBulkUniqueRule, GeofenceBulkUniqueRule>();

builder.Services.AddScoped<IBulkProcessor, ConfigurableBulkProcessor>();
```

## Scalability Notes

- Keep persistence logic in the target module services, and let the bulk engine focus on parsing, mapping, validation, and batching.
- Prefer `BulkCreateAsync(List<TDto>)` service methods for high-volume modules.
- Add new modules by seeding `bulk_upload_config`, implementing lookup resolvers only when needed, and adding uniqueness rules only when the module requires DB-level duplicate checks.

## Test Cases

### 1. Template Download - Excel

- Call `GET /api/bulk-upload/template/VEHICLE?format=excel`
- Verify response status is `200`
- Verify file name ends with `_template.xlsx`
- Verify sheet name is `Template`
- Verify headers contain only friendly column names like `Account Name`, `Vehicle Number`, `VIN Or Chassis Number`, `Vehicle Type Name`
- Verify system-managed fields like `Id`, `CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt`, `IsDeleted`, and `Status` are not present

### 2. Template Download - CSV

- Call `GET /api/bulk-upload/template/VEHICLE?format=csv`
- Verify response status is `200`
- Verify file name ends with `_template.csv`
- Verify CSV contains only the configured header row
- Verify system-managed fields are excluded

### 3. Upload Valid Vehicle File

- Prepare an Excel or CSV file with valid rows:
  `AccountName`, `VehicleNumber`, `VinOrChassisNumber`, `VehicleTypeName`
- Call `POST /api/bulk-upload/VEHICLE` with multipart key `file`
- Verify response status is `202 Accepted`
- Capture `jobId`
- Poll `GET /api/bulk-upload/status/{jobId}`
- Verify final status is `COMPLETED`
- Verify `SuccessRows` equals uploaded row count
- Verify `FailedRows` is `0`
- Verify records are inserted in `mst_vehicle`

### 4. Upload Valid Device File With Lookups

- Prepare a file using friendly values for `AccountName`, `Manufacturer Name`, and `Device Type Name`
- Upload using `POST /api/bulk-upload/DEVICE`
- Verify lookup values are resolved to numeric ids before persistence
- Verify final status is `COMPLETED`
- Verify inserted records contain resolved `AccountId`, `ManufacturerId`, and `DeviceTypeId`

### 5. Required Field Validation

- Prepare a row with blank `VehicleNumber`
- Upload the file
- Verify job status is `COMPLETED_WITH_ERRORS`
- Verify the error report contains a row-level message like `VehicleNumber is required`
- Verify failed row is stored in `bulk_job_rows`

### 6. Max Length Validation

- Prepare a row where `VehicleNumber` exceeds configured `maxLength`
- Upload the file
- Verify the row fails
- Verify the error message is similar to `VehicleNumber cannot exceed 50 characters`

### 7. Min Length Validation

- Configure a column with `minLength`
- Upload a row with shorter input than allowed
- Verify the row fails
- Verify the error message mentions minimum character length

### 8. Regex Validation

- Configure `VehicleNumber` with regex `^[A-Za-z0-9-]+$`
- Upload a row containing invalid characters such as `RJ@123`
- Verify the row fails
- Verify the error message is similar to `VehicleNumber format is invalid`

### 9. Allowed Values Validation

- Configure a column with `allowedValues`, for example `["ACTIVE","INACTIVE"]`
- Upload a row with value `PENDING`
- Verify the row fails
- Verify the error message lists the allowed values

### 10. Account Lookup Success

- Upload a row with `AccountName = IoTH`
- Ensure matching account exists in database and is within current user scope
- Verify the row succeeds
- Verify DTO receives resolved `AccountId`

### 11. Account Lookup Not Found

- Upload a row with a non-existent `AccountName`
- Verify the row fails
- Verify the error message is similar to `Account Name 'Unknown Account' was not found for lookup 'account'`

### 12. Lookup Ambiguous Match

- Seed two records that normalize to the same lookup value if applicable
- Upload a row using that shared friendly value
- Verify the row fails
- Verify the message says the lookup matched multiple records and asks for a unique value

### 13. Vehicle Type Lookup Success

- Upload a row with `VehicleTypeName = Van`
- Verify it resolves to the correct `VehicleTypeId`
- Verify the row is persisted successfully

### 14. Manufacturer Lookup Success

- Upload a device row with `Manufacturer Name`
- Verify it resolves correctly to `ManufacturerId`
- Verify final persistence is successful

### 15. Geofence Lookup Success

- For a module using `GeofenceId`, upload a row with a friendly geofence name
- Verify it resolves correctly to the stored geofence id

### 16. Duplicate Inside Same Upload File

- Upload two rows with the same `VehicleNumber` and same `AccountName`
- Verify both rows are marked failed when `unique = true` and `uniqueWith = ["AccountId"]`
- Verify the error report says the value is duplicated inside the uploaded file

### 17. Duplicate Across Different Scope In Same File

- Upload two rows with same `VehicleNumber` but different `AccountName`
- Verify rows pass when uniqueness is scoped by `AccountId`
- Verify final status is `COMPLETED` if no other validation fails

### 18. Duplicate Already Existing In Database

- Ensure a vehicle already exists in DB for `AccountId = 12` with `VehicleNumber = RJ12345`
- Upload a new row with the same scoped values
- Verify the row fails
- Verify the error report says the value already exists in the database

### 19. Non-Duplicate Across Different Scope In Database

- Ensure a vehicle exists for `AccountId = 12`, `VehicleNumber = RJ12345`
- Upload another row with `VehicleNumber = RJ12345` but a different valid account
- Verify row passes when uniqueness is scoped by `AccountId`

### 20. Mixed Success And Failure File

- Upload a file containing:
  one valid row
  one required-field failure
  one lookup failure
  one duplicate failure
- Verify final status is `COMPLETED_WITH_ERRORS`
- Verify `SuccessRows` and `FailedRows` match the actual breakdown
- Verify error report contains only failed rows

### 21. Custom Validator Failure

- Register an `IBulkCustomValidator` for a module
- Upload a row that violates the custom rule
- Verify the row fails even if base metadata validation passes
- Verify the custom validator message appears in the error report

### 22. Service Method Using `List<TDto>`

- Configure a module whose `ServiceMethod` accepts `List<TDto>`
- Upload multiple valid rows
- Verify the batch method is invoked successfully
- Verify all rows are inserted

### 23. Service Method Using Single `TDto`

- Configure a module whose `ServiceMethod` accepts one DTO at a time
- Upload multiple valid rows
- Verify processor calls the service once per row
- Verify all rows are inserted successfully

### 24. Invalid Service Interface Or DTO Name

- Seed a config with invalid `DtoName` or `ServiceInterface`
- Upload a file
- Verify processing fails
- Verify job does not silently succeed
- Verify logs clearly indicate missing type or service registration

### 25. Invalid Service Method

- Seed a config with wrong `ServiceMethod`
- Upload a file
- Verify processing fails with method resolution error

### 26. Unsupported File Extension

- Upload a `.txt` or `.json` file
- Verify API returns a bad request error
- Verify message says only `.xlsx` and `.csv` are supported

### 27. Empty File Upload

- Upload an empty file
- Verify API returns a bad request error
- Verify message says upload file is required

### 28. Status API

- Upload a valid file and capture `jobId`
- Call `GET /api/bulk-upload/status/{jobId}` while processing and after completion
- Verify `Status`, `TotalRows`, `ProcessedRows`, `SuccessRows`, `FailedRows`, and `CompletedAt` are returned correctly

### 29. Error Report API

- Upload a file with failed rows
- Call `GET /api/bulk-upload/error-report/{jobId}`
- Verify response is `200`
- Verify file is an Excel workbook
- Verify workbook contains `RowNumber` and `ErrorMessage` columns

### 30. No Error Report For Successful Job

- Upload a fully valid file
- Call `GET /api/bulk-upload/error-report/{jobId}`
- Verify API returns `404` or no report, depending on controller behavior

### 31. External Sync Disabled

- Configure `ExternalSync = false`
- Upload a valid file
- Verify data is inserted locally
- Verify no external sync call is attempted

### 32. External Sync Enabled With Base URL Configured

- Configure `ExternalSync = true`
- Set `BulkUpload:ExternalSyncBaseUrl`
- Upload a valid file
- Verify local insert succeeds
- Verify batch payload is posted to `/bulk-sync/{moduleKey}`

### 33. External Sync Enabled Without Base URL

- Configure `ExternalSync = true`
- Do not set `BulkUpload:ExternalSyncBaseUrl`
- Upload a valid file
- Verify local insert still succeeds
- Verify logs contain warning that external sync was skipped

### 34. Parallel Row Processing

- Upload a large file with hundreds of rows
- Verify the job completes successfully
- Verify row-level validation is stable and does not produce inconsistent lookup or duplicate results

### 35. Batch Failure Handling

- Force the configured service method to throw for one batch
- Upload enough valid rows to create at least one batch
- Verify every row in the failed batch is marked failed
- Verify rows from successful batches are counted as success

### 36. In-Memory Queue Behavior

- Upload multiple files quickly
- Verify all jobs are queued and processed in order by the single-reader worker
- Restart the application with queued items not yet processed
- Verify in-memory queued items do not survive restart

### 37. Account Scope Enforcement

- Use a user with limited accessible accounts
- Upload rows referencing an out-of-scope account
- Verify lookup resolution or downstream service rules reject unauthorized data

### 38. CSV Parsing With Header Aliases

- Create a CSV using alias headers like `Account Name` and `Vehicle Type Name`
- Upload the file
- Verify alias matching works and rows are mapped successfully

### 39. Excel Parsing With Blank Rows

- Create an Excel file with valid rows and blank rows in between
- Upload the file
- Verify blank rows are ignored
- Verify `TotalRows` reflects only meaningful data rows

### 40. Boolean And Date Conversion

- Configure columns mapped to boolean or date fields
- Upload values like `1`, `0`, `true`, `false`, and valid dates
- Verify conversion succeeds
- Upload invalid boolean/date values
- Verify row-level conversion errors are reported
