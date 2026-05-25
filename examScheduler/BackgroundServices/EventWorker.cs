using examScheduler.Data;
using examScheduler.Hubs;
using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Frozen;
using System.Linq.Expressions;
using System.Reflection;
using Util;
using Util.DataStructures;
using Util.Extensions;

namespace examScheduler.BackgroundServices;

public interface IEvent;


public sealed record ScheduleUpdatedEvent(Guid ScheduleId) : IEvent;
public sealed record ScheduleRemovedEvent(Guid ScheduleId) : IEvent;
public sealed record CalendarUpdatedEvent(Guid CalendarId) : IEvent;
public sealed record ApplicationStartedEvent : IEvent;

public sealed record ExtendCalendarTask(Guid RegisterClientId, Guid StudentProfileId) : IEvent;
public sealed record LockScheduleTask(Guid ScheduleId) : IEvent;


[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EventAttribute(Type eventType) : Attribute
{
	public readonly Type EventType = eventType.IsAssignableTo(typeof(IEvent)) ? eventType : throw new ArgumentException($"{nameof(eventType)} is not assignable to {nameof(IEvent)}");
}

public interface IEventWorker
{
	ILogger<EventWorker> Logger { get; }
	IServiceScopeFactory ScopeFactory { get; }

	void Publish(IEvent @event);
	void Publish(IEvent @event, int offsetSeconds);
	void Publish(IEvent @event, TimeSpan offset);
	void Publish(IEvent @event, DateTimeOffset deferUntil);
}

public class EventWorker : BackgroundService, IEventWorker
{
	[Event(typeof(ScheduleUpdatedEvent))]
	public async Task ScheduleUpdated(ScheduleUpdatedEvent @event, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var hub = scope.ServiceProvider.GetRequiredService<IHubContext<ScheduleHub, IScheduleClient>>();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var schedule = await context._Schedules
			.AsNoTracking()
			.FindByIdAsync(@event.ScheduleId, ct);

		if (schedule is null)
		{
			Logger.LogWarning("unable to get schedule {id}", @event.ScheduleId);
			return;
		}

		await hub.ScheduleGroup(@event.ScheduleId).ScheduleUpdated(schedule.ToDTO());
	}

	[Event(typeof(ScheduleRemovedEvent))]
	public async Task ScheduleRemoved(ScheduleRemovedEvent @event, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var hub = scope.ServiceProvider.GetRequiredService<IHubContext<ScheduleHub, IScheduleClient>>();

		await hub.ScheduleGroup(@event.ScheduleId).ScheduleRemoved(@event.ScheduleId);
	}

	[Event(typeof(CalendarUpdatedEvent))]
	public async Task CalendarChanged(CalendarUpdatedEvent @event, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var hub = scope.ServiceProvider.GetRequiredService<IHubContext<ScheduleHub, IScheduleClient>>();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var classroom = await context.Classrooms
			.AsNoTracking()
			.JoinOnId(
				context._Calendars,
				c => c.CalendarId,
				(o, i) => o
			)
			.OrderById()
			.FirstOrDefaultAsync(ct);
		if (classroom is null)
		{
			Logger.LogWarning("unable to find classroom for calendar ({id})", @event.CalendarId);
			return;
		}

		await hub.ClassroomGroup(classroom.Id).ClassroomUpdated(classroom.ToDTO());
	}

	[Event(typeof(ExtendCalendarTask))]
	public async Task ExtendCalendar(ExtendCalendarTask task, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var clientService = scope.ServiceProvider.GetRequiredService<IDigitalRegisterClientService>();
		var calendarService = scope.ServiceProvider.GetRequiredService<ICalendarService>();

		using var transaction = await context.Database.BeginTransactionAsync(ct);

		var client = clientService.TryGetClient(task.RegisterClientId);
		if (client is null)
		{
			Logger.LogWarning("register client with id {id} not found", task.RegisterClientId);
			return;
		}

		var student = await context._StudentProfiles.FindByIdAsync(task.StudentProfileId, ct);
		if (student is null)
		{
			Logger.LogWarning("student with id {id} not found", task.StudentProfileId);
			return;
		}

		var calendar = await context.Classrooms
			.Where(c => c.Students.Any(s => s.Id == task.StudentProfileId))
			.JoinInnerOnId(context._Calendars, c => c.CalendarId)
			.OrderById()
			.FirstOrDefaultAsync(ct);
		if (calendar is null)
		{
			Logger.LogWarning("calendar not found");
			return;
		}
		if (calendar.LastExtended.ToDateOnly() >= DateTimeOffset.UtcNow.ToDateOnly())
		{
			Logger.LogDebug("not required to extend calendar");
			return;
		}

		var digitalRegisterLessons = await client.GetCalendarAsync(DateTimeOffset.UtcNow, DateUtils.Min(calendar.LastsUntil.AddMonths(1), DateTimeOffset.UtcNow), ct);
		if (digitalRegisterLessons is null || !digitalRegisterLessons.Any())
		{
			Logger.LogWarning("unable to get lessons: {start} to {end}", DateTimeOffset.UtcNow, DateUtils.Min(calendar.LastsUntil.AddMonths(1), DateTimeOffset.UtcNow));
			return;
		}
		if (!await calendarService.TryExtendCalendarAsync(calendar.Id, student.UserProfile.SchoolId, digitalRegisterLessons, ct))
		{
			Logger.LogWarning("failed to extend calendar ({id})", calendar.Id);
			return;
		}
		await context.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);
	}

	[Event(typeof(LockScheduleTask))]
	public async Task LockSchedule(LockScheduleTask task, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var schedule = await context.Classrooms
			.SelectMany(c => c.Schedules)
			.FindByIdAsync(task.ScheduleId, ct);
		if (schedule is null)
		{
			Logger.LogWarning("schedule doesnt exist ({id})", task.ScheduleId);
			return;
		}

		var students = await context.Classrooms
			.Where(c => c.Schedules.Any(s => s.Id == task.ScheduleId))
			.Select(c => c.Students)
			.FirstOrDefaultAsync(ct);
		if (students is null || students.Count == 0)
		{
			Logger.LogDebug("student count is 0 ({id})", task.ScheduleId);
			return;
		}

		var fillResult = schedule.TryFillSlots(students);
		if (!fillResult.Success)
		{
			Logger.LogWarning("failed to fill slots ({id})", task.ScheduleId);
			return;
		}
		await context.SaveChangesAsync(ct);
		Publish(new ScheduleUpdatedEvent(schedule.Id));
	}

	[Event(typeof(ApplicationStartedEvent))]
	public async Task AddSchoolsToDigitalRegisterClientService(ApplicationStartedEvent @event, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var clientService = scope.ServiceProvider.GetRequiredService<IDigitalRegisterClientService>();

		var schools = await context.Schools
			.AsNoTracking()
			.Where(s => s.IsEnabled)
			.ToListAsync(ct);
		foreach (var school in schools)
		{
			clientService.TryAddSchool(school);
		}
		var registeredSchools = clientService.GetRegisteredSchoolIds();
	}

	[Event(typeof(ApplicationStartedEvent))]
	public async Task InitializeScheduleLockTasks(ApplicationStartedEvent @event, CancellationToken ct)
	{
		using var scope = ScopeFactory.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var currentDateOnly = DateTimeOffset.UtcNow.ToDateOnly();

		var scheduleLockInDates = await context.Classrooms
			.AsNoTracking()
			.SelectMany(c => c.Schedules)
			.Where(s => s.ExamSlots.Any(e => e.Date <= currentDateOnly && e.LockInDate >= DateTimeOffset.UtcNow))
			.Select(s => new
			{
				s.Id,
				LockInDates = s.ExamSlots
					.Where(e => e.Date <= currentDateOnly && e.LockInDate >= DateTimeOffset.UtcNow)
					.Select(e => e.LockInDate)
					.ToList(),
			})
			.ToListAsync(ct);
		foreach (var scheduleDates in scheduleLockInDates)
		{
			foreach (var date in scheduleDates.LockInDates)
			{
				Publish(new LockScheduleTask(scheduleDates.Id), date);
			}
		}
	}

	#region Logic
	public ILogger<EventWorker> Logger { get; }
	public IServiceScopeFactory ScopeFactory { get; }

	private readonly FrozenDictionary<Type, FrozenSet<EventHandler>> _handlers;
	private readonly TimestampedQueue<IEvent> _events = new();

	public EventWorker(ILogger<EventWorker> logger, IServiceScopeFactory serviceScopeFactory)
	{
		Logger = logger;
		ScopeFactory = serviceScopeFactory;
		_handlers = GetDecoratedMethods().ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToFrozenSet());

		Publish(new ApplicationStartedEvent());
	}

	public void Publish(IEvent @event) => Publish(@event, TimeSpan.Zero);
	public void Publish(IEvent @event, int offsetSeconds) => Publish(@event, TimeSpan.FromSeconds(offsetSeconds));
	public void Publish(IEvent @event, TimeSpan offset) => Publish(@event, DateTimeOffset.UtcNow + offset);
	public void Publish(IEvent @event, DateTimeOffset deferUntil) => _events.Enqueue(@event, deferUntil);

	protected sealed override async Task ExecuteAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			var @event = await _events.DequeueAsync(ct);

			var type = @event.GetType();
			using (Logger.BeginScope("{event}", type.Name))
			{
				if (!_handlers.TryGetValue(type, out var eventHandlers))
				{
					Logger.LogWarning("No event listeners subscribed to {Type}", type.Name);
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
						Logger.LogError(ex, "Exception caught in {event}", type.Name);
					}
				}
			}
		}
	}

	private Dictionary<Type, IEnumerable<EventHandler>> GetDecoratedMethods() => GetType()
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

	private static Func<IEventWorker, IEvent, CancellationToken, Task> CompileHandler(MethodInfo method)
	{
		var targetParam = Expression.Parameter(typeof(IEventWorker), "target");
		var eventParam = Expression.Parameter(typeof(IEvent), "event");
		var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

		var instance = Expression.Convert(targetParam, method.DeclaringType!);
		var typedEvent = Expression.Convert(eventParam, method.GetParameters()[ 0 ].ParameterType);

		var call = Expression.Call(instance, method, typedEvent, ctParam);

		return Expression
			.Lambda<Func<IEventWorker, IEvent, CancellationToken, Task>>(call, targetParam, eventParam, ctParam)
			.Compile();
	}
	#endregion
}

internal enum HandlerType
{
	Invalid,
	Asynchronous,
}

internal sealed record EventHandler(Func<IEventWorker, IEvent, CancellationToken, Task> Handler);