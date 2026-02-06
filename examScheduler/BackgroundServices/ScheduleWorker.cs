
using examScheduler.Data;
using examScheduler.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Util.Extensions;

namespace examScheduler.BackgroundServices;

public interface IScheduleWorker;

public class ScheduleWorkerConfig
{
	public required int PollingDelaySeconds { get; set; } = 60;
}

public class ScheduleWorker(
	IServiceScopeFactory serviceScopeFactory,
	IOptions<ScheduleWorkerConfig> config,
	ILogger<ScheduleWorker> logger,
	EventWorker eventBus
) : BackgroundService, IScheduleWorker
{
	private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
	private readonly IOptions<ScheduleWorkerConfig> _config = config;
	private readonly ILogger _logger = logger;
	private readonly EventWorker _eventBus = eventBus;

	protected override async Task ExecuteAsync(CancellationToken ct)
	{
		_logger.LogInformation("{Name} started: {Config}", nameof(ScheduleWorker), _config.Value.Stringify());
		while (!ct.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(TimeSpan.FromSeconds(_config.Value.PollingDelaySeconds), ct);
				using var scope = _serviceScopeFactory.CreateScope();
				using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

				var schedules = await context.Classrooms
					.SelectMany(c => c.Schedules)
					.Where(s => s.ExamSlots.Any(e =>
						e.LockInDate <= DateTimeOffset.UtcNow &&
						e.Date >= DateTimeOffset.UtcNow
						)
					)
					.ToListAsync(ct);
				if (schedules.Count == 0)
				{
					continue;
				}

				foreach (var schedule in schedules)
				{
					_logger.LogInformation("Working on {Id}", schedule.Id);
					var students = await context.Classrooms
						.Where(c => c.Schedules.ContainsId(schedule.Id))
						.Select(c => c.Students)
						.FirstOrDefaultAsync(ct);
					if (students is null)
					{
						continue;
					}
					schedule.FillSlots(students);
					await _eventBus.PublishAsync(new ScheduleUpdatedEvent(schedule.Id), ct);
				}
				await context.SaveChangesAsync(ct);
			}
			catch (Exception e)
			{
				_logger.LogError("Error caught in ScheduleWorker: {Message}", e.Message);
			}

		}
	}
}
