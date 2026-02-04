using examScheduler.Events;
using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Models.API;
using System.Collections.Concurrent;
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
	Task RemoveSchedule(Guid scheduleId);
}

public static class ScheduleHubConnectionIds
{
	private static ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _connections = new();

	public static IEnumerable<string> GetConnections(Guid scheduleId)
	{
		if (!_connections.TryGetValue(scheduleId, out var connectionBag))
		{
			return [];
		}
		return connectionBag.Keys.ToArray();
	}

	public static void Add(Guid scheduleId, string connectionId)
	{
		var dict = _connections.GetOrAdd(scheduleId, _ => new());
		dict.TryAdd(connectionId, new());
	}

	public static void Remove(string connectionId)
	{
		foreach (var dict in _connections.Values.ToArray())
		{
			if (dict.Remove(connectionId, out var _))
			{
				return;
			}
		}
	}
}

[Authorize]
public class ScheduleHub : Hub<IScheduleClient>, IScheduleHub
{
	private readonly IScheduleService _scheduleService;
	private readonly IEventBus _eventBus;

	private Guid? _guid = default;

	private Guid? UpdateEventListenerId = null;
	private Guid? RemoveEventListenerId = null;

	private CancellationToken _ct => Context.ConnectionAborted;

	public ScheduleHub(IScheduleService scheduleService, IEventBus eventBus)
	{
		_scheduleService = scheduleService;
		_eventBus = eventBus;
		_eventBus.Subscribe<ScheduleUpdatedEvent>(TransmitUpdateAsync);
		_eventBus.Subscribe<ScheduleDeletedEvent>(TransmitRemoveAsync);
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		_eventBus.Unsubscribe(UpdateEventListenerId);
		_eventBus.Unsubscribe(RemoveEventListenerId);

		ScheduleHubConnectionIds.Remove(Context.ConnectionId);

		await base.OnDisconnectedAsync(exception);
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
		_guid = userId;

		var scheduleIds = await _scheduleService.GetScheduleIdsForStudentAsync_AsNoTracking(userId, _ct);
		foreach (var scheduleId in scheduleIds)
		{
			ScheduleHubConnectionIds.Add(scheduleId, Context.ConnectionId);
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
		return new(result, HttpStatusCode.BadRequest, x => x);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid examSlotId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryCreateSwapRequestAsync(scheduleId, _guid.Value, examSlotId, _ct);
		return new(result, HttpStatusCode.BadRequest, x => x);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> DeleteSwapRequest(Guid swapRequestId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryDeleteSwapRequestAsync(swapRequestId, _guid.Value, _ct);
		return new(result, HttpStatusCode.BadRequest, x => x);
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryAcceptSwapRequestAsync(swapRequestId, _guid.Value, _ct);
		return new(result, HttpStatusCode.BadRequest, x => x);
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> CreateSchedule(ScheduleCreateRequest request)
	{
		if (!_guid.HasValue)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		var result = await _scheduleService.TryCreateSchedule(request, _guid.Value, _ct);
		return new(result, HttpStatusCode.BadRequest, x => x);
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

	private async Task TransmitUpdateAsync(ScheduleUpdatedEvent @event, CancellationToken ct)
	{
		var schedule = await _scheduleService.GetScheduleAsync(@event.ScheduleId, ct);
		if (schedule is not null)
		{
			await ScheduleGroup(@event.ScheduleId).UpdateSchedule(@event.ScheduleId, schedule.ToDTO());
		}
	}
	private async Task TransmitRemoveAsync(ScheduleDeletedEvent @event, CancellationToken ct)
	{
		await ScheduleGroup(@event.ScheduleId).RemoveSchedule(@event.ScheduleId);
		await DissolveScheduleGuidAsync(@event.ScheduleId, ct);
	}

	private IScheduleClient ScheduleGroup(Guid scheduleId) => Clients.Group(scheduleId.ToString());

	private async Task DissolveScheduleGuidAsync(Guid scheduleId, CancellationToken ct = default)
	{
		var tasks =  new List<Task>();
		foreach (var connectionId in ScheduleHubConnectionIds.GetConnections(scheduleId))
		{
			tasks.Add(Groups.RemoveFromGroupAsync(connectionId, scheduleId.ToString(), ct));
		}
		await Task.WhenAll(tasks).WaitAsync(ct);
	}
}

public sealed record ScheduleUpdatedEvent(Guid ScheduleId) : IEvent;
public sealed record ScheduleDeletedEvent(Guid ScheduleId) : IEvent;