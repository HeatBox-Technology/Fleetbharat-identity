using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Controllers
{
    /// <summary>
    /// API controller for managing SIM cards.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SimController : ControllerBase
    {
        private readonly ISimService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimController"/> class.
        /// </summary>
        public SimController(ISimService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets a paged list of SIMs.
        /// </summary>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>Paged result of SIMs.</returns>
        /// <remarks>
        /// Sample response:
        /// {
        ///   "data": [ { /* SimDto fields */ } ],
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
        /// Gets a SIM by its unique identifier.
        /// </summary>
        /// <param name="id">SIM ID.</param>
        /// <returns>The SIM if found; otherwise, NotFound.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new SIM.
        /// </summary>
        /// <param name="dto">SIM DTO.</param>
        /// <returns>The created SIM.</returns>
        /// <remarks>Returns 201 Created with the new SIM.</remarks>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SimDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.SimId }, result);
        }

        /// <summary>
        /// Updates an existing SIM.
        /// </summary>
        /// <param name="id">SIM ID.</param>
        /// <param name="dto">SIM DTO.</param>
        /// <returns>The updated SIM.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] SimDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a SIM.
        /// </summary>
        /// <param name="id">SIM ID.</param>
        /// <returns>No content if deleted; otherwise, NotFound.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
        /// <summary>
        /// Bulk upload SIMs.
        /// </summary>
        /// <remarks>Uploads multiple SIMs at once.</remarks>
        /// <param name="sims">List of SIMs to upload.</param>
        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUpload([FromBody] IEnumerable<SimDto> sims)
        {
            var result = await _service.BulkCreateAsync(sims);
            return Ok(result);
        }
    }
}
