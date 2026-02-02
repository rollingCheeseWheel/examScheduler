
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace examScheduler.BackgroundServices;

public interface IScheduleWorker
{

}

public class ScheduleWorkerConfig
{
	public required int PollingOffsetSeconds { get; set; }
}

public class ScheduleWorker(
	IServiceScopeFactory serviceScopeFactory,
	IOptions<ScheduleWorkerConfig> options,
	ILogger<ScheduleWorker> logger
) : BackgroundService, IScheduleWorker
{
	private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
	private readonly IOptions<ScheduleWorkerConfig> _options = options;
	private readonly ILogger _logger = logger;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(TimeSpan.FromSeconds(_options.Value.PollingOffsetSeconds), stoppingToken);
				using var scope = _serviceScopeFactory.CreateScope();
				using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

				var entities = await context.Classrooms
					.SelectMany(c => c.Schedules)
					.Where(s => s.ExamSlots.Any(e => e.LockedAt != null && e.ProcessedAt == null))
					.ToListAsync(stoppingToken);
				if (entities.Count == 0)
				{
					continue;
				}
				_logger.LogInformation("Found {Count} items to work on", entities.Count);
			}
			catch (Exception e)
			{
				_logger.LogError("Error caught in ScheduleWorker: {Message} - retrying in {Offset} seconds", e.Message, _options.Value.PollingOffsetSeconds);
			}

		}
	}
}
