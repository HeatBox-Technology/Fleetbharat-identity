using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

public class CsvParser : ICsvParser
{
    public Task<List<Dictionary<string, string>>> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        var rows = new List<Dictionary<string, string>>();

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,
            MissingFieldFound = null
        });

        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? new string[0];

        while (csv.Read())
        {
            ct.ThrowIfCancellationRequested();

            var row = headers.ToDictionary(
                h => h,
                h => (csv.GetField(h) ?? "").Trim(),
                System.StringComparer.OrdinalIgnoreCase);

            if (row.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                rows.Add(row);
        }

        return Task.FromResult(rows);
    }
}
