using System;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Android;
using LibVLCSharp.Platforms.Android;
using LibVLCSharp.Shared;

namespace IPTV.Android.Services;

public class AndroidVideoViewHost : NativeControlHost
{
    private readonly MediaPlayer _mediaPlayer;
    private readonly global::Android.App.Activity _activity;
    private LibVLCSharp.Platforms.Android.VideoView? _videoView;

    public AndroidVideoViewHost(MediaPlayer mediaPlayer, global::Android.App.Activity activity)
    {
        _mediaPlayer = mediaPlayer;
        _activity = activity;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        try
        {
            var context = (parent as AndroidViewControlHandle)?.View.Context ?? _activity;

            _videoView = new LibVLCSharp.Platforms.Android.VideoView(context)
            {
                Clickable = false,
                Focusable = false
            };
            _videoView.MediaPlayer = _mediaPlayer;

            return new AndroidViewControlHandle(_videoView);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AndroidVideoViewHost] Error creating native control: {ex}");
            // Return an empty view if failed
            return new AndroidViewControlHandle(new global::Android.Views.View(_activity));
        }
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (_videoView != null)
        {
            _videoView.MediaPlayer = null;
            _videoView.Dispose();
            _videoView = null;
        }

        base.DestroyNativeControlCore(control);
    }
}
