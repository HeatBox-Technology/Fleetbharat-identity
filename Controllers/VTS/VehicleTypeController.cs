using Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;


/// <summary>
/// API controller for managing vehicle types.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VehicleTypeController : ControllerBase
{
    private readonly IVehicleTypeService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="VehicleTypeController"/> class.
    /// </summary>
    public VehicleTypeController(IVehicleTypeService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets all vehicle types.
    /// </summary>
    /// <returns>List of vehicle types.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? accountId = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetAllAsync(page, pageSize, accountId, search);
        return Ok(result);
    }

    /// <summary>
    /// Gets a vehicle type by its unique identifier.
    /// </summary>
    /// <param name="id">Vehicle type ID.</param>
    /// <returns>The vehicle type if found; otherwise, NotFound.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Creates a new vehicle type.
    /// </summary>
    /// <param name="dto">Vehicle type DTO.</param>
    /// <returns>The created vehicle type.</returns>
    /// <remarks>Returns 201 Created with the new vehicle type.</remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleTypeDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing vehicle type.
    /// </summary>
    /// <param name="id">Vehicle type ID.</param>
    /// <param name="dto">Vehicle type DTO.</param>
    /// <returns>The updated vehicle type.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] VehicleTypeDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpPost("{accountId:int}/{id:int}/icons")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadIcons(int accountId, int id, [FromForm] VehicleTypeIconUploadRequest req)
    {
        var result = await _service.UploadIconsAsync(accountId, id, req);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a vehicle type.
    /// </summary>
    /// <param name="id">Vehicle type ID.</param>
    /// <returns>No content if deleted; otherwise, NotFound.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}
