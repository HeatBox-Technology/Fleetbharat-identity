using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

public class DeviceModelRepository : IDeviceModelRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public DeviceModelRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<PagedResultDto<DeviceModelResponseDto>> GetAllAsync(
        DeviceModelGetAllRequestDto request,
        CancellationToken cancellationToken = default)
    {
        const string baseSql = """
            SELECT
                dm."Id",
                dm."ManufacturerId",
                COALESCE(manufacturer."Name", '') AS "ManufacturerName",
                dm."Name",
                dm."DeviceCategoryId",
                COALESCE(device_type."Name", '') AS "DeviceCategoryName",
                COALESCE(dm."UseIMEIAsPrimaryId", FALSE) AS "UseIMEIAsPrimaryId",
                dm."DeviceNo",
                dm."IMEISerialNumber",
                COALESCE(dm."IsEnabled", TRUE) AS "IsEnabled",
                dm."CreatedAt",
                dm."UpdatedAt",
                COUNT(*) OVER() AS "TotalRecords"
            FROM public."DeviceModels" dm
            LEFT JOIN public."OemManufacturer" manufacturer
                ON dm."ManufacturerId" = manufacturer."Id"
            LEFT JOIN public."mst_device_type" device_type
                ON dm."DeviceCategoryId" = device_type."Id"
            WHERE COALESCE(dm."IsDeleted", FALSE) = FALSE
            """;

        var sql = baseSql;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            sql += """
                
                AND dm."Name" ILIKE @Search
                """;
            command.Parameters.AddWithValue("Search", $"%{request.Search}%");
        }

        if (request.ManufacturerId.HasValue)
        {
            sql += """
                
                AND dm."ManufacturerId" = @ManufacturerId
                """;
            command.Parameters.AddWithValue("ManufacturerId", request.ManufacturerId.Value);
        }

        if (request.DeviceCategoryId.HasValue)
        {
            sql += """
                
                AND dm."DeviceCategoryId" = @DeviceCategoryId
                """;
            command.Parameters.AddWithValue("DeviceCategoryId", request.DeviceCategoryId.Value);
        }

        if (request.IsEnabled.HasValue)
        {
            sql += """
                
                AND dm."IsEnabled" = @IsEnabled
                """;
            command.Parameters.AddWithValue("IsEnabled", request.IsEnabled.Value);
        }

        sql += """
            
            ORDER BY COALESCE(dm."UpdatedAt", dm."CreatedAt") DESC, dm."Id" DESC
            OFFSET @Offset
            LIMIT @PageSize;
            """;

        command.Parameters.AddWithValue("Offset", (request.Page - 1) * request.PageSize);
        command.Parameters.AddWithValue("PageSize", request.PageSize);
        command.CommandText = sql;

        var items = new List<DeviceModelResponseDto>();
        var totalRecords = 0;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapDeviceModel(reader));

            if (totalRecords == 0)
                totalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
        }

        return new PagedResultDto<DeviceModelResponseDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords,
            TotalPages = totalRecords == 0
                ? 0
                : (int)Math.Ceiling(totalRecords / (double)request.PageSize)
        };
    }

    public async Task<DeviceModelResponseDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                dm."Id",
                dm."ManufacturerId",
                COALESCE(manufacturer."Name", '') AS "ManufacturerName",
                dm."Name",
                dm."DeviceCategoryId",
                COALESCE(device_type."Name", '') AS "DeviceCategoryName",
                COALESCE(dm."UseIMEIAsPrimaryId", FALSE) AS "UseIMEIAsPrimaryId",
                dm."DeviceNo",
                dm."IMEISerialNumber",
                COALESCE(dm."IsEnabled", TRUE) AS "IsEnabled",
                dm."CreatedAt",
                dm."UpdatedAt"
            FROM public."DeviceModels" dm
            LEFT JOIN public."OemManufacturer" manufacturer
                ON dm."ManufacturerId" = manufacturer."Id"
            LEFT JOIN public."mst_device_type" device_type
                ON dm."DeviceCategoryId" = device_type."Id"
            WHERE dm."Id" = @Id
              AND COALESCE(dm."IsDeleted", FALSE) = FALSE
            LIMIT 1;
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapDeviceModel(reader);
    }

    public async Task<bool> ManufacturerExistsAsync(
        int manufacturerId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM public."OemManufacturer"
                WHERE "Id" = @ManufacturerId
                  AND COALESCE("IsDeleted", FALSE) = FALSE
                  AND COALESCE("IsEnabled", TRUE) = TRUE
            );
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("ManufacturerId", manufacturerId);

        return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> DeviceTypeExistsAsync(
        int deviceCategoryId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM public."mst_device_type"
                WHERE "Id" = @DeviceCategoryId
                  AND COALESCE("IsDeleted", FALSE) = FALSE
                  AND COALESCE("IsEnabled", TRUE) = TRUE
                  AND COALESCE("IsActive", TRUE) = TRUE
            );
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("DeviceCategoryId", deviceCategoryId);

        return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> DeviceNoExistsAsync(
        string deviceNo,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT EXISTS (
                SELECT 1
                FROM public."DeviceModels"
                WHERE COALESCE("IsDeleted", FALSE) = FALSE
                  AND LOWER(BTRIM(COALESCE("DeviceNo", ''))) = LOWER(BTRIM(@DeviceNo))
            """;

        if (excludeId.HasValue)
        {
            sql += """
                
                  AND "Id" <> @ExcludeId
                """;
        }

        sql += """
                
            );
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("DeviceNo", deviceNo);

        if (excludeId.HasValue)
            command.Parameters.AddWithValue("ExcludeId", excludeId.Value);

        return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM public."DeviceModels"
                WHERE "Code" = @Code
                  AND COALESCE("IsDeleted", FALSE) = FALSE
            );
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("Code", code);

        return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<int> AddAsync(
        DeviceModel entity,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO public."DeviceModels"
            (
                "Code",
                "Name",
                "ManufacturerId",
                "DeviceCategoryId",
                "ProtocolType",
                "UseIMEIAsPrimaryId",
                "DeviceNo",
                "IMEISerialNumber",
                "IsEnabled",
                "CreatedAt",
                "UpdatedAt",
                "IsDeleted"
            )
            VALUES
            (
                @Code,
                @Name,
                @ManufacturerId,
                @DeviceCategoryId,
                @ProtocolType,
                @UseIMEIAsPrimaryId,
                @DeviceNo,
                @IMEISerialNumber,
                @IsEnabled,
                @CreatedAt,
                @UpdatedAt,
                @IsDeleted
            )
            RETURNING "Id";
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        command.Parameters.AddWithValue("Code", entity.Code);
        command.Parameters.AddWithValue("Name", entity.Name);
        command.Parameters.AddWithValue("ManufacturerId", entity.ManufacturerId);
        command.Parameters.AddWithValue("DeviceCategoryId", entity.DeviceCategoryId);
        command.Parameters.AddWithValue("ProtocolType", entity.ProtocolType);
        command.Parameters.AddWithValue("UseIMEIAsPrimaryId", entity.UseIMEIAsPrimaryId);
        AddNullableString(command, "DeviceNo", entity.DeviceNo);
        AddNullableString(command, "IMEISerialNumber", entity.IMEISerialNumber);
        command.Parameters.AddWithValue("IsEnabled", entity.IsEnabled);
        command.Parameters.AddWithValue("CreatedAt", entity.CreatedAt);
        AddNullableDateTime(command, "UpdatedAt", entity.UpdatedAt);
        command.Parameters.AddWithValue("IsDeleted", entity.IsDeleted);

        var insertedId = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(insertedId);
    }

    public async Task<bool> UpdateAsync(
        DeviceModel entity,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE public."DeviceModels"
            SET
                "Name" = @Name,
                "ManufacturerId" = @ManufacturerId,
                "DeviceCategoryId" = @DeviceCategoryId,
                "UseIMEIAsPrimaryId" = @UseIMEIAsPrimaryId,
                "DeviceNo" = @DeviceNo,
                "IMEISerialNumber" = @IMEISerialNumber,
                "IsEnabled" = @IsEnabled,
                "UpdatedAt" = @UpdatedAt
            WHERE "Id" = @Id
              AND COALESCE("IsDeleted", FALSE) = FALSE;
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        command.Parameters.AddWithValue("Id", entity.Id);
        command.Parameters.AddWithValue("Name", entity.Name);
        command.Parameters.AddWithValue("ManufacturerId", entity.ManufacturerId);
        command.Parameters.AddWithValue("DeviceCategoryId", entity.DeviceCategoryId);
        command.Parameters.AddWithValue("UseIMEIAsPrimaryId", entity.UseIMEIAsPrimaryId);
        AddNullableString(command, "DeviceNo", entity.DeviceNo);
        AddNullableString(command, "IMEISerialNumber", entity.IMEISerialNumber);
        command.Parameters.AddWithValue("IsEnabled", entity.IsEnabled);
        AddNullableDateTime(command, "UpdatedAt", entity.UpdatedAt);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> SoftDeleteAsync(
        int id,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE public."DeviceModels"
            SET
                "IsDeleted" = TRUE,
                "UpdatedAt" = @UpdatedAt
            WHERE "Id" = @Id
              AND COALESCE("IsDeleted", FALSE) = FALSE;
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("Id", id);
        command.Parameters.AddWithValue("UpdatedAt", updatedAt);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<IReadOnlyList<DropdownDto>> GetManufacturerDropdownAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                "Id",
                "Name" AS "Value"
            FROM public."OemManufacturer"
            WHERE COALESCE("IsDeleted", FALSE) = FALSE
              AND COALESCE("IsEnabled", TRUE) = TRUE
            ORDER BY "Name";
            """;

        return await GetDropdownAsync(sql, cancellationToken);
    }

    public async Task<IReadOnlyList<DropdownDto>> GetDeviceTypeDropdownAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                "Id",
                "Name" AS "Value"
            FROM public."mst_device_type"
            WHERE COALESCE("IsDeleted", FALSE) = FALSE
              AND COALESCE("IsEnabled", TRUE) = TRUE
              AND COALESCE("IsActive", TRUE) = TRUE
            ORDER BY "Name";
            """;

        return await GetDropdownAsync(sql, cancellationToken);
    }

    private async Task<IReadOnlyList<DropdownDto>> GetDropdownAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var items = new List<DropdownDto>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new DropdownDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Value = reader.GetString(reader.GetOrdinal("Value"))
            });
        }

        return items;
    }

    private static DeviceModelResponseDto MapDeviceModel(NpgsqlDataReader reader)
    {
        return new DeviceModelResponseDto
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            ManufacturerId = reader.GetInt32(reader.GetOrdinal("ManufacturerId")),
            ManufacturerName = reader.GetString(reader.GetOrdinal("ManufacturerName")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            DeviceCategoryId = reader.GetInt32(reader.GetOrdinal("DeviceCategoryId")),
            DeviceCategoryName = reader.GetString(reader.GetOrdinal("DeviceCategoryName")),
            UseIMEIAsPrimaryId = reader.GetBoolean(reader.GetOrdinal("UseIMEIAsPrimaryId")),
            DeviceNo = GetNullableString(reader, "DeviceNo"),
            IMEISerialNumber = GetNullableString(reader, "IMEISerialNumber"),
            IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = GetNullableDateTime(reader, "UpdatedAt")
        };
    }

    private static string? GetNullableString(NpgsqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? GetNullableDateTime(NpgsqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static void AddNullableString(NpgsqlCommand command, string name, string? value)
    {
        command.Parameters.AddWithValue(name, (object?)value ?? DBNull.Value);
    }

    private static void AddNullableDateTime(NpgsqlCommand command, string name, DateTime? value)
    {
        command.Parameters.AddWithValue(name, (object?)value ?? DBNull.Value);
    }
}
