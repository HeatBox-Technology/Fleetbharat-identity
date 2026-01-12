
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/accounts")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _service;

    public AccountController(IAccountService service)
    {
        _service = service;
    }

    // POST
    [HttpPost]
    public async Task<IActionResult> Create(mst_account account)
    {
        var result = await _service.CreateAsync(account);
        return Ok(result);
    }

    // GET ALL
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    // GET BY ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var account = await _service.GetByIdAsync(id);
        if (account == null) return NotFound();
        return Ok(account);
    }

    // PUT (FULL UPDATE)
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, mst_account account)
    {
        await _service.UpdateAsync(id, account);
        return NoContent();
    }

    // PATCH (STATUS UPDATE)
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
    {
        await _service.UpdateStatusAsync(id, status);
        return NoContent();
    }

    // DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
