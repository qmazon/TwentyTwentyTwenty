using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using log4net;
using Microsoft.Win32;
using TwentyTwentyTwenty.Data;
using TwentyTwentyTwenty.Util;
using Timer = System.Timers.Timer;

namespace TwentyTwentyTwenty;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : INotifyPropertyChanged
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(App));
    public static App CurrentApp => (App)Current;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

    private static Mutex? _mutex;
    private static bool _mutexOwned;
    private TaskbarIcon? _tray;
    private string _toolTipText = "20-20-20";

    private AppTimeController _controller = null!;

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

    private AppSettingsRecord Settings { get; set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        Settings = AppSettingsLoader.Load(out var errors);
        if (errors.Count != 0)
        {
            MessageBox.Show(string.Join('\n', errors), "配置错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        _mutex = new Mutex(true, "TwentyTwentyTwenty_SingleInstance", out var createdNew);
        _mutexOwned = createdNew;
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);
        // BasicConfigurator.Configure();
        Log.Info("App Starts.");
        // 托盘
        _tray = (TaskbarIcon)FindResource("TrayIcon")!;
        _tray.Visibility = Visibility.Visible;

        SystemEvents.SessionSwitch += OnSessionSwitch;
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _controller = new AppTimeController(Settings);
        _controller.TrayTickChanged += span => ToolTipText = $"20-20-20\n剩余时间：{span:mm\\:ss}";
        _controller.Restart();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Log.Info("Exit app.");
        _controller.Dispose();
        _tray?.Dispose();
        Shutdown();
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        switch (e.Reason)
        {
            case SessionSwitchReason.SessionLock:
                Log.Info("Workstation locked – pausing timer.");
                _controller.Abort();
                break;
            case SessionSwitchReason.SessionUnlock:
                Log.Info("Workstation unlocked – resuming timer.");
                _controller.Restart();
                break;
            case SessionSwitchReason.ConsoleConnect:
            case SessionSwitchReason.ConsoleDisconnect:
            case SessionSwitchReason.RemoteConnect:
            case SessionSwitchReason.RemoteDisconnect:
            case SessionSwitchReason.SessionLogon:
            case SessionSwitchReason.SessionLogoff:
            case SessionSwitchReason.SessionRemoteControl:
            default:
                return;
        }
    }

    private class ResetAndShowCommand : ICommand
    {
        public bool CanExecute(object? parameter) => AppTimeController.CanShow();
        public void Execute(object? parameter) => CurrentApp.ResetAndShow();
        public event EventHandler? CanExecuteChanged;
    }

    public static ICommand ShowWindowCommand { get; } = new ResetAndShowCommand();

    private void ResetAndShow() => _controller.ShowNow();

    private static string AppVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version!;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public static string AppVersionFormatted => $"版本{AppVersion}";

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error($"Unhandled Exception: {e.Exception}");
    }

    private void ReleaseMutex()
    {
        SystemEvents.SessionSwitch -= OnSessionSwitch;

        if (_mutexOwned)
        {
            _mutex!.ReleaseMutex();
            _mutexOwned = false;
        }

        _mutex?.Dispose();
    }

    private void App_OnExit(object sender, ExitEventArgs e)
    {
        Log.Info("App OnExit");
        ReleaseMutex();
    }

    private void Config_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppSettingsLoader.FilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开文件失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void About_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/qmazon/TwentyTwentyTwenty",
            UseShellExecute = true
        });
    }

    private void Restart_OnClick(object sender, RoutedEventArgs e)
    {
        var currentExePath = Environment.ProcessPath!;
        try
        {
            ReleaseMutex();
            Process.Start(currentExePath);
            Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"重启应用程序时出错: {ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}