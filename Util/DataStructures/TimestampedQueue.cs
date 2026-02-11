namespace Util.DataStructures;

public class TimestampedQueue<T>
{
	private readonly PriorityQueue<(DateTimeOffset Timestamp, T Item), DateTimeOffset> _queue = new();
	private readonly SemaphoreSlim _signal = new(0);
	private readonly object _lock = new();
	private CancellationTokenSource _queueAddedCts = new();

	public void Enqueue(DateTimeOffset timestampUtc, T item)
	{
		lock (_lock)
		{
			_queue.Enqueue((timestampUtc, item), timestampUtc);
			CancelAndResetToken();
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
			}

			using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _queueAddedCts.Token);
			var delay = _queue.Peek().Timestamp - DateTime.UtcNow;
			if (delay > TimeSpan.Zero)
			{
				try
				{
					await Task.Delay(delay, linkedTokenSource.Token).ConfigureAwait(false);
				}
				catch
				{

				}
			}
		}
	}

	private void CancelAndResetToken()
	{
		_queueAddedCts.Cancel();
		_queueAddedCts.Dispose();
		_queueAddedCts = new();
	}
}
