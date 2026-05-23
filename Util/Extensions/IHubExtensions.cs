using Microsoft.AspNetCore.SignalR;

namespace Util.Extensions;

public static class IHubExtensions
{
	private static string SK(Guid scheduleId) => $"schedule:{scheduleId}";
	private static string CK(Guid classroomId) => $"classroom:{classroomId}";

	public static TClient ScheduleGroup<TClient>(this IHubClients<TClient> clients, Guid scheduleId) => clients.Group(SK(scheduleId));
	public static TClient ClassroomGroup<TClient>(this IHubClients<TClient> clients, Guid classroomId) => clients.Group(CK(classroomId));

	public static TClient ScheduleGroup<THub, TClient>(this IHubContext<THub, TClient> hub, Guid scheduleId) where THub : Hub<TClient> where TClient : class => hub.Clients.ScheduleGroup(scheduleId);
	public static TClient ClassroomGroup<THub, TClient>(this IHubContext<THub, TClient> hub, Guid classroomId) where THub : Hub<TClient> where TClient : class => hub.Clients.ClassroomGroup(classroomId);

	public static async Task AddToScheduleGroupAsync<THub>(this THub hub, Guid scheduleId, CancellationToken ct = default) where THub : Hub => await hub.Groups.AddToGroupAsync(hub.Context.ConnectionId, SK(scheduleId), ct);

	public static async Task AddToClassroomGroupAsync<THub>(this THub hub, Guid classroomId, CancellationToken ct = default) where THub : Hub => await hub.Groups.AddToGroupAsync(hub.Context.ConnectionId, CK(classroomId), ct);
}
