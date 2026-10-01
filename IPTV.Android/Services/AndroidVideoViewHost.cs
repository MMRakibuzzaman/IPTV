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
    private readonly Action? _onTapped;
    private LibVLCSharp.Platforms.Android.VideoView? _videoView;

    public AndroidVideoViewHost(MediaPlayer mediaPlayer, global::Android.App.Activity activity, Action? onTapped = null)
    {
        _mediaPlayer = mediaPlayer;
        _activity = activity;
        _onTapped = onTapped;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        try
        {
            var context = (parent as AndroidViewControlHandle)?.View.Context ?? _activity;

            _videoView = new LibVLCSharp.Platforms.Android.VideoView(context)
            {
                Clickable = true,
                Focusable = false
            };
            _videoView.MediaPlayer = _mediaPlayer;

            if (_onTapped != null)
            {
                _videoView.SetOnTouchListener(new VideoTouchListener(_onTapped));
            }

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
            _videoView.SetOnTouchListener(null);
            _videoView.MediaPlayer = null;
            _videoView.Dispose();
            _videoView = null;
        }

        base.DestroyNativeControlCore(control);
    }

    private class VideoTouchListener : Java.Lang.Object, global::Android.Views.View.IOnTouchListener
    {
        private readonly Action _onTapped;
        private float _startX, _startY;
        private long _startTime;

        public VideoTouchListener(Action onTapped)
        {
            _onTapped = onTapped;
        }

        public bool OnTouch(global::Android.Views.View? v, global::Android.Views.MotionEvent? e)
        {
            if (e == null) return false;
            switch (e.Action)
            {
                case global::Android.Views.MotionEventActions.Down:
                    _startX = e.GetX();
                    _startY = e.GetY();
                    _startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    return true;
                case global::Android.Views.MotionEventActions.Up:
                    var dx = Math.Abs(e.GetX() - _startX);
                    var dy = Math.Abs(e.GetY() - _startY);
                    var dt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTime;
                    if (dx < 40 && dy < 40 && dt < 500)
                    {
                        _onTapped();
                    }
                    return true;
            }
            return false;
        }
    }
}

