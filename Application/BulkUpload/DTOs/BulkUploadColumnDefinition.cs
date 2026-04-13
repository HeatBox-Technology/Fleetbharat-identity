using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public class BulkUploadColumnDefinition
{
    public string Header { get; set; } = "";
    public string PropertyName { get; set; } = "";
    public string? LookupType { get; set; }
    public bool Required { get; set; }
    public bool Unique { get; set; }
    public List<string> UniqueWith { get; set; } = new();
    public int? MaxLength { get; set; }
    public int? MinLength { get; set; }
    public string? Regex { get; set; }
    public List<string> AllowedValues { get; set; } = new();
    public bool IncludeInTemplate { get; set; } = true;
    public List<string> Aliases { get; set; } = new();
}

public static class BulkUploadColumnDefinitionParser
{
    public static List<BulkUploadColumnDefinition> Parse(string? columnsJson)
    {
        var results = new List<BulkUploadColumnDefinition>();

        if (string.IsNullOrWhiteSpace(columnsJson))
            return results;

        using var doc = JsonDocument.Parse(columnsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return results;

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var rawPropertyName = item.GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(rawPropertyName))
                    continue;

                results.Add(CreateDefault(NormalizePropertyName(rawPropertyName)));
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var propertyName =
                GetString(item, "property") ??
                GetString(item, "propertyName") ??
                GetString(item, "field") ??
                GetString(item, "dtoProperty") ??
                GetString(item, "key") ??
                GetString(item, "name") ??
                GetString(item, "header") ??
                GetString(item, "Header");

            if (string.IsNullOrWhiteSpace(propertyName))
                continue;

            propertyName = NormalizePropertyName(propertyName);

            var definition = CreateDefault(propertyName);

            definition.Header =
                GetString(item, "header") ??
                GetString(item, "Header") ??
                definition.Header;

            definition.LookupType =
                GetString(item, "lookup") ??
                GetString(item, "lookupType") ??
                InferLookupType(propertyName);

            definition.Required =
                GetBool(item, "required") ??
                GetBool(item, "isRequired") ??
                false;

            definition.Unique =
                GetBool(item, "unique") ??
                false;

            definition.UniqueWith = GetStringList(item, "uniqueWith");

            definition.MaxLength = GetInt(item, "maxLength");
            definition.MinLength = GetInt(item, "minLength");
            definition.Regex = GetString(item, "regex");
            definition.AllowedValues = GetStringList(item, "allowedValues");

            definition.IncludeInTemplate =
                GetBool(item, "includeInTemplate") ??
                GetBool(item, "visible") ??
                definition.IncludeInTemplate;

            if (item.TryGetProperty("aliases", out var aliasesProp) && aliasesProp.ValueKind == JsonValueKind.Array)
            {
                definition.Aliases = aliasesProp
                    .EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString()?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            results.Add(definition);
        }

        return results;
    }

    private static BulkUploadColumnDefinition CreateDefault(string propertyName)
    {
        var header = GetFriendlyHeader(propertyName);
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            propertyName,
            header
        };

        foreach (var alias in GetDefaultAliases(propertyName))
            aliases.Add(alias);

        return new BulkUploadColumnDefinition
        {
            PropertyName = propertyName,
            Header = header,
            LookupType = InferLookupType(propertyName),
            IncludeInTemplate = !IsSystemManagedFieldName(propertyName),
            Aliases = aliases.Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
        };
    }

    public static string GetFriendlyHeader(string propertyName)
    {
        return propertyName switch
        {
            "AccountId" => "Account Name",
            "VehicleTypeId" => "Vehicle Type Name",
            "DeviceTypeId" => "Device Type Name",
            "ManufacturerId" => "Manufacturer Name",
            "GeofenceId" => "Geofence Name",
            "CreatedBy" => "Created By",
            "UpdatedBy" => "Updated By",
            _ => SplitPascalCase(propertyName)
        };
    }

    public static string NormalizePropertyName(string propertyName)
    {
        return propertyName switch
        {
            "AccountName" => "AccountId",
            "VehicleTypeName" => "VehicleTypeId",
            "DeviceTypeName" => "DeviceTypeId",
            "ManufacturerName" => "ManufacturerId",
            "GeofenceName" => "GeofenceId",
            _ => propertyName
        };
    }

    public static string? InferLookupType(string propertyName)
    {
        return propertyName switch
        {
            "AccountId" => "account",
            "VehicleTypeId" => "vehicleType",
            "DeviceTypeId" => "deviceType",
            "ManufacturerId" => "manufacturer",
            "GeofenceId" => "geofence",
            _ => null
        };
    }

    private static IEnumerable<string> GetDefaultAliases(string propertyName)
    {
        if (propertyName.Equals("AccountId", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Account";
            yield return "AccountName";
            yield return "Account Name";
        }

        if (propertyName.Equals("VehicleTypeId", StringComparison.OrdinalIgnoreCase))
        {
            yield return "VehicleType";
            yield return "VehicleTypeName";
            yield return "Vehicle Type";
            yield return "Vehicle Type Name";
        }

        if (propertyName.Equals("DeviceTypeId", StringComparison.OrdinalIgnoreCase))
        {
            yield return "DeviceType";
            yield return "DeviceTypeName";
            yield return "Device Type";
            yield return "Device Type Name";
        }

        if (propertyName.Equals("ManufacturerId", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Manufacturer";
            yield return "ManufacturerName";
            yield return "Manufacturer Name";
            yield return "OEM Manufacturer";
        }

        if (propertyName.Equals("GeofenceId", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Geofence";
            yield return "GeofenceName";
            yield return "Geofence Name";
            yield return "Zone Name";
        }
    }

    public static bool IsSystemManagedFieldName(string propertyName)
    {
        return propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("CreatedBy", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("UpdatedBy", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("DeletedBy", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("DeletedAt", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("Status", StringComparison.OrdinalIgnoreCase);
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var chars = new List<char>(value.Length * 2);
        for (int i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (i > 0 && char.IsUpper(c) && !char.IsWhiteSpace(value[i - 1]))
                chars.Add(' ');

            chars.Add(c);
        }

        return new string(chars.ToArray()).Trim();
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return null;

        var value = property.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool? GetBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        if (property.ValueKind == JsonValueKind.True)
            return true;

        if (property.ValueKind == JsonValueKind.False)
            return false;

        if (property.ValueKind == JsonValueKind.String &&
            bool.TryParse(property.GetString(), out var parsed))
            return parsed;

        return null;
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
            return number;

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(property.GetString(), out var parsed))
            return parsed;

        return null;
    }

    private static List<string> GetStringList(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            return new List<string>();

        return property
            .EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString()?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
