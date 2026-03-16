using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/bulk")]
public class BulkController : ControllerBase
{
    private readonly IBulkService _service;

    public BulkController(IBulkService service)
    {
        _service = service;
    }

    [HttpPost("upload/{module}")]
    public async Task<IActionResult> Upload(string module, IFormFile file)
    {
        var jobId = await _service.CreateJobAsync(module, file);

        return Ok(new { JobId = jobId });
    }

    [HttpPost("retry/{jobId}")]
    public async Task<IActionResult> Retry(int jobId)
    {
        await _service.RetryFailedAsync(jobId);

        return Ok("Retry started");
    }

    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> Status(int jobId)
    {
        var result = await _service.GetStatusAsync(jobId);

        return Ok(result);
    }
}