using System.Threading.Channels;

namespace examScheduler.BackgroundServices;

public interface ITaskWorker
{
	Task EnqueueAsync(Func<IServiceScope, ILogger, CancellationToken, Task> task, CancellationToken ct = default);
}

public class TaskWorker(
	ILogger<TaskWorker> logger,
	IServiceScopeFactory serviceScopeFactory
) : BackgroundService, ITaskWorker
{
	private readonly ILogger<TaskWorker> _logger = logger;
	private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

	private readonly Channel<(Guid Id, Func<IServiceScope, ILogger, CancellationToken, Task> Task)> _queue = Channel.CreateUnbounded<(Guid Id, Func<IServiceScope, ILogger, CancellationToken, Task> Task)>();

	protected override async Task ExecuteAsync(CancellationToken ct)
	{
		_logger.LogInformation("{Name} started", nameof(TaskWorker));

		using var scope = _serviceScopeFactory.CreateScope();
		while (!ct.IsCancellationRequested)
		{
			try
			{
				var task = await _queue.Reader.ReadAsync(ct);
				_logger.LogInformation("Working on task {Id}", task.Id);
				await task.Task(scope, _logger, ct);
			} catch (Exception ex)
			{
				_logger.LogWarning("Exception caught: {Message}", ex.Message);
			}
		}
	}

	public async Task EnqueueAsync(Func<IServiceScope, ILogger, CancellationToken, Task> task, CancellationToken ct = default)
	{
		await _queue.Writer.WriteAsync(new(Guid.NewGuid(), task), ct);
	}
}