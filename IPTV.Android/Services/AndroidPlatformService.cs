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
    private readonly Activity _activity;

    public AndroidPlatformService(Activity activity)
    {
        _activity = activity;
    }

    public override async Task<string?> PickFileAsync()
    {
        try
        {
            // Try Avalonia's StorageProvider first (works when we can get a TopLevel)
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
                            MimeTypes = new[] { "audio/x-mpegurl", "application/vnd.apple.mpegurl", "audio/mpegurl" }
                        },
                        FilePickerFileTypes.All
                    }
                });

                if (files != null && files.Count > 0)
                {
                    var file = files[0];

                    // On Android, content:// URIs can't be read by File.ReadAllText.
                    // Copy the file content to local storage.
                    await using var stream = await file.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();

                    var localDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IPTV", "imports");
                    Directory.CreateDirectory(localDir);

                    var localPath = Path.Combine(localDir, file.Name);
                    await File.WriteAllTextAsync(localPath, content);
                    return localPath;
                }
            }
            else
            {
                // Fallback: Use Android's native ACTION_OPEN_DOCUMENT intent
                return await PickFileViaIntentAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AndroidPlatformService] PickFileAsync error: {ex}");
        }

        return null;
    }

    private Task<string?> PickFileViaIntentAsync()
    {
        var tcs = new TaskCompletionSource<string?>();

        try
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            intent.PutExtra(Intent.ExtraMimeTypes, new[] {
                "audio/x-mpegurl", 
                "application/vnd.apple.mpegurl", 
                "audio/mpegurl",
                "application/octet-stream"
            });

            if (_activity is FilePickerActivity pickerActivity)
            {
                pickerActivity.SetPendingResult(tcs);
                _activity.StartActivityForResult(intent, FilePickerActivity.PickFileRequestCode);
            }
            else
            {
                // If the Activity doesn't support our callback, resolve with null
                tcs.SetResult(null);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AndroidPlatformService] Intent picker error: {ex}");
            tcs.SetResult(null);
        }

        return tcs.Task;
    }

    public override void SetOrientation(bool isLandscape)
    {
        _activity.RunOnUiThread(() =>
        {
            _activity.RequestedOrientation = isLandscape 
                ? ScreenOrientation.Landscape 
                : ScreenOrientation.Unspecified;
        });
    }

    public override void SetFullscreen(bool isFullscreen)
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

    public override bool CanChangeOrientation => true;
}
