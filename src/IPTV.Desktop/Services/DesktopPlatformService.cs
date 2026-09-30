using Avalonia.Controls;
using IPTV.Services.Platform;

namespace IPTV.Desktop.Services;

public class DesktopPlatformService : DefaultPlatformService
{
    private readonly Window? _window;

    public DesktopPlatformService(Window? window = null)
    {
        _window = window;
    }

    public override void SetFullscreen(bool isFullscreen)
    {
        if (_window != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _window.WindowState = isFullscreen ? WindowState.FullScreen : WindowState.Normal;
            });
        }
        else
        {
            base.SetFullscreen(isFullscreen);
        }
    }
}
