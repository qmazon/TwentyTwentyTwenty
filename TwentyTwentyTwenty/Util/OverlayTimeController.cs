using System.Windows;
using System.Windows.Media;
using log4net;
using TwentyTwentyTwenty.Data;
using TwentyTwentyTwenty.Overlay;

namespace TwentyTwentyTwenty.Util;

public sealed class OverlayTimeController : IDisposable, IAsyncDisposable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayTimeController));

    private readonly AppSettingsRecord _settings;

    public event Action<OverlayWindow.FinishReason> OverlayFinished = _ => { };

    private readonly CountdownTimer _overlayWatch;

    private OverlayWindow? Window { get; set; }

    public OverlayTimeController(AppSettingsRecord settings)
    {
        _settings = settings;

        _overlayWatch = new CountdownTimer(
            settings.RestTime.ToTimeSpan(),
            TimeSpan.FromSeconds(1.0)
        );
        _overlayWatch.Elapsed += span => SetOverlayTick(Convert.ToInt32(Math.Round(span.TotalSeconds)));
        _overlayWatch.Finished += OnOverlayWatchFinished;
    }

    /// <summary>
    /// 无须在UI线程上调用此方法
    /// </summary>
    public void ShowOverlay()
    {
        Log.Info("ShowOverlay Called.");
        Application.Current.Dispatcher.Invoke(() =>
        {
            Window?.Close();
            Window = new OverlayWindow(_settings);
            Window.FadeInCompleted += (_, _) =>
            {
                Log.Info("FadeIn Completed.");
                _overlayWatch.Start();
            };
            Window.FadeOutCompleted += (_, _) =>
            {
                Log.Info($"Window Closed. Reason: {Window.Reason}.");
                Window.Close();
                OverlayFinished(Window.Reason);
            };
            Window.Show();
        });
    }

    private void SetOverlayTick(int tick)
    {
        // 用户强行取消了本次休息
        if (Window!.Reason == OverlayWindow.FinishReason.Forced)
        {
            _overlayWatch.Cancel();
            return;
        }

        Log.Debug($"SetTick: {tick}");
        Window!.Dispatcher.Invoke(() => { Window!.CountText.Text = $"{tick:d2}"; });
    }

    private void OnOverlayWatchFinished()
    {
        Log.Debug($"Window should fade out.");
        Window!.FadeOutStarted = true;
        Window!.Dispatcher.InvokeAsync(async () =>
        {
            Window.CountText.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, Window.AnimationOnSuccess);
            Window.CountText.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, Window.AnimationOnSuccess);
            await Task.Delay(_settings.RestFinishedColorChangeTime.ToTimeSpan());
            Window.BeginAnimation(UIElement.OpacityProperty, Window.FadeOut);
        });
        // 之后会调用FadeOutCompleted
    }

    public void Dispose()
    {
        _overlayWatch.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _overlayWatch.Dispose();
        return ValueTask.CompletedTask;
    }
}