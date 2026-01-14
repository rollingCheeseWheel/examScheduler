using examScheduler.Data;
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

	Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid userId);
	Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId);
}

public interface IScheduleClient
{
	Task ReceiveSchedules(Schedule[ ] schedules);
	Task UpdateSchedule(Guid scheduleId, Schedule schedule);

	Task ReceiveSwapRequest(SwapRequest swapRequest);
	Task ReceiveInitialSwapRequests(IEnumerable<SwapRequest> swapRequests);
}

[Authorize]
public class ScheduleHub(
	IScheduleService scheduleService
) : Hub<IScheduleClient>, IScheduleHub
{
	private readonly IScheduleService _scheduleService = scheduleService;

	private CancellationToken ct => Context.ConnectionAborted;

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

		var scheduleIds = await _scheduleService.GetScheduleIdsForStudentAsync(userId, ct);
		foreach (var scheduleId in scheduleIds)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, scheduleId.ToString(), ct);
		}

		await TransmitBacklogAsync(userId, ct);

		await base.OnConnectedAsync();
	}

	public Task<Result<bool>> RegisterForSlot(Guid scheduleId, Guid slotId)
	{
		throw new NotImplementedException();
	}

	public async Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid userId)
	{
		var swapRequestId = await _scheduleService.CreateSwapRequestAsync(scheduleId, userId, ct);
		if (swapRequestId is null)
		{
			return new(HttpStatusCode.NotFound);
		} else
		{
			return new(true);
		}
	}

	public Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId)
	{
		throw new NotImplementedException();
	}

	private async Task TransmitBacklogAsync(Guid userId, CancellationToken ct = default)
	{
		var swapRequests = (await _scheduleService.GetSwapRequestForStudentAsync(userId, ct))
			.Select(SwapRequestMappings.ToDTO);
		await Clients.Caller.ReceiveInitialSwapRequests(swapRequests);
	}
}