using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/modules")]
public class ModulesController : ControllerBase
{
    private readonly IHierarchyService _service;

    public ModulesController(IHierarchyService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetBySolution([FromQuery] int solutionId)
    {
        var data = await _service.GetModulesBySolutionAsync(solutionId);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }
}

