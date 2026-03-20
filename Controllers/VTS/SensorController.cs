using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Controllers
{
    /// <summary>
    /// API controller for managing sensors.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SensorController : ControllerBase
    {
        private readonly ISensorService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorController"/> class.
        /// </summary>
        public SensorController(ISensorService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets all sensors.
        /// </summary>
        /// <returns>List of sensors.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Gets a sensor by its unique identifier.
        /// </summary>
        /// <param name="id">Sensor ID.</param>
        /// <returns>The sensor if found; otherwise, NotFound.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new sensor.
        /// </summary>
        /// <param name="dto">Sensor DTO.</param>
        /// <returns>The created sensor.</returns>
        /// <remarks>Returns 201 Created with the new sensor.</remarks>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SensorDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.SensorId }, result);
        }

        /// <summary>
        /// Updates an existing sensor.
        /// </summary>
        /// <param name="id">Sensor ID.</param>
        /// <param name="dto">Sensor DTO.</param>
        /// <returns>The updated sensor.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] SensorDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a sensor.
        /// </summary>
        /// <param name="id">Sensor ID.</param>
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
