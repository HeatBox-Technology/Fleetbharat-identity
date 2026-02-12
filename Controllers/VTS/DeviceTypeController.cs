using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Controllers
{
    /// <summary>
    /// API controller for managing device types.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceTypeController : ControllerBase
    {
        private readonly IDeviceTypeService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceTypeController"/> class.
        /// </summary>
        public DeviceTypeController(IDeviceTypeService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets all device types.
        /// </summary>
        /// <returns>List of device types.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Gets a device type by its unique identifier.
        /// </summary>
        /// <param name="id">Device type ID.</param>
        /// <returns>The device type if found; otherwise, NotFound.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new device type.
        /// </summary>
        /// <param name="dto">Device type DTO.</param>
        /// <returns>The created device type.</returns>
        /// <remarks>Returns 201 Created with the new device type.</remarks>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DeviceTypeDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Updates an existing device type.
        /// </summary>
        /// <param name="id">Device type ID.</param>
        /// <param name="dto">Device type DTO.</param>
        /// <returns>The updated device type.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DeviceTypeDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a device type.
        /// </summary>
        /// <param name="id">Device type ID.</param>
        /// <returns>No content if deleted; otherwise, NotFound.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
