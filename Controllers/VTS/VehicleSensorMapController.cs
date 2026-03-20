using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Controllers
{
    /// <summary>
    /// API controller for mapping vehicles to sensors (mount points).
    /// Example: POST { "vehicleId": 101, "sensorId": 5, "mountPoint": "tank1" }
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleSensorMapController : ControllerBase
    {
        private readonly IVehicleSensorMapService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleSensorMapController"/> class.
        /// </summary>
        public VehicleSensorMapController(IVehicleSensorMapService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets all vehicle-sensor mappings.
        /// </summary>
        /// <returns>List of vehicle-sensor mappings.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Gets a vehicle-sensor mapping by its unique identifier.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>The vehicle-sensor mapping if found; otherwise, NotFound.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new vehicle-sensor mapping.
        /// </summary>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The created mapping.</returns>
        /// <remarks>Returns 201 Created with the new mapping.</remarks>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VehicleSensorMapDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.VehicleSensorId }, result);
        }

        /// <summary>
        /// Updates an existing vehicle-sensor mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The updated mapping.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] VehicleSensorMapDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a vehicle-sensor mapping.
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
