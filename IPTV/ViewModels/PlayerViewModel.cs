using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPTV.Models;
using IPTV.Repositories;
using IPTV.Services;
using IPTV.Services.Platform;
using LibVLCSharp.Shared;

namespace IPTV.ViewModels;

public partial class SidebarItemViewModel : ObservableObject
{
    public bool IsHeader { get; set; }
    public string Name { get; set; } = string.Empty;
    public Channel? Channel { get; set; }
    public bool IsExpanded { get; set; }

    [ObservableProperty]
    private bool _isActive;
}

public partial class PlayerViewModel : ViewModelBase, IDisposable
{
    private readonly MainViewModel _mainViewModel;
    private readonly IPlaylistRepository _playlistRepository;
    private readonly IPlayerService _playerService;
    private readonly IHlsParserService _hlsParserService;
    private readonly IPlatformService _platformService;

    private Playlist? _playlist;
    private Timer? _hideTimer;
    private IDisposable? _pointerHook;
    private readonly Dictionary<string, bool> _groupExpandedState = new();
    private readonly string[] _aspects = { "Fit", "16:9", "4:3", "16:10", "21:9" };
    private int _aspectIndex = 0;

    public MediaPlayer? MediaPlayer => _playerService.MediaPlayer;

    [ObservableProperty]
    private string _playlistTitle = "Channels";

    [ObservableProperty]
    private bool _isSidebarOpen = false;

    [ObservableProperty]
    private bool _showControls = true;

    [ObservableProperty]
    private bool _isFullscreen = false;

    [ObservableProperty]
    private bool _isPlaying = true;

    [ObservableProperty]
    private double _currentPositionSeconds = 0;

    [ObservableProperty]
    private string _currentPositionText = "00:00";

    [ObservableProperty]
    private double _durationSeconds = 0;

    [ObservableProperty]
    private string _durationText = "00:00";

    private double _pendingResumeSeconds = 0;
    private bool _isUpdatingPositionFromPlayer = false;

    [ObservableProperty]
    private double _volume = 1.0;

    [ObservableProperty]
    private bool _isLandscape = false;

    [ObservableProperty]
    private string _currentAspect = "Fit";

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private Channel? _activeChannel;

    [ObservableProperty]
    private ObservableCollection<StreamQuality> _availableQualities = new();

    [ObservableProperty]
    private StreamQuality? _selectedQuality;

    [ObservableProperty]
    private bool _isQualityMenuOpen = false;

    [ObservableProperty]
    private ObservableCollection<SidebarItemViewModel> _visibleSidebarItems = new();

    public bool CanChangeOrientation => _platformService.CanChangeOrientation;

    public PlayerViewModel(
        MainViewModel mainViewModel,
        string playlistId,
        IPlaylistRepository playlistRepository,
        IPlayerService playerService,
        IHlsParserService hlsParserService,
        IPlatformService platformService)
    {
        _mainViewModel = mainViewModel;
        _playlistRepository = playlistRepository;
        _playerService = playerService;
        _hlsParserService = hlsParserService;
        _platformService = platformService;

        _playerService.OnPlayStateChanged += HandlePlayStateChanged;
        _playerService.OnPositionChanged += HandlePositionChanged;
        _playerService.OnDurationChanged += HandleDurationChanged;

        _pointerHook = _platformService.InstallPlayerPointerHook(
            () => IsSidebarOpen,
            () => ShowControls,
            () => IsFullscreen,
            () =>
            {
                if (!ShowControls)
                {
                    ShowControls = true;
                }
                ResetHideTimer();
            },
            () =>
            {
                ToggleControls();
            },
            () =>
            {
                ToggleSidebar();
            }
        );

        _ = InitializePlaylistAsync(playlistId);
    }

    private async Task InitializePlaylistAsync(string playlistId)
    {
        _playlist = await _playlistRepository.GetByIdAsync(playlistId);
        if (_playlist == null || _playlist.Channels.Count == 0)
        {
            _mainViewModel.NavigateToHome();
            return;
        }

        PlaylistTitle = _playlist.Name;

        var groups = _playlist.Channels
            .Select(c => string.IsNullOrEmpty(c.Group) ? "Ungrouped" : c.Group)
            .Distinct();

        foreach (var g in groups)
        {
            _groupExpandedState[g] = false;
        }

        UpdateVisibleItems();

        if (_playlist.Channels.Count > 0)
        {
            await PlayChannelAsync(_playlist.Channels[0]);
        }

        ResetHideTimer();
    }

    partial void OnSearchQueryChanged(string value)
    {
        UpdateVisibleItems();
    }

