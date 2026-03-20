INSERT INTO bulk_upload_config
(
    "ModuleKey",
    "DtoName",
    "ServiceInterface",
    "ServiceMethod",
    "ColumnsJson",
    "ExternalSync",
    "IsActive",
    "CreatedAt",
    "UpdatedAt"
)
VALUES
(
    'device-master',
    'CreateDeviceDto',
    'IDeviceService',
    'BulkCreateAsync',
    '[
      {"Header":"AccountId","Required":true},
      {"Header":"ManufacturerId","Required":true},
      {"Header":"DeviceTypeId","Required":true},
      {"Header":"DeviceNo","Required":true},
      {"Header":"DeviceImeiOrSerial","Required":true},
      {"Header":"CreatedBy","Required":true}
    ]',
    false,
    true,
    NOW(),
    NOW()
)
ON CONFLICT ("ModuleKey")
DO UPDATE SET
    "DtoName" = EXCLUDED."DtoName",
    "ServiceInterface" = EXCLUDED."ServiceInterface",
    "ServiceMethod" = EXCLUDED."ServiceMethod",
    "ColumnsJson" = EXCLUDED."ColumnsJson",
    "ExternalSync" = EXCLUDED."ExternalSync",
    "IsActive" = EXCLUDED."IsActive",
    "UpdatedAt" = NOW();
