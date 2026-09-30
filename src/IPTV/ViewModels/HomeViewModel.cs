using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPTV.Models;
using IPTV.Repositories;
using IPTV.Services;
using IPTV.Services.Platform;

namespace IPTV.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly MainViewModel _mainViewModel;
    private readonly IPlaylistRepository _playlistRepository;
    private readonly IM3uParserService _parserService;
    private readonly IPlayerService _playerService;
    private readonly IPlatformService _platformService;
    private readonly HttpClient _httpClient;

    [ObservableProperty]
    private ObservableCollection<Playlist> _playlists = new();

    [ObservableProperty]
    private string _newPlaylistName = string.Empty;

    [ObservableProperty]
    private string _newPlaylistContent = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _editingPlaylistId = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    public bool IsEditing => !string.IsNullOrEmpty(EditingPlaylistId);
    public string CardHeaderTitle => IsEditing ? "Edit Playlist" : "Add New Playlist";
    public string ActionButtonText => IsLoading ? "Loading..." : (IsEditing ? "Save Changes" : "Import Playlist");

    partial void OnEditingPlaylistIdChanged(string value)
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(CardHeaderTitle));
        OnPropertyChanged(nameof(ActionButtonText));
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(ActionButtonText));
    }

    public HomeViewModel(
        MainViewModel mainViewModel,
        IPlaylistRepository playlistRepository,
        IM3uParserService parserService,
        IPlayerService playerService,
        IPlatformService platformService,
        HttpClient httpClient)
    {
        _mainViewModel = mainViewModel;
        _playlistRepository = playlistRepository;
        _parserService = parserService;
        _playerService = playerService;
        _platformService = platformService;
        _httpClient = httpClient;

        _playerService.Stop();
        _ = LoadPlaylistsAsync();
    }

    [RelayCommand]
    public async Task LoadPlaylistsAsync()
    {
        var list = await _playlistRepository.GetAllAsync();
        Playlists = new ObservableCollection<Playlist>(list);
    }

    [RelayCommand]
    public async Task PickFileAsync()
    {
        try
        {
            var filePath = await _platformService.PickFileAsync();
            if (!string.IsNullOrEmpty(filePath))
            {
                NewPlaylistContent = filePath;
                if (string.IsNullOrWhiteSpace(NewPlaylistName))
                {
                    NewPlaylistName = Path.GetFileNameWithoutExtension(filePath);
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error selecting file: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task AddOrUpdatePlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPlaylistName))
        {
            ErrorMessage = "Name is required.";
            return;
        }

        if (string.IsNullOrEmpty(EditingPlaylistId) && string.IsNullOrWhiteSpace(NewPlaylistContent))
        {
            ErrorMessage = "Playlist URL or file is required when adding a new playlist.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var isNew = string.IsNullOrEmpty(EditingPlaylistId);
            var playlistToSave = isNew 
                ? new Playlist() 
                : Playlists.FirstOrDefault(p => p.Id == EditingPlaylistId) ?? new Playlist();

            if (!string.IsNullOrWhiteSpace(NewPlaylistContent))
            {
                string contentToParse = NewPlaylistContent;
                if (NewPlaylistContent.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    contentToParse = await _httpClient.GetStringAsync(NewPlaylistContent);
                    playlistToSave.Url = NewPlaylistContent;
                }
                else
                {
                    if (File.Exists(NewPlaylistContent))
                    {
                        contentToParse = await File.ReadAllTextAsync(NewPlaylistContent);
                        playlistToSave.Url = "local";
                    }
                    else
                    {
                        ErrorMessage = "Local file not found.";
                        IsLoading = false;
                        return;
                    }
                }

                var parsedChannels = _parserService.Parse(contentToParse);
                if (parsedChannels.Count == 0)
                {
                    ErrorMessage = "No channels could be parsed from the provided playlist.";
                    IsLoading = false;
                    return;
                }

                playlistToSave.Channels = parsedChannels;
            }

            playlistToSave.Name = NewPlaylistName;

            if (isNew)
            {
                await _playlistRepository.AddAsync(playlistToSave);
            }
            else
            {
                await _playlistRepository.UpdateAsync(playlistToSave);
            }

            CancelEdit();
            await LoadPlaylistsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to import playlist: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void EditPlaylist(Playlist playlist)
    {
        EditingPlaylistId = playlist.Id;
        NewPlaylistName = playlist.Name;
        NewPlaylistContent = playlist.Url == "local" ? string.Empty : playlist.Url;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(IsEditing));
    }

    [RelayCommand]
    public void CancelEdit()
    {
        EditingPlaylistId = string.Empty;
        NewPlaylistName = string.Empty;
        NewPlaylistContent = string.Empty;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(IsEditing));
    }

    [RelayCommand]
    public async Task DeletePlaylistAsync(string id)
    {
        await _playlistRepository.DeleteAsync(id);
        if (EditingPlaylistId == id)
        {
            CancelEdit();
        }
        await LoadPlaylistsAsync();
    }

    [RelayCommand]
    public void OpenPlaylist(string id)
    {
        _mainViewModel.NavigateToPlayer(id);
    }
}
