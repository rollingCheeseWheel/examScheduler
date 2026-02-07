using System.Diagnostics.CodeAnalysis;

namespace Util.DataStructures;

public class TimestampedQueue<T>
{
	private readonly PriorityQueue<(DateTimeOffset timestamp, T item), DateTimeOffset> _queue = new();
	private readonly SemaphoreSlim _signal = new(0);
	private readonly object _lock = new();

	public void Enqueue(DateTimeOffset timestampUtc, T item)
	{
		lock (_lock)
		{
			_queue.Enqueue((timestampUtc, item), timestampUtc);
		}

		_signal.Release();
	}

	public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default)
	{
		while (true)
		{
			await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);

			lock (_lock)
			{
				if (_queue.Count == 0)
					continue;

				var (timestamp, item) = _queue.Peek();

				if (timestamp <= DateTime.UtcNow)
				{
					_queue.Dequeue();
					return item;
				}

				_signal.Release();
			}

			var delay = _queue.Peek().timestamp - DateTime.UtcNow;
			if (delay > TimeSpan.Zero)
				await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
		}
	}

	public bool TryDequeue([NotNullWhen(true)] out T item)
	{
		lock (_lock)
		{
			if (_queue.Count == 0)
			{
				item = default!;
				return false;
			}

			var (timestamp, value) = _queue.Peek();
			if (timestamp > DateTime.UtcNow || value is null)
			{
				item = default!;
				return false;
			}

			_queue.Dequeue();
			item = value;
			return true;
		}
	}
}
