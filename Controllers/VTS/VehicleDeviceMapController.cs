using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Controllers
{
    /// <summary>
    /// API controller for managing vehicle-device mappings (historical mapping).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleDeviceMapController : ControllerBase
    {
        private readonly IVehicleDeviceMapService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleDeviceMapController"/> class.
        /// </summary>
        public VehicleDeviceMapController(IVehicleDeviceMapService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets all vehicle-device mappings.
        /// </summary>
        /// <returns>List of vehicle-device mappings.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Gets a vehicle-device mapping by its unique identifier.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>The vehicle-device mapping if found; otherwise, NotFound.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new vehicle-device mapping.
        /// </summary>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The created mapping.</returns>
        /// <remarks>Returns 201 Created with the new mapping.</remarks>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VehicleDeviceMapDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.VehicleDeviceId }, result);
        }

        /// <summary>
        /// Updates an existing vehicle-device mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The updated mapping.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] VehicleDeviceMapDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a vehicle-device mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>No content if deleted; otherwise, NotFound.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
