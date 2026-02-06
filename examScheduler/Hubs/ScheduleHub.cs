using examScheduler.BackgroundServices;
using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Models.API;
using System.Net;
using System.Security.Claims;
using Util;

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
	Task<Result<bool>> ReportStudents(Guid scheduleSlotId, IEnumerable<UserProfile> actualParticipants);
}

public interface IScheduleClient
{
	Task ReceiveInitial(IEnumerable<Schedule> schedules);
	Task UpdateSchedule(Guid scheduleId, Schedule schedule);
	Task RemoveSchedule(Guid scheduleId);
}

[Authorize]
public class ScheduleHub(IScheduleService scheduleService, IEventWorker eventWorker) : Hub<IScheduleClient>, IScheduleHub
{
	private readonly IScheduleService _scheduleService = scheduleService;
	private readonly IEventWorker _eventWorker = eventWorker;

	private Guid? _guid = default;

	private CancellationToken _ct => Context.ConnectionAborted;

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

		var scheduleIds = await _scheduleService.GetScheduleIdsForStudentAsync_AsNoTracking(userId, _ct);
		foreach (var scheduleId in scheduleIds)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, scheduleId.ToString(), _ct);
		}

		await TransmitInitialSchedules(userId, _ct);

		await base.OnConnectedAsync();
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> RegisterForSlot(Guid slotId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryEnlistStudentAsync(slotId, _guid.Value, _ct);
		return new(result, HttpStatusCode.BadRequest, result);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid examSlotId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryCreateSwapRequestAsync(scheduleId, _guid.Value, examSlotId, _ct);
		return new(result, HttpStatusCode.BadRequest, result);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> DeleteSwapRequest(Guid swapRequestId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryDeleteSwapRequestAsync(swapRequestId, _guid.Value, _ct);
		return new(result, HttpStatusCode.BadRequest, result);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryAcceptSwapRequestAsync(swapRequestId, _guid.Value, _ct);
		return new(result, HttpStatusCode.BadRequest, result);
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> CreateSchedule(ScheduleCreateRequest request)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryCreateSchedule(request, _guid.Value, _ct);
		return new(result, HttpStatusCode.BadRequest, result);
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> DeleteSchedule(Guid scheduleId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryDeleteSchedule(scheduleId, _guid.Value, _ct);
		return new(result, HttpStatusCode.BadRequest, result);
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> ReportStudents(Guid scheduleSlotId, IEnumerable<UserProfile> actualParticipants)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryReportActualStudentsForScheduleSlot(scheduleSlotId, _guid.Value, actualParticipants);
		return new(result, HttpStatusCode.BadRequest, x => x);
	}

	private async Task TransmitInitialSchedules(Guid userId, CancellationToken ct = default)
	{
		var schedules = await _scheduleService.GetSchedulesForStudentAsync_AsNoTracking(userId, ct);
		await Clients.Caller.ReceiveInitial(schedules.Select(x => x.ToDTO()));
	}
}