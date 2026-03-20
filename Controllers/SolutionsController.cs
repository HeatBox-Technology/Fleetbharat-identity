using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/solutions")]
public class SolutionsController : ControllerBase
{
    private readonly IHierarchyService _service;

    public SolutionsController(IHierarchyService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetSolutionsAsync();
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }
}

