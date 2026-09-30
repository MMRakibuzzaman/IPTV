using CommunityToolkit.Mvvm.ComponentModel;
using IPTV.Repositories;
using IPTV.Services;
using IPTV.Services.Platform;

namespace IPTV.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IPlaylistRepository _playlistRepository;
    private readonly IM3uParserService _m3uParserService;
    private readonly IHlsParserService _hlsParserService;
    private readonly IPlayerService _playerService;
    private readonly IPlatformService _platformService;
    private readonly HttpClient _httpClient;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    public MainViewModel(
        IPlaylistRepository playlistRepository,
        IM3uParserService m3uParserService,
        IHlsParserService hlsParserService,
        IPlayerService playerService,
        IPlatformService platformService,
        HttpClient httpClient)
    {
        _playlistRepository = playlistRepository;
        _m3uParserService = m3uParserService;
        _hlsParserService = hlsParserService;
        _playerService = playerService;
        _platformService = platformService;
        _httpClient = httpClient;

        NavigateToHome();
    }

    public void NavigateToHome()
    {
        CurrentView = new HomeViewModel(
            this,
            _playlistRepository,
            _m3uParserService,
            _playerService,
            _platformService,
            _httpClient);
    }

    public void NavigateToPlayer(string playlistId)
    {
        CurrentView = new PlayerViewModel(
            this,
            playlistId,
            _playlistRepository,
            _playerService,
            _hlsParserService,
            _platformService);
    }
}
