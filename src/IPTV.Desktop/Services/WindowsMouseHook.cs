using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace IPTV.Desktop.Services;

public class WindowsMouseHook : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEMOVE = 0x0200;
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
    private static extern IntPtr WindowFromPoint(POINT Point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    private readonly Func<bool> _isSidebarOpen;
    private readonly Func<bool> _areControlsShowing;
    private readonly Action _onPointerMoved;
    private readonly Action _onVideoClicked;

    private readonly LowLevelMouseProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private int _lastX = -1;
    private int _lastY = -1;

    public WindowsMouseHook(
        Func<bool> isSidebarOpen,
        Func<bool> areControlsShowing,
        Action onPointerMoved,
        Action onVideoClicked)
    {
        _isSidebarOpen = isSidebarOpen;
        _areControlsShowing = areControlsShowing;
        _onPointerMoved = onPointerMoved;
        _onVideoClicked = onVideoClicked;

        _proc = HookCallback;
        try
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule?.ModuleName), 0);
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
            if (msg == WM_MOUSEMOVE || msg == WM_LBUTTONDOWN)
            {
                try
                {
                    var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    HandleMouseEvent(msg, hookStruct.pt);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WindowsMouseHook] Error processing mouse event: {ex.Message}");
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
        var ptWindow = WindowFromPoint(screenPt);

        // Verify if pointer is over this window or any of its native child controls (e.g. VLC HWND)
        if (ptWindow != hwnd && GetAncestor(ptWindow, GA_ROOT) != hwnd)
            return;

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

        bool controlsShowing = _areControlsShowing();
        bool sidebarOpen = _isSidebarOpen();

        double topBarLimit = 52;
        double bottomBarLimit = controlsShowing ? (dipHeight - 88) : dipHeight;
        double rightLimit = sidebarOpen ? (dipWidth - 320) : dipWidth;

        bool isOverVideoArea = dipY >= topBarLimit && dipY <= bottomBarLimit && dipX >= 0 && dipX <= rightLimit;

        if (msg == WM_MOUSEMOVE)
        {
            if (Math.Abs(screenPt.x - _lastX) > 2 || Math.Abs(screenPt.y - _lastY) > 2)
            {
                _lastX = screenPt.x;
                _lastY = screenPt.y;
                Avalonia.Threading.Dispatcher.UIThread.Post(_onPointerMoved);
            }
        }
        else if (msg == WM_LBUTTONDOWN)
        {
            if (isOverVideoArea)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(_onVideoClicked);
            }
        }
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
