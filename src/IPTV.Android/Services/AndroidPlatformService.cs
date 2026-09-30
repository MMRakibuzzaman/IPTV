using Android.App;
using Android.Content.PM;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using IPTV.Services.Platform;

namespace IPTV.Android.Services;

public class AndroidPlatformService : IPlatformService
{
    private readonly Activity _activity;

    public AndroidPlatformService(Activity activity)
    {
        _activity = activity;
    }

    public async Task<string?> PickFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(null);
        if (topLevel?.StorageProvider != null)
        {
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
        }

        return null;
    }

    public void SetOrientation(bool isLandscape)
    {
        _activity.RunOnUiThread(() =>
        {
            _activity.RequestedOrientation = isLandscape 
                ? ScreenOrientation.Landscape 
                : ScreenOrientation.Unspecified;
        });
    }

    public void SetFullscreen(bool isFullscreen)
    {
        _activity.RunOnUiThread(() =>
        {
            if (isFullscreen)
            {
                _activity.Window?.AddFlags(global::Android.Views.WindowManagerFlags.Fullscreen);
            }
            else
            {
                _activity.Window?.ClearFlags(global::Android.Views.WindowManagerFlags.Fullscreen);
            }
        });
    }

    public bool CanChangeOrientation => true;
}
