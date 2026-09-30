using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace IPTV.Desktop.Services;

public class WindowsMouseHook : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const uint GA_ROOT = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    private readonly Func<bool> _isSidebarOpen;
    private readonly Func<bool> _areControlsShowing;
    private readonly Func<bool> _isFullscreen;
    private readonly Action _onPointerMoved;
    private readonly Action _onVideoClicked;
    private readonly Action _onChannelsClicked;

    private readonly LowLevelMouseProc _proc;
    private IntPtr _hookId = IntPtr.Zero;

    public WindowsMouseHook(
        Func<bool> isSidebarOpen,
        Func<bool> areControlsShowing,
        Func<bool> isFullscreen,
        Action onPointerMoved,
        Action onVideoClicked,
        Action onChannelsClicked)
    {
        _isSidebarOpen = isSidebarOpen;
        _areControlsShowing = areControlsShowing;
        _isFullscreen = isFullscreen;
        _onPointerMoved = onPointerMoved;
        _onVideoClicked = onVideoClicked;
        _onChannelsClicked = onChannelsClicked;

        _proc = HookCallback;
        try
        {
            var hMod = GetModuleHandle(null);
            _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, hMod, 0);
            if (_hookId == IntPtr.Zero)
            {
                var err = Marshal.GetLastWin32Error();
                Debug.WriteLine($"[WindowsMouseHook] Failed to set hook, error: {err}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowsMouseHook] Failed to set hook: {ex.Message}");
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && lParam != IntPtr.Zero)
        {
            var msg = wParam.ToInt32();
            if (msg == WM_LBUTTONDOWN)
            {
                try
                {
                    var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    HandleMouseEvent(msg, hookStruct.pt);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WindowsMouseHook] Error processing mouse click: {ex.Message}");
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void HandleMouseEvent(int msg, POINT screenPt)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var window = desktop.MainWindow;
        if (window == null || !window.IsVisible)
            return;

        var platformHandle = window.TryGetPlatformHandle();
        if (platformHandle == null)
            return;

        var hwnd = platformHandle.Handle;

        var fg = GetForegroundWindow();
        if (fg != hwnd && GetAncestor(fg, GA_ROOT) != hwnd)
            return;

        if (!GetWindowRect(hwnd, out var winRect))
            return;

        if (screenPt.x < winRect.Left || screenPt.x > winRect.Right ||
            screenPt.y < winRect.Top || screenPt.y > winRect.Bottom)
        {
            return;
        }

        var clientPt = screenPt;
        if (!ScreenToClient(hwnd, ref clientPt))
            return;

        double scale = window.RenderScaling;
        if (scale <= 0) scale = 1.0;

        double dipX = clientPt.x / scale;
        double dipY = clientPt.y / scale;
        double dipWidth = window.Bounds.Width;
        double dipHeight = window.Bounds.Height;

        // Is pointer within window client bounds?
        if (dipX < 0 || dipX > dipWidth || dipY < 0 || dipY > dipHeight)
            return;

        // Hide logic and click-to-toggle overlay are strictly for fullscreen mode only!
        bool isFullscreen = (_isFullscreen != null && _isFullscreen()) || (window.WindowState == WindowState.FullScreen);
        if (!isFullscreen)
        {
            return;
        }

        bool controlsShowing = _areControlsShowing();
        bool sidebarOpen = _isSidebarOpen();

        // If sidebar is currently open, clicks on the right sidebar area are handled by the sidebar
        if (sidebarOpen && dipX >= (dipWidth - 320))
        {
            return;
        }

        // If controls are currently showing:
        // Clicks in top bar (<= 52) and bottom bar (>= dipHeight - 88) are handled directly by Avalonia controls
        // (Back button, Channels button, Play, Volume, Quality, Aspect, Fullscreen).
        if (controlsShowing)
        {
            if (dipY <= 52 || dipY >= (dipHeight - 88))
            {
                return;
            }

            // Clicking over the video area while controls are showing hides them!
            Avalonia.Threading.Dispatcher.UIThread.Post(_onVideoClicked);
            return;
        }

        // If controls are currently HIDDEN:
        // 1. Tapping in the top-right corner opens the Channels sidebar
        if (dipY <= 60 && dipX >= (dipWidth - 150))
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(_onChannelsClicked);
            return;
        }

        // 2. Tapping anywhere else on the video brings up controls!
        Avalonia.Threading.Dispatcher.UIThread.Post(_onVideoClicked);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
}
