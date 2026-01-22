using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.API;
using Util;

namespace examScheduler.Controllers;

[Route("api/[controller]")]
[Authorize]
public class ClassroomsController(IClassroomService classroomService) : ControllerBase
{
	private readonly IClassroomService _classroomService = classroomService;

	[HttpGet]
	public async Task<Result<IEnumerable<Classroom>>> GetClassrooms(CancellationToken ct)
	{
		if (User.TryGetId(out var id))
		{
			return new(( await _classroomService.GetClassroomsForUserAsync(id, ct) )
				.Select(x => x.ToDTO())
			);
		}
		return new([ ]);
	}
}
