using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/common/dropdowns")]
public class CommonDropdownController : ControllerBase
{
    private readonly ICommonDropdownService _service;

    public CommonDropdownController(ICommonDropdownService service)
    {
        _service = service;
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts([FromQuery] string? search = null, [FromQuery] int limit = 20)
    {
        var data = await _service.GetAccountsAsync(search, limit);
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }

    [HttpGet("accounts/filter")]
    public async Task<IActionResult> AccountDropdown(
        [FromQuery] int? accountId = null,
        [FromQuery] int? categoryId = null)
    {
        var data = await _service.GetAccountDropdownAsync(accountId, categoryId);
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories([FromQuery] string? search = null, [FromQuery] int limit = 20)
    {
        var data = await _service.GetCategoriesAsync(search, limit);
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }

    [HttpGet("roles")]
    public async Task<IActionResult> Roles(
        [FromQuery] int accountId,
        [FromQuery] string? search = null,
        [FromQuery] int limit = 20)
    {
        var data = await _service.GetRolesAsync(accountId, search, limit);
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(
        [FromQuery] int accountId,
        [FromQuery] string? search = null,
        [FromQuery] int limit = 20)
    {
        var data = await _service.GetUsersAsync(accountId, search, limit);
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }

    [HttpGet("drivers")]
    public async Task<IActionResult> Drivers(
        [FromQuery] int? driverId = null,
        [FromQuery] int? accountId = null,
        [FromQuery] string? name = null,
        [FromQuery] string? mobile = null)
    {
        var data = await _service.GetDriversDropdownAsync(driverId, accountId, name, mobile);
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }

    [HttpGet("vehicle-types")]
    public async Task<IActionResult> VehicleTypes([FromQuery] int? id = null)
    {
        var data = await _service.GetVehicleTypesAsync(id);
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }

    [HttpGet("Currency")]
    public async Task<IActionResult> GetCurrency()
    {
        var result = await _service.GetCurrencyDropdownAsync();
        return Ok(ApiResponse<object>.Ok(result, "Success"));
    }
    [HttpGet("form-modules/dropdown")]
    public async Task<IActionResult> GetFormModulesDropdown()
    {
        var data = await _service.GetFormModuleDropdownAsync();
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }
    [HttpGet("vehicles/{accountId}")]
    public async Task<IActionResult> GetVehicles(int accountId)
    {
        var data = await _service.GetVehicles(accountId);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("devices/{accountId}")]
    public async Task<IActionResult> GetDevices(int accountId)
    {
        var data = await _service.GetDevices(accountId);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("sims/{accountId}")]
    public async Task<IActionResult> GetSims(int accountId)
    {
        var data = await _service.GetSims(accountId);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("device-types")]
    public async Task<IActionResult> GetDeviceTypes()
    {
        var data = await _service.GetDeviceType();
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }
    [HttpGet("trip-types")]
    public async Task<IActionResult> GetTripTypes()
    {
        var data = await _service.GetTripTypes();
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }
    [HttpGet("geofences")]
    public async Task<IActionResult> GetGeofences(
        [FromQuery] int accountId,
        [FromQuery] string? search = null,
        [FromQuery] int limit = 20)
    {
        var data = await _service.GetGeofences(accountId, search, limit);

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }
    [HttpGet("manufacturers")]
    public async Task<IActionResult> GetManufacturers()
    {
        var data = await _service.GetManufacture();
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("~/api/common/filter-config")]
    public async Task<IActionResult> GetFilterConfig([FromQuery] string formName)
    {
        if (string.IsNullOrWhiteSpace(formName))
            return BadRequest(ApiResponse<object>.Fail("formName is required", 400));

        var data = await _service.GetFilterConfigByFormNameAsync(formName);
        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Filter config not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }


}
