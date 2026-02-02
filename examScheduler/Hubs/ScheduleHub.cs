using examScheduler.Events;
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

	Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid examSlotId);
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
public class ScheduleHub : Hub<IScheduleClient>, IScheduleHub
{
	private readonly IScheduleService _scheduleService;
	private readonly IEventBus _eventBus;

	private Guid _guid = default;
	private bool _isGuidSet = false;

	private CancellationToken _ct => Context.ConnectionAborted;

	public ScheduleHub(IScheduleService scheduleService, IEventBus eventBus)
	{
		_scheduleService = scheduleService;
		_eventBus = eventBus;
		_eventBus.Subscribe<ScheduleUpdatedEvent>(TransmitScheduleAsync);
	}

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

		var result = await _scheduleService.TryEnlistStudentAsync(slotId, _guid, _ct);
		return new(result, HttpStatusCode.BadRequest, x => x);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid examSlotId)
	{
		if (!_isGuidSet)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryCreateSwapRequestAsync(scheduleId, _guid, examSlotId, _ct);
		return new(result, HttpStatusCode.BadRequest, x => x);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> DeleteSwapRequest(Guid swapRequestId)
	{
		if (!_isGuidSet)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryDeleteSwapRequestAsync(swapRequestId, _guid, _ct);
		return new(result, HttpStatusCode.BadRequest, x => x);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId)
	{
		if (!_isGuidSet)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryAcceptSwapRequestAsync(swapRequestId, _guid, _ct);
		return new(result, HttpStatusCode.BadRequest, x => x);
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> CreateSchedule(ScheduleCreateRequest request)
	{
		if (!_isGuidSet)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryCreateSchedule(request, _guid, _ct);
		return new(result, HttpStatusCode.BadRequest, x => x);
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> ReportStudents(Guid scheduleSlotId, IEnumerable<UserProfile> actualParticipants)
	{
		if (!_isGuidSet)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryReportActualStudentsForScheduleSlot(scheduleSlotId, _guid, actualParticipants);
		return new(result, HttpStatusCode.BadRequest, x => x);
	}

	private async Task TransmitScheduleAsync(ScheduleUpdatedEvent @event, CancellationToken ct)
	{
		var schedule = await _scheduleService.GetScheduleAsync(@event.ScheduleId, ct);
		if (schedule is not null)
		{
			await Clients.Group(@event.ScheduleId.ToString()).UpdateSchedule(@event.ScheduleId, schedule.ToDTO());
		}
	}

	private async Task TransmitInitialSchedules(Guid userId, CancellationToken ct = default)
	{
		var schedules = await _scheduleService.GetSchedulesForStudentAsync_AsNoTracking(userId, ct);
		await Clients.Caller.ReceiveInitial(schedules.Select(x => x.ToDTO()));
	}
}

public sealed record ScheduleUpdatedEvent(Guid ScheduleId) : IEvent;