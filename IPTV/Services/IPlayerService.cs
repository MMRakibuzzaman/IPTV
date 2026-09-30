using LibVLCSharp.Shared;

namespace IPTV.Services;

public interface IPlayerService : IDisposable
{
    event Action<string>? OnPlayStream;
    event Action? OnStopStream;
    event Action<double>? OnSetVolume;
    event Action<string>? OnSetAspect;
    event Action<bool>? OnSetOrientation;
    event Action? OnTogglePlayPause;
    event Action<bool>? OnPlayStateChanged;
    event Action<TimeSpan>? OnSetPosition;
    event Action<TimeSpan>? OnPositionChanged;
    event Action<TimeSpan>? OnDurationChanged;

    LibVLC? LibVLCInstance { get; }
    MediaPlayer? MediaPlayer { get; }

    void Play(string url, TimeSpan? startPosition = null);
    void Stop();
    void SetVolume(double volume);
    void SetAspect(string aspect);
    void SetOrientation(bool isLandscape);
    void TogglePlayPause();
    void SetPosition(TimeSpan position);
    void NotifyPlayState(bool isPlaying);
    void NotifyPositionChanged(TimeSpan position);
    void NotifyDurationChanged(TimeSpan duration);
}
