using examScheduler.Services;
using Microsoft.AspNetCore.Mvc;

namespace examScheduler.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SchoolsController(ISchoolsService schoolService) : ControllerBase
{
    private readonly ISchoolsService _schoolService = schoolService;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        return Ok(await _schoolService.GetSchoolsAsync(ct));
    }
}
