using Android.App;
using Android.Content;
using Android.Content.PM;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using IPTV.Services.Platform;

namespace IPTV.Android.Services;

public class AndroidPlatformService : DefaultPlatformService
{
    private Activity _activity;

    public AndroidPlatformService(Activity activity)
    {
        _activity = activity;
    }

    public void UpdateActivity(Activity activity)
    {
        _activity = activity;
    }

    public override async Task<string?> PickFileAsync()
    {
        try
        {
            // 1. Try Avalonia's StorageProvider first (cross-platform, handles SAF properly)
            var topLevel = GetCurrentTopLevel();
            if (topLevel?.StorageProvider != null && topLevel.StorageProvider.CanOpen)
            {
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select M3U Playlist File",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        FilePickerFileTypes.All,
                        new FilePickerFileType("Playlist (*.m3u;*.m3u8)")
                        {
                            Patterns = new[] { "*.m3u", "*.m3u8" },
                            MimeTypes = new[] { "*/*" }
                        }
                    }
                });

                if (files != null && files.Count > 0)
                {
                    var file = files[0];
                    await using var stream = await file.OpenReadAsync();

                    var fileName = file.Name;
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        fileName = "playlist.m3u";
                    }

                    var localDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Personal), "imports");
                    Directory.CreateDirectory(localDir);

                    var localPath = Path.Combine(localDir, Path.GetFileName(fileName));
                    await using (var outputStream = File.Create(localPath))
                    {
                        await stream.CopyToAsync(outputStream);
                    }
                    return localPath;
                }

                // If user cancelled selection via Avalonia picker
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AndroidPlatformService] StorageProvider OpenFilePickerAsync error: {ex}");
        }

        // 2. Fallback: Use Android's native intent chooser
        return await PickFileViaIntentAsync();
    }

    private Task<string?> PickFileViaIntentAsync()
    {
        var tcs = new TaskCompletionSource<string?>();

        _activity.RunOnUiThread(() =>
        {
            try
            {
                if (_activity is FilePickerActivity pickerActivity)
                {
                    pickerActivity.SetPendingResult(tcs);

                    Intent intent;
                    try
                    {
                        intent = new Intent(Intent.ActionGetContent);
                        intent.SetType("*/*");
                        intent.AddCategory(Intent.CategoryOpenable);
                    }
                    catch
                    {
                        intent = new Intent(Intent.ActionOpenDocument);
                        intent.SetType("*/*");
                        intent.AddCategory(Intent.CategoryOpenable);
                    }

                    var chooser = Intent.CreateChooser(intent, "Select M3U Playlist");
                    _activity.StartActivityForResult(chooser, FilePickerActivity.PickFileRequestCode);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidPlatformService] Intent picker error: {ex}");
                tcs.TrySetResult(null);
            }
        });

        return tcs.Task;
    }

    public override void SetOrientation(bool isLandscape)
    {
        _activity.RunOnUiThread(() =>
        {
            // Use ScreenOrientation.Landscape / Portrait to force orientation change
            // even if system auto-rotate is toggled off by the user.
            _activity.RequestedOrientation = isLandscape 
                ? ScreenOrientation.Landscape 
                : ScreenOrientation.Portrait;
        });
    }

    public override void SetFullscreen(bool isFullscreen)
    {
        _activity.RunOnUiThread(() =>
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var controller = _activity.Window?.InsetsController;
                if (controller != null)
                {
                    if (isFullscreen)
                    {
                        controller.Hide(global::Android.Views.WindowInsets.Type.StatusBars() | global::Android.Views.WindowInsets.Type.NavigationBars());
                        controller.SystemBarsBehavior = (int)global::Android.Views.WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                    }
                    else
                    {
                        controller.Show(global::Android.Views.WindowInsets.Type.StatusBars() | global::Android.Views.WindowInsets.Type.NavigationBars());
                    }
                }
            }
            else
            {
                if (isFullscreen)
                {
                    _activity.Window?.AddFlags(global::Android.Views.WindowManagerFlags.Fullscreen);
                }
                else
                {
                    _activity.Window?.ClearFlags(global::Android.Views.WindowManagerFlags.Fullscreen);
                }
            }
        });
    }

    public override bool CanChangeOrientation => true;

    public override Control? CreateVideoView(object mediaPlayer)
    {
        if (mediaPlayer is LibVLCSharp.Shared.MediaPlayer mp)
        {
            return new AndroidVideoViewHost(mp, _activity);
        }
        return null;
    }
}
