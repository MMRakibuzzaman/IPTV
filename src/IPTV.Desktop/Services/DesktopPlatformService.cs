using Avalonia.Controls;
using Avalonia.Platform.Storage;
using IPTV.Services.Platform;

namespace IPTV.Desktop.Services;

public class DesktopPlatformService : IPlatformService
{
    private readonly Window _window;

    public DesktopPlatformService(Window window)
    {
        _window = window;
    }

    public async Task<string?> PickFileAsync()
    {
        if (_window.StorageProvider == null) return null;

        var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select M3U Playlist File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("M3U Playlist (*.m3u;*.m3u8)")
                {
                    Patterns = new[] { "*.m3u", "*.m3u8" }
                },
                FilePickerFileTypes.All
            }
        });

        if (files != null && files.Count > 0)
        {
            return files[0].Path.LocalPath;
        }

        return null;
    }

    public void SetOrientation(bool isLandscape)
    {
        // Desktop window orientation - on desktop we can resize or adjust window aspect if desired
    }

    public void SetFullscreen(bool isFullscreen)
    {
        _window.WindowState = isFullscreen ? WindowState.FullScreen : WindowState.Normal;
    }

    public bool CanChangeOrientation => false;
}