    private void UpdateVisibleItems()
    {
        if (_playlist == null) return;

        var query = SearchQuery.Trim().ToLowerInvariant();
        bool isSearching = !string.IsNullOrEmpty(query);

        var grouped = _playlist.Channels
            .GroupBy(c => string.IsNullOrEmpty(c.Group) ? "Ungrouped" : c.Group)
            .OrderBy(g => g.Key);

        var items = new List<SidebarItemViewModel>();

        foreach (var group in grouped)
        {
            var matching = isSearching
                ? group.Where(c => c.Name.ToLowerInvariant().Contains(query)).ToList()
                : group.ToList();

            if (matching.Count > 0)
            {
                bool isExpanded = isSearching || _groupExpandedState.GetValueOrDefault(group.Key);

                items.Add(new SidebarItemViewModel
                {
                    IsHeader = true,
                    Name = group.Key,
                    IsExpanded = isExpanded
                });

                if (isExpanded)
                {
                    foreach (var ch in matching)
                    {
                        items.Add(new SidebarItemViewModel
                        {
                            IsHeader = false,
                            Channel = ch,
                            IsActive = ActiveChannel?.Id == ch.Id
                        });
                    }
                }
            }
        }

        VisibleSidebarItems = new ObservableCollection<SidebarItemViewModel>(items);
    }

