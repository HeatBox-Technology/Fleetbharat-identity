using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;


/// <summary>
/// API controller for lookup data (OEMs, categories, providers, etc.).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LookupController : ControllerBase
{
    private readonly IdentityDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupController"/> class.
    /// </summary>
    public LookupController(IdentityDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all OEM manufacturers.
    /// </summary>
    /// <returns>List of OEM manufacturers (Id, Name).</returns>
    [HttpGet("oem-manufacturers")]
    public async Task<IActionResult> GetOemManufacturers()
    {
        var data = await _context.OemManufacturers.Select(x => new { x.Id, x.Name }).ToListAsync();
        return Ok(data);
    }

    /// <summary>
    /// Gets all device categories.
    /// </summary>
    /// <returns>List of device categories (Id, Name).</returns>
    [HttpGet("device-types")]
    public async Task<IActionResult> GetDeviceTypes()
    {
        var data = await _context.DeviceTypes.Select(x => new { x.Id, x.Name }).ToListAsync();
        return Ok(data);
    }

    /// <summary>
    /// Gets all network providers.
    /// </summary>
    /// <returns>List of network providers (Id, Name).</returns>
    [HttpGet("network-providers")]
    public async Task<IActionResult> GetNetworkProviders()
    {
        var data = await _context.NetworkProviders
            .Where(x => !x.IsDeleted && x.IsEnabled)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        return Ok(data);
    }

    /// <summary>
    /// Gets all vehicle brand OEMs.
    /// </summary>
    /// <returns>List of vehicle brand OEMs (Id, Name).</returns>
    [HttpGet("vehicle-brand-oems")]
    public async Task<IActionResult> GetVehicleBrandOems()
    {
        var data = await _context.VehicleBrandOems
            .Where(x => !x.IsDeleted && x.IsEnabled)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        return Ok(data);
    }

    /// <summary>
    /// Gets all leased vendors.
    /// </summary>
    /// <returns>List of leased vendors (Id, Name).</returns>
    [HttpGet("leased-vendors")]
    public async Task<IActionResult> GetLeasedVendors()
    {
        var data = await _context.LeasedVendors.Select(x => new { x.Id, x.Name }).ToListAsync();
        return Ok(data);
    }
}

