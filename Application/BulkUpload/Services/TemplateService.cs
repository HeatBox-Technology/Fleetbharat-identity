using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

public class TemplateService : ITemplateService
{
    private readonly IdentityDbContext _db;

    public TemplateService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> GenerateTemplateAsync(string moduleKey, string format, CancellationToken ct = default)
    {
        var config = await ResolveConfigAsync(moduleKey, ct);

        if (config == null)
            throw new KeyNotFoundException($"Bulk upload config not found for module '{moduleKey}'.");

        var columns = BulkUploadColumnDefinitionParser.Parse(config.ColumnsJson)
            .Where(x => x.IncludeInTemplate)
            .Where(x => !BulkUploadColumnDefinitionParser.IsSystemManagedFieldName(x.PropertyName))
            .ToList();

        if (IsGeofenceBulkModule(moduleKey))
            return GenerateGeofenceTemplate(format);

        if (columns.Count == 0)
            throw new InvalidDataException($"ColumnsJson is empty for module '{moduleKey}'.");

        if (string.Equals(format, "csv", System.StringComparison.OrdinalIgnoreCase))
        {
            var csv = string.Join(",", columns.Select(x => EscapeCsv(x.Header)));
            return (Encoding.UTF8.GetBytes(csv), "text/csv", $"{moduleKey}_template.csv");
        }

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Template");
        for (int i = 0; i < columns.Count; i++)
            ws.Cell(1, i + 1).Value = columns[i].Header;

        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{moduleKey}_template.xlsx");
    }

    private static (byte[] Content, string ContentType, string FileName) GenerateGeofenceTemplate(string format)
    {
        var headers = new[]
        {
            "AccountName",
            "UniqueCode",
            "DisplayName",
            "GeometryType",
            "Latitude",
            "Longitude",
            "GeoPoint",
            "RadiusM"
        };

        if (string.Equals(format, "csv", System.StringComparison.OrdinalIgnoreCase))
        {
            var csv = string.Join(",", headers.Select(EscapeCsv));
            return (Encoding.UTF8.GetBytes(csv), "text/csv", "geofence-master_template.csv");
        }

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Template");
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "geofence-master_template.xlsx");
    }

    private static string EscapeCsv(string value)
    {
        var v = value ?? "";
        if (v.Contains('"'))
            v = v.Replace("\"", "\"\"");

        if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
            return $"\"{v}\"";

        return v;
    }

    private static bool IsGeofenceBulkModule(string? moduleKey)
    {
        return string.Equals(moduleKey, "geofence-master", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(moduleKey, "geofence", System.StringComparison.OrdinalIgnoreCase);
    }

    private async Task<BulkUploadConfig?> ResolveConfigAsync(string moduleKey, CancellationToken ct)
    {
        var candidateKeys = GetConfigLookupKeys(moduleKey);

        return await _db.BulkUploadConfigs
            .AsNoTracking()
            .Where(x => x.IsActive && candidateKeys.Contains(x.ModuleKey))
            .OrderBy(x => x.ModuleKey == moduleKey ? 0 : 1)
            .FirstOrDefaultAsync(ct);
    }

    private static List<string> GetConfigLookupKeys(string moduleKey)
    {
        var keys = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { moduleKey };

        if (string.Equals(moduleKey, "geofence", System.StringComparison.OrdinalIgnoreCase))
            keys.Add("geofence-master");
        else if (string.Equals(moduleKey, "geofence-master", System.StringComparison.OrdinalIgnoreCase))
            keys.Add("geofence");

        return keys.ToList();
    }
}
