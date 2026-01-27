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
	1.	Swap requests target slots instead of students
	2.	If two students from different slots want to swap with each others slots, the swap should resolve instantly	
 */

public interface IScheduleHub
{
	Task<Result<bool>> RegisterForSlot(Guid slotId);

	Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid userId);
	Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId);
	Task<Result<bool>> DeleteSwapRequest(Guid swaprequestId);

	Task<Result<bool>> CreateSchedule(ScheduleCreateRequest request);
	Task<Result<bool>> ReportStudents(Guid scheduleSlotId, IEnumerable<UserProfile> actualParticipants);
}

public interface IScheduleClient
{
	Task ReceiveInitial(IEnumerable<Schedule> schedules);
	Task UpdateSchedule(Guid scheduleId, Schedule schedule);
}

[Authorize]
public class ScheduleHub(
	IScheduleService scheduleService
) : Hub<IScheduleClient>, IScheduleHub
{
	private readonly IScheduleService _scheduleService = scheduleService;

	private Guid _guid = default;
	private bool _isGuidSet = false;

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
		_isGuidSet = true;
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
		if (!_isGuidSet)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var enlistedScheduleId = await _scheduleService.TryEnlistStudentAsync(slotId, _guid, _ct);
		if (enlistedScheduleId is not null)
		{
			await TransmitScheduleAsync(enlistedScheduleId.Value, _ct);
		}
		return new(enlistedScheduleId is not null,
			HttpStatusCode.BadRequest,
			enlistedScheduleId is not null
		);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid examSlotId)
	{
		if (!_isGuidSet)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var swapRequest = await _scheduleService.TryCreateSwapRequestAsync(scheduleId, _guid, examSlotId, DateTimeOffset.UtcNow.AddDays(30), _ct);
		if (swapRequest is not null)
		{
			await TransmitScheduleAsync(swapRequest.ScheduleId, _ct);
		}
		return new(
			swapRequest is null,
			HttpStatusCode.BadRequest,
			swapRequest is null
		);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> DeleteSwapRequest(Guid swapRequestId)
	{
		if (!_isGuidSet)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var swapRequest = await _scheduleService.TryDeleteSwapRequestAsync(swapRequestId, _guid, _ct);
		if (swapRequest is not null)
		{
			await TransmitScheduleAsync(swapRequest.ScheduleId, _ct);
		}
		return new(swapRequest is not null, HttpStatusCode.BadRequest, swapRequest is not null);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId)
	{
		if (!_isGuidSet)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var swapRequest = await _scheduleService.TryAcceptSwapRequestAsync(swapRequestId, _guid, _ct);
		if (swapRequest is not null)
		{
			await TransmitScheduleAsync(swapRequest.ScheduleId, _ct);
		}
		return new(swapRequest is not null, HttpStatusCode.BadRequest, swapRequest is not null);
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> CreateSchedule(ScheduleCreateRequest request)
	{
		if (!_isGuidSet)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var scheduleId = await _scheduleService.TryCreateSchedule(request, _guid, _ct);
		if (scheduleId is not null)
		{
			await TransmitScheduleAsync(scheduleId.Value, _ct);
		}
		return new(scheduleId is not null,
			HttpStatusCode.BadRequest,
			scheduleId is not null
		);
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> ReportStudents(Guid scheduleSlotId, IEnumerable<UserProfile> actualParticipants)
	{
		if (!_isGuidSet)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var scheduleId = await _scheduleService.TryReportActualStudentsForScheduleSlot(scheduleSlotId, actualParticipants);
		if (scheduleId is not null)
		{
			await TransmitScheduleAsync(scheduleId.Value, _ct);
		}
		return new(scheduleId is not null,
			HttpStatusCode.BadRequest,
			scheduleId is not null
		);
	}

	private async Task TransmitScheduleAsync(Guid scheduleId, CancellationToken ct = default)
	{
		var schedule = await _scheduleService.GetScheduleAsync(scheduleId, ct);
		if (schedule is not null)
		{
			await Clients.Group(scheduleId.ToString()).UpdateSchedule(scheduleId, schedule.ToDTO());
		}
	}

	private async Task TransmitInitialSchedules(Guid userId, CancellationToken ct = default)
	{
		var schedules = await _scheduleService.GetSchedulesForStudentAsync_AsNoTracking(userId, ct);
		await Clients.Caller.ReceiveInitial(schedules.Select(x => x.ToDTO()));
	}
}