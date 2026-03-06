using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/bulk-upload")]
public class BulkUploadController : ControllerBase
{
    private readonly IBulkUploadService _bulkUploadService;

    public BulkUploadController(IBulkUploadService bulkUploadService)
    {
        _bulkUploadService = bulkUploadService;
    }

    [HttpPost("{moduleKey}")]
    [RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> Upload(string moduleKey, IFormFile file, CancellationToken ct)
    {
        var result = await _bulkUploadService.EnqueueUploadAsync(moduleKey, file, ct);
        return Accepted(result);
    }

    [HttpGet("status/{jobId:int}")]
    public async Task<IActionResult> Status(int jobId, CancellationToken ct)
    {
        var status = await _bulkUploadService.GetStatusAsync(jobId, ct);
        if (status == null)
            return NotFound();

        return Ok(status);
    }

    [HttpGet("template/{moduleKey}")]
    public async Task<IActionResult> Template(string moduleKey, [FromQuery] string format = "excel", CancellationToken ct = default)
    {
        var (content, contentType, fileName) = await _bulkUploadService.GetTemplateAsync(moduleKey, format, ct);
        return File(content, contentType, fileName);
    }

    [HttpGet("error-report/{jobId:int}")]
    public async Task<IActionResult> ErrorReport(int jobId, CancellationToken ct)
    {
        var report = await _bulkUploadService.GetErrorReportAsync(jobId, ct);
        if (report == null)
            return NotFound("No error report found for this job.");

        return File(report.Value.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", report.Value.FileName);
    }
}
