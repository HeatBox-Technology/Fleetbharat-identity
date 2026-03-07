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
