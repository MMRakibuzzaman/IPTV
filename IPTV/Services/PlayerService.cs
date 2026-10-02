using System;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace IPTV.Services;

public class PlayerService : IPlayerService
{
    public event Action<string>? OnPlayStream;
    public event Action? OnStopStream;
    public event Action<double>? OnSetVolume;
    public event Action<string>? OnSetAspect;
    public event Action<bool>? OnSetOrientation;
    public event Action? OnTogglePlayPause;
    public event Action<bool>? OnPlayStateChanged;
    public event Action<TimeSpan>? OnSetPosition;
    public event Action<TimeSpan>? OnPositionChanged;
    public event Action<TimeSpan>? OnDurationChanged;

    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;

    public LibVLC? LibVLCInstance => _libVLC;
    public MediaPlayer? MediaPlayer => _mediaPlayer;

    public PlayerService()
    {
        try
        {
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.EnableMouseInput = false;
            _mediaPlayer.EnableKeyInput = false;

            _mediaPlayer.Playing += (s, e) => NotifyPlayState(true);
            _mediaPlayer.Paused += (s, e) => NotifyPlayState(false);
            _mediaPlayer.Stopped += (s, e) => NotifyPlayState(false);
            _mediaPlayer.EndReached += (s, e) => NotifyPlayState(false);

            _mediaPlayer.TimeChanged += (s, e) =>
            {
                NotifyPositionChanged(TimeSpan.FromMilliseconds(e.Time));
            };

            _mediaPlayer.LengthChanged += (s, e) =>
            {
                NotifyDurationChanged(TimeSpan.FromMilliseconds(e.Length));
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlayerService] Error initializing LibVLC: {ex.Message}");
        }
    }

    public void Play(string url, TimeSpan? startPosition = null)
    {
        OnPlayStream?.Invoke(url);
        if (_libVLC != null && _mediaPlayer != null && !string.IsNullOrWhiteSpace(url))
        {
            Task.Run(() =>
            {
                try
                {
                    var media = new Media(_libVLC, new Uri(url));
                    if (startPosition.HasValue && startPosition.Value.TotalSeconds > 1)
                    {
                        media.AddOption($":start-time={(int)startPosition.Value.TotalSeconds}");
                    }
                    _mediaPlayer.Play(media);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PlayerService] Error playing stream: {ex.Message}");
                }
            });
        }
    }

    public void Stop()
    {
        OnStopStream?.Invoke();
        try
        {
            Task.Run(() =>
            {
                try
                {
                    _mediaPlayer?.Stop();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PlayerService] Error inside stop task: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlayerService] Error stopping: {ex.Message}");
        }
    }

    public void SetVolume(double volume)
    {
        OnSetVolume?.Invoke(volume);
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Volume = (int)Math.Clamp(volume * 100, 0, 100);
        }
    }

    public void SetAspect(string aspect)
    {
        OnSetAspect?.Invoke(aspect);
        if (_mediaPlayer != null)
        {
            try
            {
                _mediaPlayer.AspectRatio = aspect switch
                {
                    "16:9" => "16:9",
                    "4:3" => "4:3",
                    "16:10" => "16:10",
                    "21:9" => "21:9",
                    _ => null
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlayerService] Error setting aspect: {ex.Message}");
            }
        }
    }

    public void SetOrientation(bool isLandscape)
    {
        OnSetOrientation?.Invoke(isLandscape);
    }

    public void TogglePlayPause()
    {
        OnTogglePlayPause?.Invoke();
        if (_mediaPlayer != null)
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
            }
            else
            {
                _mediaPlayer.Play();
            }
        }
    }

    public void SetPosition(TimeSpan position)
    {
        OnSetPosition?.Invoke(position);
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Time = (long)position.TotalMilliseconds;
        }
    }

    public void NotifyPlayState(bool isPlaying) => OnPlayStateChanged?.Invoke(isPlaying);
    public void NotifyPositionChanged(TimeSpan position) => OnPositionChanged?.Invoke(position);
    public void NotifyDurationChanged(TimeSpan duration) => OnDurationChanged?.Invoke(duration);

    public void Dispose()
    {
        _mediaPlayer?.Dispose();
        _libVLC?.Dispose();
    }
}

