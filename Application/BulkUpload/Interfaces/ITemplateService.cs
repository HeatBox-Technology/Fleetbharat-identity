using System.Threading;
using System.Threading.Tasks;

public interface ITemplateService
{
    Task<(byte[] Content, string ContentType, string FileName)> GenerateTemplateAsync(string moduleKey, string format, CancellationToken ct = default);
}
