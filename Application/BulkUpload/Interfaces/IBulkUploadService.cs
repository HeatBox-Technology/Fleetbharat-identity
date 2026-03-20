using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IBulkUploadService
{
    Task<BulkUploadStartResultDto> EnqueueUploadAsync(string moduleKey, IFormFile file, CancellationToken ct = default);
    Task<BulkUploadStatusDto?> GetStatusAsync(int jobId, CancellationToken ct = default);
    Task<(byte[] Content, string ContentType, string FileName)> GetTemplateAsync(string moduleKey, string format, CancellationToken ct = default);
    Task<(byte[] Content, string FileName)?> GetErrorReportAsync(int jobId, CancellationToken ct = default);
}
