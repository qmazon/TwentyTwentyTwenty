using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using log4net;
using log4net.Config;
using TwentyTwentyTwenty.Data;
using TwentyTwentyTwenty.Overlay;
using Timer = System.Timers.Timer;

namespace TwentyTwentyTwenty;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : INotifyPropertyChanged
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(App));
    private static OverlayWindow? _window;
    public static App CurrentApp => (App)Current;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    
    private static Mutex? _mutex;
    private DateTime _nextTick;
    private TaskbarIcon? _tray;
    private Timer? _timer;
    private readonly AppSettings _settings = AppSettings.Load();
    
    private string _toolTipText = "20-20-20";

    public string ToolTipText
    {
        get => _toolTipText;
        private set
        {
            if (_toolTipText == value) return;
            _toolTipText = value; 
            OnPropertyChanged();
        }
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "TwentyTwentyTwenty_SingleInstance", out var createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }
        
        base.OnStartup(e);
        BasicConfigurator.Configure();
        Log.Info("App Starts.");
        // 托盘
        _tray = (TaskbarIcon)FindResource("TrayIcon")!;
        _tray.Visibility = Visibility.Visible;

        _timer = new Timer(TimeSpan.FromMinutes(_settings.IntervalMinutes));
        _timer.Elapsed += (_, _) => Dispatcher.Invoke(ResetAndShow);
        // 1 秒更新 1 次托盘。timer 被 Dispatcher 引用，不会被 GC
        _ = new DispatcherTimer(
            TimeSpan.FromSeconds(1),
            DispatcherPriority.Normal,
            (_, _) => UpdateToolTip(),
            Dispatcher);
        ResetTimer();

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    private static void ShowOverlay()
    {
        Log.Info("Method: ShowOverlay");
        _window?.Close();
        _window = new OverlayWindow(CurrentApp._settings);
        _window.Show();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Log.Info("Exit app.");
        _timer?.Stop();
        _tray?.Dispose();
        Shutdown();
    }

    private void ResetTimer()
    {
        _nextTick = DateTime.Now.AddMilliseconds(_timer!.Interval);
        _timer.Start();
    }
    
    private void UpdateToolTip()
    {
        var left = _nextTick - DateTime.Now;
        if (left < TimeSpan.Zero) left = TimeSpan.Zero;
        ToolTipText = $"20-20-20\n剩余时间：{left:mm\\:ss}";
    }
    
    private class ResetAndShowCommand : ICommand
    {
        // null or false is accepted
        public bool CanExecute(object? parameter)
        {
            var instanceIsVisible = OverlayWindow.Instance?.IsVisible;
            Log.Info($"Check if able to show (not true for OK): instanceIsVisible is {(object?)instanceIsVisible ?? "Null"}");
            
            return instanceIsVisible != true;
        }

        public void Execute(object? parameter)
        {
            CurrentApp.ResetAndShow();
        }

        public event EventHandler? CanExecuteChanged;
    }
    
    public static ICommand ShowWindowCommand { get; } = new ResetAndShowCommand();
    private void ResetAndShow()
    {
        // 1. 计时器归零
        _timer?.Stop();
        ResetTimer();

        // 2. 立刻弹出窗口
        ShowOverlay();
    }

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error($"Unhandled Exception: {e.Exception}");
    }

    private void App_OnExit(object sender, ExitEventArgs e)
    {
        _mutex!.ReleaseMutex();
        _mutex.Dispose();
        base.OnExit(e);
    }
}