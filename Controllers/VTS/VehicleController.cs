using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Controllers
{
    /// <summary>
    /// API controller for managing vehicles.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleController"/> class.
        /// </summary>
        public VehicleController(IVehicleService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets all vehicles.
        /// </summary>
        /// <returns>List of vehicles.</returns>
        /// <summary>
        /// Gets a paged list of vehicles.
        /// </summary>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>Paged result of vehicles.</returns>
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Gets a vehicle by its unique identifier.
        /// </summary>
        /// <param name="id">Vehicle ID.</param>
        /// <returns>The vehicle if found; otherwise, NotFound.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new vehicle.
        /// </summary>
        /// <param name="dto">Vehicle DTO.</param>
        /// <returns>The created vehicle.</returns>
        /// <remarks>Returns 201 Created with the new vehicle.</remarks>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VehicleDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Updates an existing vehicle.
        /// </summary>
        /// <param name="id">Vehicle ID.</param>
        /// <param name="dto">Vehicle DTO.</param>
        /// <returns>The updated vehicle.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VehicleDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a vehicle.
        /// </summary>
        /// <param name="id">Vehicle ID.</param>
        /// <returns>No content if deleted; otherwise, NotFound.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
        /// <summary>
        /// Bulk upload vehicles.
        /// </summary>
        /// <remarks>Uploads multiple vehicles at once.</remarks>
        /// <param name="vehicles">List of vehicles to upload.</param>
        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUpload([FromBody] IEnumerable<VehicleDto> vehicles)
        {
            var result = await _service.BulkCreateAsync(vehicles);
            return Ok(result);
        }
    }
}
