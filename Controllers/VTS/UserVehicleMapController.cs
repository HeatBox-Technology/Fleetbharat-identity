using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Controllers
{
    /// <summary>
    /// API controller for managing user-vehicle mappings (vehicle-wise login/filtering).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserVehicleMapController : ControllerBase
    {
        private readonly IUserVehicleMapService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserVehicleMapController"/> class.
        /// </summary>
        public UserVehicleMapController(IUserVehicleMapService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets all user-vehicle mappings.
        /// </summary>
        /// <returns>List of user-vehicle mappings.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Gets a user-vehicle mapping by its unique identifier.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>The user-vehicle mapping if found; otherwise, NotFound.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new user-vehicle mapping.
        /// </summary>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The created mapping.</returns>
        /// <remarks>Returns 201 Created with the new mapping.</remarks>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserVehicleMapDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.UserVehicleId }, result);
        }

        /// <summary>
        /// Updates an existing user-vehicle mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The updated mapping.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UserVehicleMapDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a user-vehicle mapping.
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
