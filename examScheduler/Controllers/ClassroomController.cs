using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.API;
using Util.Extensions;

namespace examScheduler.Controllers;

[Route("api/[controller]")]
[Authorize]
public class ClassroomController(IClassroomService classroomService) : ControllerBase
{
	private readonly IClassroomService _classroomService = classroomService;

	[HttpGet("/all")]
	public async Task<Result<IEnumerable<Classroom>>> GetClassrooms(CancellationToken ct) => User.TryGetId(out var id)
			? new(( await _classroomService.GetClassroomsForUserAsync(id, ct) )
				.Select(x => x.ToDTO())
			)
			: new([ ]);
}
