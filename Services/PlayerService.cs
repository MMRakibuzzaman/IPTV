namespace IPTV.Services;

public class PlayerService
{
    public event Action<string>? OnPlayStream;
    public event Action? OnStopStream;
    
    public event Action<double>? OnSetVolume;
    public event Action<string>? OnSetAspect;
    public event Action<bool>? OnSetOrientation;
    public event Action? OnTogglePlayPause;
    public event Action<bool>? OnPlayStateChanged; // true = playing, false = paused

    public void Play(string url)
    {
        OnPlayStream?.Invoke(url);
    }

    public void Stop()
    {
        OnStopStream?.Invoke();
    }

    public void SetVolume(double volume)
    {
        OnSetVolume?.Invoke(volume);
    }

    public void SetAspect(string aspect)
    {
        OnSetAspect?.Invoke(aspect);
    }

    public void SetOrientation(bool isLandscape)
    {
        OnSetOrientation?.Invoke(isLandscape);
    }

    public void TogglePlayPause()
    {
        OnTogglePlayPause?.Invoke();
    }

    public void NotifyPlayState(bool isPlaying)
    {
        OnPlayStateChanged?.Invoke(isPlaying);
    }

    public event Action<TimeSpan>? OnSetPosition;
    public event Action<TimeSpan>? OnPositionChanged;
    public event Action<TimeSpan>? OnDurationChanged;

    public void SetPosition(TimeSpan position)
    {
        OnSetPosition?.Invoke(position);
    }

    public void NotifyPositionChanged(TimeSpan position)
    {
        OnPositionChanged?.Invoke(position);
    }

    public void NotifyDurationChanged(TimeSpan duration)
    {
        OnDurationChanged?.Invoke(duration);
    }
}
