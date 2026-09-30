using System.Collections.ObjectModel;
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
    private double _durationSeconds = 0;

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
    public async Task PlayChannelAsync(Channel channel)
    {
        ActiveChannel = channel;
        IsQualityMenuOpen = false;

        var qualities = await _hlsParserService.GetAvailableQualitiesAsync(channel.StreamUrl);
        AvailableQualities = new ObservableCollection<StreamQuality>(qualities);
        SelectedQuality = qualities.FirstOrDefault();

        var streamToPlay = SelectedQuality?.Url ?? channel.StreamUrl;
        if (!string.IsNullOrWhiteSpace(streamToPlay))
        {
            _playerService.Play(streamToPlay);
        }

        foreach (var item in VisibleSidebarItems)
        {
            if (!item.IsHeader && item.Channel != null)
            {
                item.IsActive = (item.Channel.Id == channel.Id);
            }
        }
    }

    [RelayCommand]
    public void SelectQuality(StreamQuality quality)
    {
        SelectedQuality = quality;
        IsQualityMenuOpen = false;
        _playerService.Play(quality.Url);
    }

    [RelayCommand]
    public void TogglePlayPause()
    {
        _playerService.TogglePlayPause();
    }

    [RelayCommand]
    public void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
    }

    [RelayCommand]
    public void ToggleControls()
    {
        ShowControls = !ShowControls;
    }

    [RelayCommand]
    public void ToggleAspect()
    {
        _aspectIndex = (_aspectIndex + 1) % _aspects.Length;
        CurrentAspect = _aspects[_aspectIndex];
        _playerService.SetAspect(CurrentAspect);
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

    [RelayCommand]
    public void GoBack()
    {
        Dispose();
        _mainViewModel.NavigateToHome();
    }

    private void HandlePlayStateChanged(bool playing)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = playing;
        });
    }

    private void HandlePositionChanged(TimeSpan pos)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            CurrentPositionSeconds = pos.TotalSeconds;
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
        ShowControls = true;
    }

    public void Dispose()
    {
        _playerService.OnPlayStateChanged -= HandlePlayStateChanged;
        _playerService.OnPositionChanged -= HandlePositionChanged;
        _playerService.OnDurationChanged -= HandleDurationChanged;

        if (IsLandscape)
        {
            _platformService.SetOrientation(false);
        }
        _playerService.Stop();
    }
}
