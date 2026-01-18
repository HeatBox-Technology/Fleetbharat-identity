using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/addons")]
public class AddonsController : ControllerBase
{
    private readonly IAddonService _service;

    public AddonsController(IAddonService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddonMaster model)
    {
        var id = await _service.CreateAsync(model);
        return Ok(new { addonId = id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Ok(list);
    }
}
