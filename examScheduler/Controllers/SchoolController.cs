using examScheduler.Services;
using Microsoft.AspNetCore.Mvc;

namespace examScheduler.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SchoolController(ISchoolService schoolService) : ControllerBase
{
	private readonly ISchoolService _schoolService = schoolService;

	[HttpGet]
	public async Task<IActionResult> Get(CancellationToken ct)
	{
		return Ok(await _schoolService.GetSchoolsAsync(ct));
	}
}
