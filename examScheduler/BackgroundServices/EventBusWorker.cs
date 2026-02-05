using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace examScheduler.BackgroundServices;

public interface IEvent;

public interface IEventBus
{
	Task PublishAsync(IEvent @event, CancellationToken ct = default);
	Guid Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : IEvent;
	void Unsubscribe(Guid id);
}

public class EventBusWorker(ILogger<EventBusWorker> logger) : BackgroundService, IEventBus
{
	private readonly ILogger<EventBusWorker> _logger = logger;

	private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, Func<IEvent, CancellationToken, Task>>> _handlers = new();
	private readonly Channel<IEvent> _events = Channel.CreateUnbounded<IEvent>();

	public async Task PublishAsync(IEvent @event, CancellationToken ct = default)
	{
		await _events.Writer.WriteAsync(@event, ct);
	}

	public Guid Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : IEvent
	{
		var id = Guid.NewGuid();
		var handlers = _handlers.GetOrAdd(typeof(T), _ => new());
		Task wrapper(IEvent @event, CancellationToken ct) => handler((T)@event, ct);
		handlers.TryAdd(id, wrapper);
		return id;
	}

	public void Unsubscribe(Guid id)
	{
		foreach (var handler in _handlers.Values)
		{
			if (handler.Remove(id, out var _))
			{
				return;
			}
		}
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Event bus started");
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				var item = await _events.Reader.ReadAsync(stoppingToken);
				if (!_handlers.TryGetValue(item.GetType(), out var handlers))
				{
					_logger.LogInformation("No event listeners subscribed to {Type}", item.GetType());
					continue;
				}

				foreach (var handler in handlers.Values.ToArray())
				{
					await handler(item, stoppingToken);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("Exception caught: {Message}", ex.Message);
			}
		}
	}
}