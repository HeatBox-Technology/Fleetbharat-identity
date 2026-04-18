using System;
using System.Threading.Tasks;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/device-transfer")]
public class DeviceTransferController : ControllerBase
{
    private readonly IDeviceTransferService _service;

    public DeviceTransferController(IDeviceTransferService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeviceTransferRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Transfer request created",
            200));
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(
        int id,
        [FromBody] UpdateDeviceTransferStatusRequest? request = null)
    {
        var result = await _service.ApproveAsync(id, request);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Transfer approved",
            200));
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] UpdateDeviceTransferStatusRequest? request = null)
    {
        var result = await _service.CancelAsync(id, request);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Transfer cancelled",
            200));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Transfer not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetTransfers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? accountId = null,
        [FromQuery] int? fromAccountId = null,
        [FromQuery] int? toAccountId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetTransfersAsync(
            page,
            pageSize,
            accountId,
            fromAccountId,
            toAccountId,
            status,
            fromDate,
            toDate,
            search);

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }
}
