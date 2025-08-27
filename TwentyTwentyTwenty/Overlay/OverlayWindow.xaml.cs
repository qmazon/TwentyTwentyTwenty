using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using log4net;
using TwentyTwentyTwenty.Data;

namespace TwentyTwentyTwenty.Overlay;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class OverlayWindow
{
    public enum FinishReason
    {
        Normal,
        Forced
    }
    
    private readonly AppSettings _settings;

    #region Win32

    // ReSharper disable InconsistentNaming
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int GWL_EXSTYLE = -20;
    private const int VK_LCONTROL = 0xA2;
    private const int VK_RCONTROL = 0xA3;
    private const int VK_LMENU = 0xA4;
    private const int VK_RMENU = 0xA5;
    // ReSharper restore InconsistentNaming

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongA")]
    private static partial int GetWindowLong(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongA")]
    private static partial int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExA", SetLastError = true)]
    private static partial IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWindowsHookEx(IntPtr hhk);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleA", SetLastError = true,
        StringMarshalling = StringMarshalling.Custom,
        StringMarshallingCustomType = typeof(System.Runtime.InteropServices.Marshalling.AnsiStringMarshaller))]
    private static partial IntPtr GetModuleHandle(string lpModuleName);

    [LibraryImport("user32.dll")]
    private static partial short GetAsyncKeyState(int vKey);

    #endregion

    // ReSharper disable once InconsistentNaming
    private static IntPtr _hookID = IntPtr.Zero;
    internal static OverlayWindow? Instance; // 让钩子回调里能拿到窗口实例
    private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayWindow));

    // todo color
    internal static ColorAnimation CyanToGold { get; } = new(Colors.Cyan, Colors.Gold, TimeSpan.FromMilliseconds(500));
    
    private DoubleAnimation FadeIn { get; }
    internal DoubleAnimation FadeOut { get; }
    
    private Duration FadeInDuration => TimeSpan.FromSeconds(_settings.FadeInSeconds);
    private Duration FadeOutDuration => TimeSpan.FromSeconds(_settings.FadeOutSeconds);
    private int RestSeconds => _settings.RestSeconds;

    internal bool FadeOutStarted;
    internal FinishReason Reason { get; private set; } = FinishReason.Normal;
    public event EventHandler FadeInCompleted = (_, _) => { };
    public event EventHandler FadeOutCompleted = (_, _) => { };

    public OverlayWindow(AppSettings settings)
    {
        _settings = settings;
        
        InitializeComponent();
        Instance = this;

        Loaded += (_, _) => _hookID = SetHook(HookCallback);
        Closed += (_, _) =>
        {
            if (_hookID != IntPtr.Zero) UnhookWindowsHookEx(_hookID);
        };
        
        FadeIn = new DoubleAnimation
        {
            From = 0,
            To   = 1,
            Duration = FadeInDuration
        };
        
        FadeOut = new DoubleAnimation
        {
            From = 1,
            To   = 0,
            Duration = FadeOutDuration
        };
        
        Left   = SystemParameters.VirtualScreenLeft;
        Top    = SystemParameters.VirtualScreenTop;
        Width  = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Log.Info("Start the overlay.");

        // todo color
        CountText.Foreground = new SolidColorBrush(Colors.Cyan);
        CountText.Text = $"{RestSeconds:d2}";
        
        FadeIn.Completed += FadeInCompleted;
        FadeOut.Completed += FadeOutCompleted;
        BeginAnimation(OpacityProperty, FadeIn);
        // FadeIn.Begin(this);

        // 获取窗口句柄
        var hWnd = new WindowInteropHelper(this).Handle;
        // 设置窗口样式，使其透明且不响应鼠标和键盘事件
        var extendedStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
        var res = SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);
        if (res == 0) Log.Error($"SetWindowLong 失败，错误码：{Marshal.GetLastWin32Error()}");
    }

    #region KeyboardHook

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            return SetWindowsHookEx(WH_KEYBOARD_LL,
                proc,
                GetModuleHandle(curModule.ModuleName),
                0);
        }
    
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Log.Debug($"wParam: 0x{Convert.ToString(wParam, 16)}");
            if (nCode != 0 || wParam != WM_KEYDOWN) return CallNextHookEx(_hookID, nCode, wParam, lParam);
            
            var vkCode = Marshal.ReadInt32(lParam);
            var ctrl = vkCode == VK_LCONTROL || vkCode == VK_RCONTROL ||
                       (GetAsyncKeyState(VK_LCONTROL) & 0x8000) != 0 ||
                       (GetAsyncKeyState(VK_RCONTROL) & 0x8000) != 0;
            var alt = vkCode == VK_LMENU || vkCode == VK_RMENU ||
                (GetAsyncKeyState(VK_LMENU) & 0x8000) != 0 ||
                (GetAsyncKeyState(VK_RMENU) & 0x8000) != 0;
    
            if (!ctrl || !alt) return CallNextHookEx(_hookID, nCode, wParam, lParam);
            Log.Info("Press Ctrl+Alt. Now close the overlay.");
            // 回到 UI 线程关闭窗口
            Instance?.Dispatcher.Invoke(async () =>
            {
                if (Instance.FadeOutStarted) return;
                Instance.Reason = FinishReason.Forced;
                Instance.CountText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                await Task.Delay(800);
                
                Instance.FadeOutStarted = true;
                Instance.BeginAnimation(OpacityProperty, Instance.FadeOut);
                // Instance.FadeOut.Begin(Instance);
            });
            
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

    #endregion
}