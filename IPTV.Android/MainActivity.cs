using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using IPTV.Android.Services;
using IPTV.Services.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace IPTV.Android;

/// <summary>
/// Interface for activities that support the intent-based file picker fallback.
/// </summary>
public interface FilePickerActivity
{
    const int PickFileRequestCode = 9001;
    void SetPendingResult(TaskCompletionSource<string?> tcs);
}

[Activity(
    Label = "IPTV",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity, FilePickerActivity
{
    private TaskCompletionSource<string?>? _pendingFilePick;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        App.ConfigureServices(services =>
        {
            services.AddSingleton<IPlatformService>(new AndroidPlatformService(this));
        });

        base.OnCreate(savedInstanceState);
    }

    public void SetPendingResult(TaskCompletionSource<string?> tcs)
    {
        _pendingFilePick = tcs;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode == FilePickerActivity.PickFileRequestCode)
        {
            var tcs = _pendingFilePick;
            _pendingFilePick = null;

            if (tcs == null) return;

            if (resultCode == Result.Ok && data?.Data != null)
            {
                try
                {
                    // Read content from the content:// URI and save to local storage
                    var uri = data.Data;
                    using var inputStream = ContentResolver?.OpenInputStream(uri);
                    if (inputStream != null)
                    {
                        using var reader = new System.IO.StreamReader(inputStream);
                        var content = reader.ReadToEnd();

                        // Extract filename from URI or use a default
                        var fileName = GetFileName(uri) ?? "playlist.m3u";

                        var localDir = System.IO.Path.Combine(
                            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                            "imports");
                        System.IO.Directory.CreateDirectory(localDir);

                        var localPath = System.IO.Path.Combine(localDir, fileName);
                        System.IO.File.WriteAllText(localPath, content);

                        tcs.SetResult(localPath);
                    }
                    else
                    {
                        tcs.SetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainActivity] File pick error: {ex}");
                    tcs.SetResult(null);
                }
            }
            else
            {
                tcs.SetResult(null);
            }
        }
    }

    private string? GetFileName(global::Android.Net.Uri uri)
    {
        try
        {
            if (ContentResolver != null)
            {
                using var cursor = ContentResolver.Query(uri, null, null, null, null);
                if (cursor != null && cursor.MoveToFirst())
                {
                    var nameIndex = cursor.GetColumnIndex(global::Android.Provider.OpenableColumns.DisplayName);
                    if (nameIndex >= 0)
                    {
                        return cursor.GetString(nameIndex);
                    }
                }
            }
        }
        catch { }

        // Fallback: use last segment of URI
        return uri.LastPathSegment ?? "playlist.m3u";
    }
}
