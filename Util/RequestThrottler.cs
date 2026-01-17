using System.Diagnostics;

namespace Util;

public class RequestThrottler(double actionsPerSecond) : IDisposable
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(1d / actionsPerSecond);
    private readonly Stopwatch _stopWatch = Stopwatch.StartNew();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task WaitAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var timeToWait = _interval - _stopWatch.Elapsed;
            timeToWait = timeToWait >= TimeSpan.Zero ? timeToWait : TimeSpan.Zero;
            await Task.Delay(timeToWait, ct);
            _stopWatch.Restart();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _semaphore.Dispose();
    }
    ~RequestThrottler() => _semaphore.Dispose();
}
