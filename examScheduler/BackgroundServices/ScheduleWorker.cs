
using examScheduler.Data;
using examScheduler.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Util.DataStructures;
using Util.Extensions;

namespace examScheduler.BackgroundServices;

public interface IScheduleWorker
{
	Task InitAsync(CancellationToken ct = default);
	void Enqueue(Guid scheduleId, DateTimeOffset processDate);
}

public class ScheduleWorker(
	IServiceScopeFactory scopeFactory,
	ILogger<ScheduleWorker> logger,
	EventWorker eventBus
) : BackgroundService, IScheduleWorker
{
	private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
	private readonly ILogger _logger = logger;
	private readonly EventWorker _eventBus = eventBus;
	private readonly TimestampedQueue<Guid> _queue = new();

	public async Task InitAsync(CancellationToken ct = default)
	{
		using var scope = _scopeFactory.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var matchedSchedules = await context.Classrooms
			.SelectMany(c => c.Schedules)
			.Where(s => s.ExamSlots.Any(e => !e.HasBeenProcessed && e.LockInDate <= DateTimeOffset.UtcNow))
			.ToListAsync(ct);

		foreach (var schedule in matchedSchedules)
		{
			Enqueue(schedule.Id, DateTimeOffset.UtcNow);
		}
	}

	public void Enqueue(Guid scheduleId, DateTimeOffset processDate) => _queue.Enqueue(processDate, scheduleId);

	protected override async Task ExecuteAsync(CancellationToken ct)
	{
		_logger.LogInformation("{Name} started", nameof(ScheduleWorker));
		using var scope = _scopeFactory.CreateScope();
		while (!ct.IsCancellationRequested)
		{
			try
			{
				var scheduleId = await _queue.DequeueAsync(ct);
				var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
				_logger.LogInformation("Working on {Id}", scheduleId);

				var schedule = await context.Classrooms
					.SelectMany(c => c.Schedules)
					.FindByIdAsync(scheduleId, ct);
				if (schedule is null)
				{
					continue;
				}

				var students = await context.Classrooms
					.Where(c => c.Schedules.ContainsId(scheduleId))
					.Select(c => c.Students)
					.FirstOrDefaultAsync(ct);
				if (students is null || students.Count == 0)
				{
					continue;
				}

				schedule.FillSlots(students);
				_eventBus.Publish(new ScheduleUpdatedEvent(schedule.Id), 3);
				await context.SaveChangesAsync(ct);
			}
			catch (Exception e)
			{
				_logger.LogError("Error caught in ScheduleWorker: {Message}", e.Message);
			}

		}
	}
}
