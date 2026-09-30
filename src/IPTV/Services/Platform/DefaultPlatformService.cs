using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace IPTV.Services.Platform;

public class DefaultPlatformService : IPlatformService
{
    public virtual async Task<string?> PickFileAsync()
    {
        var topLevel = GetCurrentTopLevel();
        if (topLevel?.StorageProvider == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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

    public virtual void SetOrientation(bool isLandscape)
    {
        // Desktop / Default does not force physical screen rotation
    }

    public virtual void SetFullscreen(bool isFullscreen)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow != null)
                {
                    desktop.MainWindow.WindowState = isFullscreen ? WindowState.FullScreen : WindowState.Normal;
                }
            }
        });
    }

    public virtual bool CanChangeOrientation => false;

    public virtual IDisposable? InstallPlayerPointerHook(
        Func<bool> isSidebarOpen,
        Func<bool> areControlsShowing,
        Action onPointerMoved,
        Action onVideoClicked,
        Action onChannelsClicked) => null;

    protected static TopLevel? GetCurrentTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        else if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            return TopLevel.GetTopLevel(singleView.MainView);
        }
        return null;
    }
}
