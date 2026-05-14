namespace Util.DataStructures;

public class TimestampedQueue<T>
{
	private readonly PriorityQueue<T, DateTimeOffset> _queue = new();
	private readonly SemaphoreSlim _signal = new(0);
	private readonly Lock _lock = new();

	public void Enqueue(T item, double deferSeconds) => Enqueue(item, TimeSpan.FromSeconds(deferSeconds));
	public void Enqueue(T item, TimeSpan defer) => Enqueue(item, DateTimeOffset.UtcNow + defer);
	public void Enqueue(T item, DateTimeOffset deferUntil)
	{
		using (_lock.EnterScope())
		{
			_queue.Enqueue(item, deferUntil);
		}

		_signal.Release();
	}

	public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default)
	{
		while (true)
		{
			_lock.Enter();
			if (_queue.TryPeek(out var item, out var timestamp))
			{
				if (timestamp <= DateTimeOffset.UtcNow)
				{
					_queue.Dequeue();
					_lock.Exit();
					return item;
				}
				_lock.Exit();

				var delay = timestamp - DateTimeOffset.UtcNow;
				if (delay > TimeSpan.Zero)
				{
					try
					{
						await _signal.WaitAsync(delay, cancellationToken);
					}
					catch (OperationCanceledException)
					{
						throw;
					}
				}
			}
			else
			{
				_lock.Exit();
				await _signal.WaitAsync(cancellationToken);
			}
		}
	}
}
