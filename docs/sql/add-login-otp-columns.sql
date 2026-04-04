ALTER TABLE "Users"
ADD COLUMN IF NOT EXISTS "LoginOtpCodeHash" text NULL;

ALTER TABLE "Users"
ADD COLUMN IF NOT EXISTS "LoginOtpExpiry" timestamp with time zone NULL;
