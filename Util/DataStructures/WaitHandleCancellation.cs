namespace Util.DataStructures;

public sealed class WaitHandleCancellation : IDisposable
{
	public CancellationToken Token => _cts.Token;

	private readonly CancellationTokenSource _cts = new();
	private readonly RegisteredWaitHandle _registration;

	public WaitHandleCancellation(WaitHandle waitHandle)
	{
		_registration = ThreadPool.RegisterWaitForSingleObject(
			waitHandle,
			static (state, _) =>
			{
				( (CancellationTokenSource)state! ).Cancel();
			},
			_cts,
			Timeout.Infinite,
			true);
	}

	public void Dispose()
	{
		_registration.Unregister(null);
		_cts.Dispose();
	}
}