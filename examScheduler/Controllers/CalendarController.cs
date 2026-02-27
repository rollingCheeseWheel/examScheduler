using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.API;
using System.Net;

namespace examScheduler.Controllers;
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = RoleNames.Teacher)]
public class CalendarController(ICalendarService calendarService) : ControllerBase
{
	private readonly ICalendarService _calendarService = calendarService;

	[HttpGet("{classroomId:guid}/{daysSinceUnixEpoch:long}")]
	public async Task<Result<IEnumerable<Lesson>>> GetWeek(Guid classroomId, long daysSinceUnixEpoch)
	{
		return new(
			( await _calendarService.TryGetWeekContaintingDateAsync(classroomId, DateTimeOffset.UnixEpoch.AddDays(daysSinceUnixEpoch)) )
				?.Select(x => x.ToDTO()),
			HttpStatusCode.BadRequest,
			x => x is not null
		);
	}
}
