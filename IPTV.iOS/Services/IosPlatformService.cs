using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Foundation;
using IPTV.Services.Platform;
using UIKit;

namespace IPTV.iOS.Services;

public class IosPlatformService : IPlatformService
{
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
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            try
            {
                if (UIDevice.CurrentDevice.CheckSystemVersion(16, 0))
                {
                    var scene = UIApplication.SharedApplication.ConnectedScenes.ToArray().FirstOrDefault() as UIWindowScene;
                    if (scene != null)
                    {
                        var orientationMask = isLandscape 
                            ? UIInterfaceOrientationMask.LandscapeRight 
                            : UIInterfaceOrientationMask.Portrait;
                        var geometryPreferences = new UIWindowSceneGeometryPreferencesIOS(orientationMask);
                        scene.RequestGeometryUpdate(geometryPreferences, error => { });
                    }
                }
                else
                {
                    var orientation = isLandscape ? UIInterfaceOrientation.LandscapeRight : UIInterfaceOrientation.Portrait;
                    UIDevice.CurrentDevice.SetValueForKey(NSNumber.FromNInt((int)orientation), new NSString("orientation"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IosPlatformService] Error setting orientation: {ex.Message}");
            }
        });
    }

    public void SetFullscreen(bool isFullscreen)
    {
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            UIApplication.SharedApplication.SetStatusBarHidden(isFullscreen, UIStatusBarAnimation.Fade);
        });
    }

    public bool CanChangeOrientation => true;
}
