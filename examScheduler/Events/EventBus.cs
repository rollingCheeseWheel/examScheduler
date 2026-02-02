using System;
using System.Collections.Concurrent;

namespace examScheduler.Events;

public interface IEvent;

public interface IEventBus
{
	Task PublishAsync(IEvent @event, CancellationToken ct = default);
	void Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : IEvent;
}

public sealed class EventBus : IEventBus
{
	private readonly ConcurrentDictionary<Type, List<Func<IEvent, CancellationToken, Task>>> _handlers = new();

	public async Task PublishAsync(IEvent @event, CancellationToken ct = default)
	{
		if (!_handlers.TryGetValue(@event.GetType(), out var handlers))
		{
			return;
		}

		Func<IEvent, CancellationToken, Task>[ ] snapshot;
		lock (handlers)
		{
			snapshot = [ .. handlers ];
		}

		foreach (var handler in snapshot)
		{
			await handler(@event, ct);
		}
	}

	public void Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : IEvent
	{
		var list = _handlers.GetOrAdd(typeof(T), []);
		Task wrapper(IEvent @event, CancellationToken ct) => handler((T)@event, ct);
		lock (list)
		{
			list.Add(wrapper);
		}
	}
}