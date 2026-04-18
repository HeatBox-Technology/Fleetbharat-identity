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
        var config = await _db.BulkUploadConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ModuleKey == moduleKey && x.IsActive, ct);

        if (config == null)
            throw new KeyNotFoundException($"Bulk upload config not found for module '{moduleKey}'.");

        var columns = BulkUploadColumnDefinitionParser.Parse(config.ColumnsJson)
            .Where(x => x.IncludeInTemplate)
            .Where(x => !BulkUploadColumnDefinitionParser.IsSystemManagedFieldName(x.PropertyName))
            .ToList();

        if (string.Equals(moduleKey, "geofence-master", System.StringComparison.OrdinalIgnoreCase))
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
}
