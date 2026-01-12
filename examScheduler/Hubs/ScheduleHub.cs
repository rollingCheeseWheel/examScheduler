using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Models.API;

namespace examScheduler.Hubs;

public interface IScheduleHub
{
	Task<Result<bool>> RegisterForSlot(Guid scheduleId, Guid slotId);

	Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid userId);
	Task<Result<bool>> AcceptSwapRequest(Guid scheduleId, Guid swapRequestId);
}

public interface IScheduleClient
{
	Task ReceiveSchedules(Schedule[ ] schedules);
	Task UpdateSchedule(Guid scheduleId, Schedule schedule);

	Task ReceiveSwapRequest(Guid scheduleId, UserProfile user, Guid swapRequestId);
}

[Authorize]
public class ScheduleHub : Hub<IScheduleClient>, IScheduleHub
{
	public Task<Result<bool>> AcceptSwapRequest(Guid scheduleId, Guid swapRequestId)
	{
		throw new NotImplementedException();
	}

	public Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid userId)
	{
		throw new NotImplementedException();
	}

	public Task<Result<bool>> RegisterForSlot(Guid scheduleId, Guid slotId)
	{
		throw new NotImplementedException();
	}
}
