using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Controllers
{
    /// <summary>
    /// API controller for managing sensor types.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SensorTypeController : ControllerBase
    {
        private readonly ISensorTypeService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorTypeController"/> class.
        /// </summary>
        public SensorTypeController(ISensorTypeService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets all sensor types.
        /// </summary>
        /// <returns>List of sensor types.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Gets a sensor type by its unique identifier.
        /// </summary>
        /// <param name="id">Sensor type ID.</param>
        /// <returns>The sensor type if found; otherwise, NotFound.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new sensor type.
        /// </summary>
        /// <param name="dto">Sensor type DTO.</param>
        /// <returns>The created sensor type.</returns>
        /// <remarks>Returns 201 Created with the new sensor type.</remarks>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SensorTypeDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.SensorTypeId }, result);
        }

        /// <summary>
        /// Updates an existing sensor type.
        /// </summary>
        /// <param name="id">Sensor type ID.</param>
        /// <param name="dto">Sensor type DTO.</param>
        /// <returns>The updated sensor type.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] SensorTypeDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a sensor type.
        /// </summary>
        /// <param name="id">Sensor type ID.</param>
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
