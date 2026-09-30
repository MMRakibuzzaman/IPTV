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
    private LibVLCSharp.Platforms.Android.VideoView? _videoView;

    public AndroidVideoViewHost(MediaPlayer mediaPlayer)
    {
        _mediaPlayer = mediaPlayer;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        try
        {
            var context = (parent as AndroidViewControlHandle)?.View.Context 
                ?? global::Android.App.Application.Context;

            _videoView = new LibVLCSharp.Platforms.Android.VideoView(context);
            _videoView.MediaPlayer = _mediaPlayer;

            return new AndroidViewControlHandle(_videoView);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AndroidVideoViewHost] Error creating native control: {ex}");
            // Return an empty view if failed
            return new AndroidViewControlHandle(new global::Android.Views.View(global::Android.App.Application.Context));
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
