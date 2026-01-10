using Microsoft.AspNetCore.SignalR;

namespace examScheduler.Hubs;

public interface IScheduleHub
{

}

public interface IScheduleClient
{

}

public class ScheduleHub : Hub<IScheduleClient>, IScheduleHub
{

}
