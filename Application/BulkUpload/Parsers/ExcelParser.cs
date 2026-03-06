using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;

public class ExcelParser : IExcelParser
{
    public Task<List<Dictionary<string, string>>> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        var rows = new List<Dictionary<string, string>>();

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.Worksheet(1);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        if (lastRow <= 1 || lastCol == 0)
            return Task.FromResult(rows);

        var headers = new List<string>();
        for (int col = 1; col <= lastCol; col++)
            headers.Add((worksheet.Cell(1, col).GetString() ?? "").Trim());

        for (int row = 2; row <= lastRow; row++)
        {
            ct.ThrowIfCancellationRequested();

            var data = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            bool hasAnyValue = false;

            for (int col = 1; col <= lastCol; col++)
            {
                var header = headers[col - 1];
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                var value = (worksheet.Cell(row, col).GetString() ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    hasAnyValue = true;

                data[header] = value;
            }

            if (hasAnyValue)
                rows.Add(data);
        }

        return Task.FromResult(rows);
    }
}
