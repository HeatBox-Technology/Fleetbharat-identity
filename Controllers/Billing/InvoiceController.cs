using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/billing/invoices")]
public class InvoiceController : ControllerBase
{
    private readonly IBillingInvoiceService _service;

    public InvoiceController(IBillingInvoiceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var data = await _service.GetInvoicesAsync(skip, take, ct);
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("{accountId:int}")]
    public async Task<IActionResult> GetByAccount(int accountId, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var data = await _service.GetInvoicesByAccountAsync(accountId, skip, take, ct);
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpPost("manual")]
    public async Task<IActionResult> CreateManual([FromBody] InvoiceManualCreateDto dto, CancellationToken ct = default)
    {
        var data = await _service.CreateManualInvoiceAsync(dto, ct);
        return Ok(ApiResponse<object>.Ok(data, "Invoice created", 200));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var ok = await _service.DeleteInvoiceAsync(id, ct);
        if (!ok)
        {
            return NotFound(ApiResponse<object>.Fail("Invoice not found", 404));
        }

        return Ok(ApiResponse<object>.Ok(null, "Invoice deleted", 200));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] int skip = 0, [FromQuery] int take = 500, CancellationToken ct = default)
    {
        var csv = await _service.ExportInvoicesCsvAsync(skip, take, ct);
        return Ok(ApiResponse<object>.Ok(new { contentType = "text/csv", csv }, "Export generated", 200));
    }
}
