using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/features")]
public class FeaturesController : ControllerBase
{
    private readonly IFeatureService _service;

    public FeaturesController(IFeatureService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FeatureMaster model)
    {
        var id = await _service.CreateAsync(model);
        return Ok(new { featureId = id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Ok(list);
    }
}
