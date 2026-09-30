using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Foundation;
using IPTV.Services.Platform;
using UIKit;

namespace IPTV.iOS.Services;

public class IosPlatformService : DefaultPlatformService
{
    public override async Task<string?> PickFileAsync()
    {
        try
        {
            var topLevel = GetCurrentTopLevel();
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
                            Patterns = new[] { "*.m3u", "*.m3u8" },
                            MimeTypes = new[] { "audio/x-mpegurl", "application/vnd.apple.mpegurl" }
                        },
                        FilePickerFileTypes.All
                    }
                });

                if (files != null && files.Count > 0)
                {
                    var file = files[0];

                    // On iOS, the file URI may not be directly accessible.
                    // Read via stream and copy to local app storage.
                    await using var stream = await file.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();

                    var localDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "IPTV", "imports");
                    Directory.CreateDirectory(localDir);

                    var localPath = Path.Combine(localDir, file.Name);
                    await File.WriteAllTextAsync(localPath, content);
                    return localPath;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IosPlatformService] PickFileAsync error: {ex}");
        }

        return null;
    }

    public override void SetOrientation(bool isLandscape)
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

    public override void SetFullscreen(bool isFullscreen)
    {
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            UIApplication.SharedApplication.SetStatusBarHidden(isFullscreen, UIStatusBarAnimation.Fade);
        });
    }

    public override bool CanChangeOrientation => true;
}
