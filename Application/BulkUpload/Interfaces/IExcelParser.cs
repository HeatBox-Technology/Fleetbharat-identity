using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public interface IExcelParser
{
    Task<List<Dictionary<string, string>>> ParseAsync(Stream stream, CancellationToken ct = default);
}
