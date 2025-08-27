using System.Windows;
using System.Timers;
using log4net;
using Timer = System.Timers.Timer;

namespace TwentyTwentyTwenty.Util;

public class CountdownTimer : IDisposable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(CountdownTimer));
    
    private readonly TimeSpan _total;
    private readonly TimeSpan _interval;
    private Timer? _timer;
    private DateTime _startTime;
    private int _expectedElapsedCount;
    private int _currentElapsedCount;
    private readonly Lock _lock = new();

    public event Action? Finished;
    public event Action<TimeSpan>? Elapsed;

    public CountdownTimer(TimeSpan total, TimeSpan interval)
    {
        if (total <= TimeSpan.Zero)
            throw new ArgumentException("Total time must be positive", nameof(total));

        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("Interval must be positive", nameof(interval));

        _total = total;
        _interval = interval;

        // 计算预期的触发次数
        _expectedElapsedCount = (int)Math.Ceiling(total / interval);
    }

    public void Start()
    {
        lock (_lock)
        {
            Cancel();

            _currentElapsedCount = 0;
            _timer = new Timer(_interval.TotalMilliseconds);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            
            _startTime = DateTime.UtcNow;
            _timer.Start();
        }
    }

    public void Cancel()
    {
        lock (_lock)
        {
            if (_timer == null) return;
            _timer.Stop();
            _timer.Elapsed -= OnTimerElapsed;
            _timer.Dispose();
            _timer = null;
        }
    }

    public void Restart()
    {
        Cancel();
        Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            if (_timer == null) return;

            _currentElapsedCount++;
            var elapsedTime = DateTime.UtcNow - _startTime;
            var remainingTime = _total - elapsedTime;

            // 确保不会出现负数
            if (remainingTime < TimeSpan.Zero)
                remainingTime = TimeSpan.Zero;
            
            Log.Debug($"RemainingTime: {remainingTime.Milliseconds}ms");

            // 触发 Elapsed 事件
            Elapsed?.Invoke(remainingTime);

            // 检查是否应该结束
            if (_currentElapsedCount < _expectedElapsedCount && remainingTime > TimeSpan.Zero) return;
            Cancel();
            Finished?.Invoke();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _timer?.Dispose();
    }
}