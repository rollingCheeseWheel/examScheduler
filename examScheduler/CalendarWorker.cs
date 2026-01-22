using System.Threading.Channels;

namespace examScheduler;

public interface ICalendarWorker
{
	Task EnqueueAsync(Func<IServiceScope, ILogger, CancellationToken, Task> task, CancellationToken ct = default);
}

public class CalendarWorker(
	ILogger<CalendarWorker> logger,
	IServiceScopeFactory serviceScopeFactory
) : BackgroundService, ICalendarWorker
{
	private readonly ILogger<CalendarWorker> _logger = logger;
	private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

	private readonly Channel<CalendarTask> _queue = Channel.CreateUnbounded<CalendarTask>();

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Calendar worker started");

		while (await _queue.Reader.WaitToReadAsync(stoppingToken))
		{
			while (_queue.Reader.TryRead(out CalendarTask? task))
			{
				_logger.LogInformation("Working on {Id}", task.Id);
				using IServiceScope scope = _serviceScopeFactory.CreateScope();
				await task.Task(scope, _logger, stoppingToken);
				_logger.LogInformation("Finished {Id}", task.Id);
			}
		}
	}

	public async Task EnqueueAsync(Func<IServiceScope, ILogger, CancellationToken, Task> task, CancellationToken ct = default)
	{
		var taskId = Guid.NewGuid();
		var calendarTask = new CalendarTask(taskId, task);
		await _queue.Writer.WriteAsync(calendarTask, ct);
	}
}

internal record CalendarTask(Guid Id, Func<IServiceScope, ILogger, CancellationToken, Task> Task);