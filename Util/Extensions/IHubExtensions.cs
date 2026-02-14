using Microsoft.AspNetCore.SignalR;

namespace Util.Extensions;

public static class IHubExtensions
{
	public static IClientProxy ScheduleGroup<THub>(this IHubContext<THub> hub, Guid scheduleId) where THub : Hub => hub.Clients.Group($"schedule:{scheduleId}");

	public static TClient ScheduleGroup<THub, TClient>(this IHubContext<THub, TClient> hub, Guid scheduleId) where THub : Hub<TClient> where TClient : class => hub.Clients.Group($"schedule:{scheduleId}");
	public static IClientProxy ClassroomGroup<THub>(this IHubContext<THub> hub, Guid classroomId) where THub : Hub => hub.Clients.Group($"classroom:{classroomId}");

	public static TClient ClassroomGroup<THub, TClient>(this IHubContext<THub, TClient> hub, Guid classroomId) where THub : Hub<TClient> where TClient : class => hub.Clients.Group($"classroom:{classroomId}");

	public static async Task AddToScheduleGroupAsync<THub>(this THub hub, Guid scheduleId, CancellationToken ct = default) where THub : Hub => await hub.Groups.AddToGroupAsync(hub.Context.ConnectionId, $"schedule:{scheduleId}", ct);

	public static async Task AddToClassroomGroupAsync<THub>(this THub hub, Guid classroomId, CancellationToken ct = default) where THub : Hub => await hub.Groups.AddToGroupAsync(hub.Context.ConnectionId, $"classroom:{classroomId}", ct);
}
