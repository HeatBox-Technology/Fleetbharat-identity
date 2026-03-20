using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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

        var headers = ParseHeaders(config.ColumnsJson);
        if (headers.Count == 0)
            throw new InvalidDataException($"ColumnsJson is empty for module '{moduleKey}'.");

        if (string.Equals(format, "csv", System.StringComparison.OrdinalIgnoreCase))
        {
            var csv = string.Join(",", headers.Select(EscapeCsv));
            return (Encoding.UTF8.GetBytes(csv), "text/csv", $"{moduleKey}_template.csv");
        }

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Template");
        for (int i = 0; i < headers.Count; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{moduleKey}_template.xlsx");
    }

    private static List<string> ParseHeaders(string columnsJson)
    {
        var results = new List<string>();

        if (string.IsNullOrWhiteSpace(columnsJson))
            return results;

        using var doc = JsonDocument.Parse(columnsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return results;

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var text = item.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    results.Add(text.Trim());
                continue;
            }

            if (item.ValueKind == JsonValueKind.Object)
            {
                if (item.TryGetProperty("header", out var headerProp))
                {
                    var header = headerProp.GetString();
                    if (!string.IsNullOrWhiteSpace(header))
                        results.Add(header.Trim());
                    continue;
                }

                if (item.TryGetProperty("Header", out var headerProp2))
                {
                    var header = headerProp2.GetString();
                    if (!string.IsNullOrWhiteSpace(header))
                        results.Add(header.Trim());
                }
            }
        }

        return results;
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
