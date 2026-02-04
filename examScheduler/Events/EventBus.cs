using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace examScheduler.Events;

public interface IEvent;

public interface IEventBus
{
	Task PublishAsync(IEvent @event, CancellationToken ct = default);
	Guid Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : IEvent;
	void Unsubscribe(Guid? id);
}

public sealed class EventBus : IEventBus
{
	private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, Func<IEvent, CancellationToken, Task>>> _handlers = new();

	public async Task PublishAsync(IEvent @event, CancellationToken ct = default)
	{
		if (!_handlers.TryGetValue(@event.GetType(), out var handlers))
		{
			return;
		}

		foreach (var handler in handlers.Values.ToArray())
		{
			await handler(@event, ct);
		}
	}

	public Guid Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : IEvent
	{
		var id = Guid.NewGuid();
		var handlers = _handlers.GetOrAdd(typeof(T), _ => new());
		Task wrapper(IEvent @event, CancellationToken ct) => handler((T)@event, ct);
		handlers.TryAdd(id, wrapper);
		return id;
	}

	public void Unsubscribe(Guid? id)
	{
		if (!id.HasValue)
		{
			return;
		}

		foreach (var handler in _handlers.Values)
		{
			if (handler.Remove(id.Value, out var _))
			{
				return;
			}
		}
	}
}