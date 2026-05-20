using examScheduler.BackgroundServices;
using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Models.API;
using System.Net;
using System.Security.Claims;
using Util;
using Util.Extensions;

namespace examScheduler.Hubs;

/*
	1.	Swap requests target Slots instead of students
	2.	If two students from different Slots want to swap with each others Slots, the swap should resolve instantly	
 */

public interface IScheduleHub
{
	Task<Result<bool>> RegisterForSlot(Guid slotId);

	Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid examSlotId);
	Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId);
	Task<Result<bool>> DeleteSwapRequest(Guid swaprequestId);

	Task<Result<bool>> CreateSchedule(ScheduleCreateRequest request);
	Task<Result<bool>> DeleteSchedule(Guid scheduleId);
	Task<Result<bool>> ReportStudents(Guid scheduleSlotId, IEnumerable<Guid> actualParticipants);
}

public interface IScheduleClient
{
	Task ReceiveInitialSchedules(IEnumerable<Schedule> schedules);
	Task UpdateSchedule(Guid scheduleId, Schedule schedule);
	Task RemoveSchedule(Guid scheduleId);

	Task ReceiveInitialClassrooms(IEnumerable<Classroom> classrooms);
	Task UpdateClassroom(Classroom classroom);
}

[Authorize]
public class ScheduleHub(
	IScheduleService scheduleService,
	IClassroomService classroomService,
	ILogger<ScheduleHub> logger
) : Hub<IScheduleClient>, IScheduleHub
{
	private readonly IScheduleService _scheduleService = scheduleService;
	private readonly IClassroomService _classroomService = classroomService;
	private readonly ILogger _logger = logger;

	private Guid? _guid;

	public override async Task OnConnectedAsync()
	{
		var claimsPrincipal = Context.User;
		var stringedUserId = claimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);
		if (claimsPrincipal is null ||
			claimsPrincipal.Identity?.IsAuthenticated is null ||
			!claimsPrincipal.Identity.IsAuthenticated)
		{
			return;
		}

		if (!Guid.TryParse(stringedUserId, out var userId))
		{
			return;
		}
		_guid = userId;

		var schedules = await _scheduleService.GetSchedulesForUserAsync_AsNoTracking(userId, Context.ConnectionAborted);
		var classrooms = await _classroomService.GetClassroomsForUserAsync_AsNoTracking(userId, Context.ConnectionAborted);

		foreach (var schedule in schedules)
		{
			await this.AddToScheduleGroupAsync(schedule.Id, Context.ConnectionAborted);
		}

		foreach (var classroom in classrooms)
		{
			await this.AddToClassroomGroupAsync(classroom.Id);
		}

		await Clients.Caller.ReceiveInitialSchedules(schedules.Select(x => x.ToDTO()));
		await Clients.Caller.ReceiveInitialClassrooms(classrooms.Select(x => x.ToDTO()));

		await base.OnConnectedAsync();
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> RegisterForSlot(Guid slotId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryEnlistStudentAsync(slotId, _guid.Value, Context.ConnectionAborted);
		return new(result, HttpStatusCode.BadRequest, result);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid examSlotId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryCreateSwapRequestAsync(scheduleId, _guid.Value, examSlotId, Context.ConnectionAborted);
		return new(result, HttpStatusCode.BadRequest, result);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> DeleteSwapRequest(Guid swapRequestId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryDeleteSwapRequestAsync(swapRequestId, _guid.Value, Context.ConnectionAborted);
		return new(result, HttpStatusCode.BadRequest, result);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryAcceptSwapRequestAsync(swapRequestId, _guid.Value, Context.ConnectionAborted);
		return new(result, HttpStatusCode.BadRequest, result);
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> CreateSchedule(ScheduleCreateRequest request)
	{
		_logger.LogInformation("schedule create request {@request}", request.Stringify());

		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryCreateSchedule(request, _guid.Value, Context.ConnectionAborted);
		return new(result, HttpStatusCode.BadRequest, result);
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> DeleteSchedule(Guid scheduleId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryDeleteSchedule(scheduleId, _guid.Value, Context.ConnectionAborted);
		return new(result, HttpStatusCode.BadRequest, result);
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> ReportStudents(Guid scheduleSlotId, IEnumerable<Guid> actualParticipants)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryReportActualStudentsForScheduleSlot(scheduleSlotId, _guid.Value, actualParticipants);
		return new(result, HttpStatusCode.BadRequest, x => x);
	}
}