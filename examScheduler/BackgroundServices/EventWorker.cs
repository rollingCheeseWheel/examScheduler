using examScheduler.Data;
using examScheduler.Hubs;
using examScheduler.Mappings;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Channels;
using Util.Extensions;

namespace examScheduler.BackgroundServices;

public abstract record IEvent(TimeSpan? offset)
{
	private readonly TimeSpan Offset = offset ?? TimeSpan.Zero;
	private readonly DateTimeOffset CreationDate = DateTimeOffset.UtcNow;
	public DateTimeOffset DelayUntil => CreationDate + Offset;
}

public sealed record ScheduleUpdatedEvent(Guid ScheduleId, TimeSpan? offset = null) : IEvent(offset);
public sealed record ScheduleRemovedEvent(Guid ScheduleId, TimeSpan? offset = null) : IEvent(offset);
public sealed record ClassroomStudentCountChangedEvent(Guid ClassroomId, TimeSpan? offset = null) : IEvent(offset);

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EventAttribute(Type eventType) : Attribute
{
	public readonly Type EventType = eventType.IsAssignableTo(typeof(IEvent)) ? eventType : throw new ArgumentException($"{nameof(eventType)} is not assignable to {nameof(IEvent)}");
}

public interface IEventWorker
{
	Task PublishAsync(IEvent @event, CancellationToken ct = default);
}

public class EventWorker : BackgroundService, IEventWorker
{
	private readonly ILogger<EventWorker> Logger;
	private readonly IServiceScopeFactory ScopeFactory;

	private readonly ReadOnlyDictionary<Type, MethodInfo> _handlers;
	private readonly Channel<IEvent> _events = Channel.CreateUnbounded<IEvent>(new() { SingleReader = true });

	public EventWorker(ILogger<EventWorker> logger, IServiceScopeFactory serviceScopeFactory)
	{
		Logger = logger;
		ScopeFactory = serviceScopeFactory;
		_handlers = new(GetDecoratedMethods());
	}

	[Event(typeof(ScheduleUpdatedEvent))]
	private async Task ScheduleUpdated(ScheduleUpdatedEvent @event, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var hub = scope.ServiceProvider.GetRequiredService<IHubContext<ScheduleHub, IScheduleClient>>();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var schedule = await context.Classrooms
			.AsNoTracking()
			.SelectMany(c => c.Schedules)
			.FindByIdAsync(@event.ScheduleId, ct);

		if (schedule is null)
		{
			return;
		}

		await hub.Clients.Group(@event.ScheduleId.ToString()).UpdateSchedule(@event.ScheduleId, schedule.ToDTO()).WaitAsync(ct);

	}

	[Event(typeof(ScheduleRemovedEvent))]
	private async Task ScheduleRemoved(ScheduleRemovedEvent @event, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var hub = scope.ServiceProvider.GetRequiredService<IHubContext<ScheduleHub, IScheduleClient>>();

		await hub.Clients.Group(@event.ScheduleId.ToString()).RemoveSchedule(@event.ScheduleId).WaitAsync(ct);
	}

	[Event(typeof(ClassroomStudentCountChangedEvent))]
	private async Task ClassroomStudentCountChanged(ClassroomStudentCountChangedEvent @event, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var classroom = await context.Classrooms.FindByIdAsync(@event.ClassroomId, ct);
		if (classroom is null)
		{
			return;
		}

		foreach (var schedule in classroom.Schedules)
		{
			schedule.Extend(classroom.Students.Count);
		}

		await context.SaveChangesAsync(ct);
	}

	public async Task PublishAsync(IEvent @event, CancellationToken ct = default)
	{
		await _events.Writer.WriteAsync(@event, ct);
	}

	protected sealed override async Task ExecuteAsync(CancellationToken ct)
	{
		Logger.LogInformation("{Name} started", nameof(EventWorker));
		while (!ct.IsCancellationRequested)
		{
			try
			{
				var @event = await _events.Reader.ReadAsync(ct);
				try
				{
					var timeOut = @event.DelayUntil - DateTimeOffset.UtcNow;
					if (timeOut < TimeSpan.Zero)
					{
						timeOut = TimeSpan.Zero;
					}
					await Task.Delay(timeOut, ct);
				} catch
				{

				}

				var type = @event.GetType();
				if (!_handlers.TryGetValue(type, out var methodInfo))
				{
					Logger.LogInformation("No event listeners subscribed to {Type}", type.Name);
					continue;
				}

				if (methodInfo.ReturnType.IsAssignableTo(typeof(Task)))
				{
					await (Task)methodInfo.Invoke(this, [ @event, ct ])!;
				}
				else
				{
					methodInfo.Invoke(this, [ @event ]);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError("Exception caught: {Message}", ex.Message);
			}
		}
	}

	private Dictionary<Type, MethodInfo> GetDecoratedMethods()
	{
		return GetType()
				.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(m => m.GetCustomAttribute<EventAttribute>() is not null)
				.ToDictionary(m => m.GetCustomAttribute<EventAttribute>()!.EventType);
	}
}