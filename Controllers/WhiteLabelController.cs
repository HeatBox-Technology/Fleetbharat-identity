using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/white-labels")]
public class WhiteLabelController : ControllerBase
{
    private readonly IWhiteLabelService _service;

    public WhiteLabelController(IWhiteLabelService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWhiteLabelRequest req)
    {
        var id = await _service.CreateAsync(req);
        return Ok(ApiResponse<object>.Ok(new { whiteLabelId = id }, "WhiteLabel provisioned", 200));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var data = await _service.GetAllAsync(page, pageSize, search, isActive);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound(ApiResponse<object>.Fail("WhiteLabel not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("by-account/{accountId}")]
    public async Task<IActionResult> GetByAccountId(int accountId)
    {
        var data = await _service.GetByAccountIdAsync(accountId);
        if (data == null)
            return NotFound(ApiResponse<object>.Fail("WhiteLabel not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateWhiteLabelRequest req)
    {
        var ok = await _service.UpdateAsync(id, req);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail("WhiteLabel not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "WhiteLabel updated", 200));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail("WhiteLabel not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "WhiteLabel deleted", 200));
    }

    [HttpPost("{accountId}/logo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadLogo(int accountId, IFormFile file)
    {
        if (file == null)
            return BadRequest(ApiResponse<object>.Fail("File is required", 400));

        var data = await _service.UploadLogoAsync(accountId, file);
        return Ok(ApiResponse<object>.Ok(data, "Logo uploaded", 200));
    }

    [HttpPost("logos")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadLogos([FromForm] WhiteLabelLogoUploadRequest req)
    {
        var data = await _service.UploadLogosAsync(req);
        return Ok(ApiResponse<object>.Ok(data, "Logos uploaded", 200));
    }
}
