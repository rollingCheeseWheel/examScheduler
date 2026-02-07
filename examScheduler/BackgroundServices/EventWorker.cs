using examScheduler.Data;
using examScheduler.Hubs;
using examScheduler.Mappings;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Channels;
using Util.DataStructures;
using Util.Extensions;

namespace examScheduler.BackgroundServices;

public interface IEvent;

public sealed record ScheduleUpdatedEvent(Guid ScheduleId) : IEvent;
public sealed record ScheduleRemovedEvent(Guid ScheduleId) : IEvent;
public sealed record ClassroomStudentCountChangedEvent(Guid ClassroomId) : IEvent;
public sealed record CalendarUpdatedEvent(Guid CalendarId) : IEvent;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EventAttribute(Type eventType) : Attribute
{
	public readonly Type EventType = eventType.IsAssignableTo(typeof(IEvent)) ? eventType : throw new ArgumentException($"{nameof(eventType)} is not assignable to {nameof(IEvent)}");
}

public interface IEventWorker
{
	void Publish(IEvent @event);
	void Publish(IEvent @event, int offsetSeconds);
	void Publish(IEvent @event, TimeSpan offset);
	void Publish(IEvent @event, DateTimeOffset deferUntil);
}

public class EventWorker : BackgroundService, IEventWorker
{

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

		await hub.ScheduleGroup(@event.ScheduleId).UpdateSchedule(@event.ScheduleId, schedule.ToDTO()).WaitAsync(ct);

	}

	[Event(typeof(ScheduleRemovedEvent))]
	private async Task ScheduleRemoved(ScheduleRemovedEvent @event, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var hub = scope.ServiceProvider.GetRequiredService<IHubContext<ScheduleHub, IScheduleClient>>();

		await hub.ScheduleGroup(@event.ScheduleId).RemoveSchedule(@event.ScheduleId).WaitAsync(ct);
	}

	[Event(typeof(ClassroomStudentCountChangedEvent))]
	private async Task ClassroomStudentCountChanged(ClassroomStudentCountChangedEvent @event, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var hub = scope.ServiceProvider.GetRequiredService<IHubContext<ScheduleHub, IScheduleClient>>();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var scheduleWorker = scope.ServiceProvider.GetRequiredService<ScheduleWorker>();

		var classroom = await context.Classrooms.FindByIdAsync(@event.ClassroomId, ct);
		if (classroom is null)
		{
			return;
		}

		foreach (var schedule in classroom.Schedules)
		{
			schedule.Extend(classroom.Students.Count);
			scheduleWorker.Enqueue(schedule.Id, DateTimeOffset.UtcNow);
		}

		await context.SaveChangesAsync(ct);

		await hub.ClassroomGroup(@event.ClassroomId).UpdateClassroom(classroom.ToDTO());
	}

	[Event(typeof(CalendarUpdatedEvent))]
	private async Task CalendarChanged(CalendarUpdatedEvent @event, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var hub = scope.ServiceProvider.GetRequiredService<IHubContext<ScheduleHub, IScheduleClient>>();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var classroom = await context.Classrooms
			.AsNoTracking()
			.WhereId(c => c.Calendar, @event.CalendarId)
			.FirstOrDefaultAsync(ct);
		if (classroom is null)
		{
			return;
		}

		await hub.ClassroomGroup(classroom.Id).UpdateClassroom(classroom.ToDTO());
	}

	#region Logic
	private readonly ILogger<EventWorker> Logger;
	private readonly IServiceScopeFactory ScopeFactory;

	private readonly FrozenDictionary<Type, ImmutableList<EventHandler>> _handlers;
	private readonly TimestampedQueue<IEvent> _events = new();

	public EventWorker(ILogger<EventWorker> logger, IServiceScopeFactory serviceScopeFactory)
	{
		Logger = logger;
		ScopeFactory = serviceScopeFactory;
		_handlers = GetDecoratedMethods().ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableList());
	}

	public void Publish(IEvent @event) => Publish(@event, TimeSpan.Zero);
	public void Publish(IEvent @event, int offsetSeconds) => Publish(@event, TimeSpan.FromSeconds(offsetSeconds));
	public void Publish(IEvent @event, TimeSpan offset) => Publish(@event, DateTimeOffset.UtcNow + offset);
	public void Publish(IEvent @event, DateTimeOffset deferUntil) => _events.Enqueue(deferUntil, @event);

	protected sealed override async Task ExecuteAsync(CancellationToken ct)
	{
		Logger.LogInformation("{Name} started", nameof(EventWorker));
		while (!ct.IsCancellationRequested)
		{
			var @event = await _events.DequeueAsync(ct);

			var type = @event.GetType();
			if (!_handlers.TryGetValue(type, out var eventHandlers))
			{
				Logger.LogInformation("No event listeners subscribed to {Type}", type.Name);
				continue;
			}

			foreach (var eventHandler in eventHandlers)
			{
				try
				{
					await eventHandler.Handler(this, @event, ct);
				}
				catch (Exception ex)
				{
					Logger.LogError("Exception caught: {Message}", ex.InnerException?.Message ?? ex.Message);
				}
			}
		}
	}

	private Dictionary<Type, IEnumerable<EventHandler>> GetDecoratedMethods()
	{
		return GetType()
				.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(m => m.GetCustomAttribute<EventAttribute>() is not null)
				.Select(m => new
				{
					MethodInfo = m,
					EventType = m.GetCustomAttribute<EventAttribute>()!.EventType
				})
				.Where(x => IsValidHandler(x.MethodInfo, x.EventType) is not HandlerType.Invalid)
				.Select(x => new
				{
					EventHandler = new EventHandler(CompileHandler(x.MethodInfo)),
					EventType = x.EventType
				})
				.GroupBy(x => x.EventType)
				.ToDictionary(g => g.Key, g => g.Select(x => x.EventHandler));
	}

	private static HandlerType IsValidHandler(MethodInfo method, Type eventType)
	{
		var parameters = method.GetParameters();
		if (method.ReturnType.IsAssignableTo(typeof(Task)))
		{
			if (parameters.Length != 2)
			{
				return HandlerType.Invalid;
			}
			else if (parameters[ 0 ].ParameterType != eventType)
			{
				return HandlerType.Invalid;
			}
			else if (!parameters[ 1 ].ParameterType.IsAssignableTo(typeof(CancellationToken)))
			{
				return HandlerType.Invalid;
			}
			return HandlerType.Asynchronous;
		}
		return HandlerType.Invalid;
	}

	private static Func<object, IEvent, CancellationToken, Task> CompileHandler(MethodInfo method)
	{
		var targetParam = Expression.Parameter(typeof(object), "target");
		var eventParam = Expression.Parameter(typeof(IEvent), "event");
		var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

		var instance = Expression.Convert(targetParam, method.DeclaringType!);
		var typedEvent = Expression.Convert(eventParam, method.GetParameters()[ 0 ].ParameterType);

		var call = Expression.Call(instance, method, typedEvent, ctParam);

		return Expression
			.Lambda<Func<object, IEvent, CancellationToken, Task>>(call, targetParam, eventParam, ctParam)
			.Compile();
	}
	#endregion
}

internal enum HandlerType
{
	Invalid,
	Asynchronous,
}

internal sealed record EventHandler(Func<object, IEvent, CancellationToken, Task> Handler);