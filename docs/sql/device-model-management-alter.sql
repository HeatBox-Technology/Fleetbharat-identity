ALTER TABLE IF EXISTS public."DeviceModels"
    ADD COLUMN IF NOT EXISTS "UseIMEIAsPrimaryId" boolean NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS "DeviceNo" varchar(100) NULL,
    ADD COLUMN IF NOT EXISTS "IMEISerialNumber" varchar(100) NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_DeviceModels_DeviceNo_Active"
    ON public."DeviceModels" (LOWER("DeviceNo"))
    WHERE "IsDeleted" = FALSE
      AND "DeviceNo" IS NOT NULL
      AND BTRIM("DeviceNo") <> '';

CREATE OR REPLACE FUNCTION public.set_devicemodels_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_devicemodels_updated_at ON public."DeviceModels";

CREATE TRIGGER trg_devicemodels_updated_at
BEFORE UPDATE ON public."DeviceModels"
FOR EACH ROW
EXECUTE FUNCTION public.set_devicemodels_updated_at();