    [RelayCommand]
    public void ToggleGroup(string groupName)
    {
        if (_groupExpandedState.TryGetValue(groupName, out bool expanded))
        {
            _groupExpandedState[groupName] = !expanded;
        }
        else
        {
            _groupExpandedState[groupName] = true;
        }
        UpdateVisibleItems();
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    [RelayCommand]
    public async Task PlayChannelAsync(Channel channel)
    {
        ActiveChannel = channel;
        IsQualityMenuOpen = false;

        DurationSeconds = 0;
        CurrentPositionSeconds = 0;
        CurrentPositionText = "00:00";
        DurationText = "00:00";

        var streamToPlay = channel.StreamUrl;
        if (!string.IsNullOrWhiteSpace(streamToPlay))
        {
            _playerService.Play(streamToPlay);
        }

        var prevActive = VisibleSidebarItems.FirstOrDefault(i => i.IsActive && i.Channel?.Id != channel.Id);
        if (prevActive != null)
        {
            prevActive.IsActive = false;
        }

        var newActive = VisibleSidebarItems.FirstOrDefault(i => i.Channel?.Id == channel.Id);
        if (newActive != null)
        {
            newActive.IsActive = true;
        }

        ResetHideTimer();

        // Populate initial "Auto" quality immediately
        var autoQuality = new StreamQuality { Name = "Auto", Url = channel.StreamUrl, IsSelected = true };
        AvailableQualities = new ObservableCollection<StreamQuality> { autoQuality };
        SelectedQuality = autoQuality;

        // Fetch stream qualities asynchronously in background
        _ = Task.Run(async () =>
        {
            var qualities = await _hlsParserService.GetAvailableQualitiesAsync(channel.StreamUrl);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ActiveChannel?.Id == channel.Id)
                {
                    AvailableQualities = new ObservableCollection<StreamQuality>(qualities);

                    var current = AvailableQualities.FirstOrDefault(q => q.Url == SelectedQuality?.Url) 
                               ?? AvailableQualities.FirstOrDefault();
                    if (current != null)
                    {
                        current.IsSelected = true;
                        SelectedQuality = current;
                    }
                }
            });
        });
    }

    [RelayCommand]
    public void SelectQuality(StreamQuality quality)
    {
        SelectedQuality = quality;
        IsQualityMenuOpen = false;

        foreach (var q in AvailableQualities)
        {
            q.IsSelected = (q == quality || (q.Name == quality.Name && q.Url == quality.Url));
        }

        TimeSpan? resumePos = null;
        if (DurationSeconds > 0 && CurrentPositionSeconds > 1)
        {
            resumePos = TimeSpan.FromSeconds(CurrentPositionSeconds);
            _pendingResumeSeconds = CurrentPositionSeconds;
        }

        _playerService.Play(quality.Url, resumePos);
        ResetHideTimer();
    }

    [RelayCommand]
    public void TogglePlayPause()
    {
        _playerService.TogglePlayPause();
        ResetHideTimer();
    }

    [RelayCommand]
    public void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
        if (IsSidebarOpen)
        {
            ShowControls = true;
            _hideTimer?.Dispose();
            _hideTimer = null;
        }
        else
        {
            ResetHideTimer();
        }
    }

    [RelayCommand]
    public void ToggleControls()
    {
        if (!IsFullscreen)
        {
            ShowControls = true;
            return;
        }

        ShowControls = !ShowControls;
        if (ShowControls)
        {
            ResetHideTimer();
        }
        else
        {
            _hideTimer?.Dispose();
            _hideTimer = null;
        }
    }

    [RelayCommand]
    public void ToggleAspect()
    {
        _aspectIndex = (_aspectIndex + 1) % _aspects.Length;
        CurrentAspect = _aspects[_aspectIndex];
        _playerService.SetAspect(CurrentAspect);
        ResetHideTimer();
    }

    [RelayCommand]
    public void ToggleOrientation()
    {
        if (_platformService.CanChangeOrientation)
        {
            IsLandscape = !IsLandscape;
            _platformService.SetOrientation(IsLandscape);
        }
        else
        {
            IsFullscreen = !IsFullscreen;
            _platformService.SetFullscreen(IsFullscreen);
        }
        ResetHideTimer();
    }

    partial void OnVolumeChanged(double value)
    {
        _playerService.SetVolume(value);
        ResetHideTimer();
    }

    [RelayCommand]
    public void Seek(double seconds)
    {
        _playerService.SetPosition(TimeSpan.FromSeconds(seconds));
        ResetHideTimer();
    }

    partial void OnIsFullscreenChanged(bool value)
    {
        if (value)
        {
            ResetHideTimer();
        }
        else
        {
            _hideTimer?.Dispose();
            _hideTimer = null;
            ShowControls = true;
        }
    }

    [RelayCommand]
    public void GoBack()
    {
        Dispose();
        _mainViewModel.NavigateToHome();
    }

    partial void OnCurrentPositionSecondsChanged(double value)
    {
        CurrentPositionText = FormatTime(TimeSpan.FromSeconds(value));
        if (_isUpdatingPositionFromPlayer)
            return;

        _playerService.SetPosition(TimeSpan.FromSeconds(value));
        ResetHideTimer();
    }

    partial void OnDurationSecondsChanged(double value)
    {
        DurationText = FormatTime(TimeSpan.FromSeconds(value));
    }

    private static string FormatTime(TimeSpan t)
    {
        if (t.TotalSeconds <= 0) return "00:00";
        return t.TotalHours >= 1 
            ? t.ToString(@"hh\:mm\:ss") 
            : t.ToString(@"mm\:ss");
    }

    private void HandlePlayStateChanged(bool playing)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = playing;
            if (playing && _pendingResumeSeconds > 1)
            {
                var resume = _pendingResumeSeconds;
                _pendingResumeSeconds = 0;
                Task.Delay(350).ContinueWith(_ =>
                {
                    _playerService.SetPosition(TimeSpan.FromSeconds(resume));
                });
            }
        });
    }

    private void HandlePositionChanged(TimeSpan pos)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _isUpdatingPositionFromPlayer = true;
            CurrentPositionSeconds = pos.TotalSeconds;
            _isUpdatingPositionFromPlayer = false;

            if (SelectedQuality?.Name != null && SelectedQuality.Name.StartsWith("Auto"))
            {
                uint px_w = 0;
                uint px_h = 0;
                if (MediaPlayer != null && MediaPlayer.Size(0, ref px_w, ref px_h))
                {
                    if (px_h > 0)
                    {
                        var newName = $"Auto • {px_h}p";
                        if (SelectedQuality.Name != newName)
                        {
                            SelectedQuality.Name = newName;
                        }
                    }
                }
            }
        });
    }

    private void HandleDurationChanged(TimeSpan dur)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            DurationSeconds = dur.TotalSeconds;
        });
    }

    public void ResetHideTimer()
    {
        _hideTimer?.Dispose();
        _hideTimer = null;

        // Never hide controls when not in fullscreen!
        if (!IsFullscreen)
        {
            ShowControls = true;
            return;
        }

        if (IsSidebarOpen)
        {
            ShowControls = true;
            return;
        }

        if (ShowControls)
        {
            _hideTimer = new Timer(_ =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (IsFullscreen && !IsSidebarOpen)
                    {
                        ShowControls = false;
                    }
                });
            }, null, 4000, Timeout.Infinite);
        }
    }

    public void Dispose()
    {
        _pointerHook?.Dispose();
        _pointerHook = null;

        _hideTimer?.Dispose();
        _hideTimer = null;

        _playerService.OnPlayStateChanged -= HandlePlayStateChanged;
        _playerService.OnPositionChanged -= HandlePositionChanged;
        _playerService.OnDurationChanged -= HandleDurationChanged;

        _platformService.SetFullscreen(false);
        IsFullscreen = false;

        if (IsLandscape)
        {
            IsLandscape = false;
            _platformService.SetOrientation(false);
        }
        _playerService.Stop();
    }
}
