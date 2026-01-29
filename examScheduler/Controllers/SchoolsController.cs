using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.Mvc;

namespace examScheduler.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SchoolsController(ISchoolsService schoolService) : ControllerBase
{
	private readonly ISchoolsService _schoolService = schoolService;

	[HttpGet]
	public async Task<IActionResult> Get(CancellationToken ct) => Ok(
		( await _schoolService.GetSchoolsAsync_AsNoTracking(ct) )
		.Select(s => s.ToDTO())
	);
}
