using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Controllers
{
    /// <summary>
    /// API controller for managing device-SIM mappings (historical mapping).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceSimMapController : ControllerBase
    {
        private readonly IDeviceSimMapService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceSimMapController"/> class.
        /// </summary>
        public DeviceSimMapController(IDeviceSimMapService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets all device-SIM mappings.
        /// </summary>
        /// <returns>List of device-SIM mappings.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Gets a device-SIM mapping by its unique identifier.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>The device-SIM mapping if found; otherwise, NotFound.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new device-SIM mapping.
        /// </summary>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The created mapping.</returns>
        /// <remarks>Returns 201 Created with the new mapping.</remarks>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DeviceSimMapDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.DeviceSimId }, result);
        }

        /// <summary>
        /// Updates an existing device-SIM mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The updated mapping.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] DeviceSimMapDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a device-SIM mapping.
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
