using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Models.API;
using System.Net;
using System.Security.Claims;

namespace examScheduler.Hubs;

public interface IScheduleHub
{
	Task<Result<bool>> RegisterForSlot(Guid scheduleId, Guid slotId);

	Task<Result<Guid>> CreateSwapRequest(Guid scheduleId, Guid userId);
	Task<Result<bool>> DeleteSwapRequest(Guid swaprequestId);
	Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId);
}

public interface IScheduleClient
{
	Task RecieveInitialSchedules(IEnumerable<Schedule> schedules);
	Task UpdateSchedule(Guid scheduleId, Schedule schedule);

	Task ReceiveInitialSwapRequests(IEnumerable<SwapRequest> swapRequests);
	Task ReceiveSwapRequest(SwapRequest swapRequest);
	Task RemoveSwapRequest(Guid swapRequestId);
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

		var scheduleIds = await _scheduleService.GetScheduleIdsForStudentAsync(userId, _ct);
		foreach (var scheduleId in scheduleIds)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, scheduleId.ToString(), _ct);
		}

		await TransmitBacklogAsync(userId, _ct);

		await base.OnConnectedAsync();
	}

	public async Task<Result<bool>> RegisterForSlot(Guid scheduleId, Guid slotId)
	{
		if (!_isGuidSet) return new(HttpStatusCode.Unauthorized);

		var isSuccess = await _scheduleService.TryEnlistStudentAsync(scheduleId, slotId, _guid, _ct);
		if (isSuccess)
		{
			await TransmitChangedScheduleAsync(scheduleId, _ct);
		}
		return new(isSuccess, HttpStatusCode.BadRequest, isSuccess);
	}

	public async Task<Result<Guid>> CreateSwapRequest(Guid scheduleId, Guid userId)
	{
		if (!_isGuidSet) return new(HttpStatusCode.Unauthorized);

		var swapRequest = await _scheduleService.CreateSwapRequestAsync(scheduleId, _guid, userId, DateTimeOffset.UtcNow.AddDays(30), _ct);
		if (swapRequest is not null)
		{
			await Clients.User(userId.ToString())
				.ReceiveSwapRequest(swapRequest.ToDTO());
			return new(swapRequest.Id);
		}
		return new(HttpStatusCode.BadRequest);
	}

	public async Task<Result<bool>> DeleteSwapRequest(Guid swapRequestId)
	{
		if (!_isGuidSet) return new(HttpStatusCode.Unauthorized);

		var swapRequest = await _scheduleService.TryDeleteSwapRequestAsync(swapRequestId, _guid, _ct);
		if (swapRequest is not null)
		{
			await Clients
				.Users(swapRequest.RequestingStudentId.ToString(), swapRequest.RequestedStudentId.ToString())
				.RemoveSwapRequest(swapRequestId);
		}
		return new(swapRequest is not null, HttpStatusCode.BadRequest, swapRequest is not null);
	}

	public async Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId)
	{
		if (!_isGuidSet) return new(HttpStatusCode.Unauthorized);
		var swapRequest = await _scheduleService.TryAcceptSwapRequestAsync(swapRequestId, _guid, _ct);
		if (swapRequest is not null)
		{
			await Clients
				.Users(swapRequest.RequestingStudentId.ToString(), swapRequest.RequestedStudentId.ToString())
				.RemoveSwapRequest(swapRequestId);
			await TransmitChangedScheduleAsync(swapRequestId, _ct);
		}
		return new(swapRequest is not null, HttpStatusCode.BadRequest, swapRequest is not null);
	}

	private async Task TransmitChangedScheduleAsync(Guid scheduleId, CancellationToken ct = default)
	{
		var schedule = await _scheduleService.GetScheduleAsync(scheduleId, ct);
		if (schedule is not null)
		{
			await Clients.Group(scheduleId.ToString()).UpdateSchedule(scheduleId, schedule.ToDTO());
		}
	}

	private async Task TransmitBacklogAsync(Guid userId, CancellationToken ct = default)
	{
		var swapRequests = ( await _scheduleService.GetSwapRequestTargetingStudentAsync(userId, ct) )
			.Select(SwapRequestMappings.ToDTO);
		await Clients.Caller.ReceiveInitialSwapRequests(swapRequests);
	}
}