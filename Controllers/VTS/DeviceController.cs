using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Controllers
{
    /// <summary>
    /// API controller for managing devices.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceController"/> class.
        /// </summary>
        public DeviceController(IDeviceService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets a paged list of devices.
        /// </summary>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>Paged result of devices.</returns>
        /// <remarks>
        /// Sample response:
        /// {
        ///   "data": [ { /* DeviceDto fields */ } ],
        ///   "totalCount": 100,
        ///   "page": 1,
        ///   "pageSize": 10
        /// }
        /// </remarks>
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Gets a device by its unique identifier.
        /// </summary>
        /// <param name="id">Device ID.</param>
        /// <returns>The device if found; otherwise, NotFound.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new device.
        /// </summary>
        /// <param name="dto">Device DTO.</param>
        /// <returns>The created device.</returns>
        /// <remarks>Returns 201 Created with the new device.</remarks>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DeviceDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Updates an existing device.
        /// </summary>
        /// <param name="id">Device ID.</param>
        /// <param name="dto">Device DTO.</param>
        /// <returns>The updated device.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DeviceDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a device.
        /// </summary>
        /// <param name="id">Device ID.</param>
        /// <returns>No content if deleted; otherwise, NotFound.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
        /// <summary>
        /// Bulk upload devices.
        /// </summary>
        /// <remarks>Uploads multiple devices at once.</remarks>
        /// <param name="devices">List of devices to upload.</param>
        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUpload([FromBody] IEnumerable<DeviceDto> devices)
        {
            var result = await _service.BulkCreateAsync(devices);
            return Ok(result);
        }
    }
}
