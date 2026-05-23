using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.API;
using System.Net;
using Util.Extensions;

namespace examScheduler.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = RoleNames.Teacher)]
public class CalendarController(ICalendarService calendarService) : ControllerBase
{
	private readonly ICalendarService _calendarService = calendarService;

	[HttpGet("{classroomId:guid}/{millisSinceUnixEpoch:long}")]
	public async Task<Result<IEnumerable<Lesson>>> GetWeek(Guid classroomId, long millisSinceUnixEpoch)
	{
		DateTimeOffset date;
		try
		{
			date = DateTimeOffset.UnixEpoch.AddMilliseconds(millisSinceUnixEpoch);
		}
		catch (ArgumentException)
		{
			return new(HttpStatusCode.BadRequest);
		}

		if (!User.TryGetId(out var userId))
		{
			return new(HttpStatusCode.BadRequest);
		}

		return new(
			( await _calendarService.TryGetWeekContaintingDateAsync(userId, classroomId, date) )
				?.Select(x => x.ToDTO()),
			HttpStatusCode.BadRequest,
			x => x is not null
		);
	}
}
