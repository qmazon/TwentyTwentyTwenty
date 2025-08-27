using log4net;
using Microsoft.VisualBasic.Logging;
using TwentyTwentyTwenty.Data;
using TwentyTwentyTwenty.Overlay;

namespace TwentyTwentyTwenty.Util;

public sealed class AppTimeController : IDisposable, IAsyncDisposable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(AppTimeController));
    
    public event Action<TimeSpan> TrayTickChanged = _ => { };

    private readonly AppSettings _settings;
    private readonly CountdownTimer _restWatch;
    private readonly CountdownTimer _escapeWatch;

    public AppTimeController(AppSettings settings)
    {
        _settings = settings;
        _restWatch = new CountdownTimer(
            TimeSpan.FromMinutes(settings.IntervalMinutes),
            TimeSpan.FromSeconds(1.0)
        );
        _restWatch.Elapsed += SetTrayTick;
        _restWatch.Finished += ShowOverlay;

        _escapeWatch = new CountdownTimer(
            TimeSpan.FromMinutes(settings.EscapeNextMinutes),
            TimeSpan.FromSeconds(1.0)
        );
        _escapeWatch.Elapsed += SetTrayTick;
        _escapeWatch.Finished += ShowOverlay;
    }

    public void Abort()
    {
        _restWatch.Cancel();
        _escapeWatch.Cancel();
    }

    public void Restart()
    {
        Abort();
        _restWatch.Restart();
    }
    
    public static bool CanShow()
    {
        var instanceIsVisible = OverlayWindow.Instance?.IsVisible ?? false;
        return !instanceIsVisible;
    }

    public void ShowNow()
    {
        if (!CanShow()) return;
        
        Abort();
        ShowOverlay();
    }

    private void ShowOverlay()
    {
        using var controller = new OverlayTimeController(_settings);
        controller.OverlayFinished += reason =>
        {
            switch (reason)
            {
                case OverlayWindow.FinishReason.Normal:
                    Restart();
                    break;
                case OverlayWindow.FinishReason.Forced:
                    Abort();
                    _escapeWatch.Restart();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
            }
        };
        controller.ShowOverlay();
    }
    
    private void SetTrayTick(TimeSpan span) => TrayTickChanged.Invoke(span);

    #region Dispose


    public void Dispose()
    {
        _restWatch.Dispose();
        _escapeWatch.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(_restWatch);
        await CastAndDispose(_escapeWatch);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
    
    #endregion
}