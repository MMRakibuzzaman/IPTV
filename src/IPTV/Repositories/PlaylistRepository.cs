using System.Text.Json;
using IPTV.Models;

namespace IPTV.Repositories;

public class PlaylistRepository : IPlaylistRepository
{
    private const string FileName = "playlists.json";
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<Playlist>? _cachedPlaylists;

    public PlaylistRepository()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IPTV");
        Directory.CreateDirectory(appData);
        _filePath = Path.Combine(appData, FileName);
    }

    public async Task<List<Playlist>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_cachedPlaylists != null)
            {
                return _cachedPlaylists.ToList();
            }

            _cachedPlaylists = await LoadFromFileAsync();
            return _cachedPlaylists.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Playlist?> GetByIdAsync(string id)
    {
        var playlists = await GetAllAsync();
        return playlists.FirstOrDefault(p => p.Id == id);
    }

    public async Task AddAsync(Playlist playlist)
    {
        await _lock.WaitAsync();
        try
        {
            _cachedPlaylists ??= await LoadFromFileAsync();
            _cachedPlaylists.Add(playlist);
            await SaveToFileAsync(_cachedPlaylists);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateAsync(Playlist playlist)
    {
        await _lock.WaitAsync();
        try
        {
            _cachedPlaylists ??= await LoadFromFileAsync();
            var index = _cachedPlaylists.FindIndex(p => p.Id == playlist.Id);
            if (index != -1)
            {
                _cachedPlaylists[index] = playlist;
                await SaveToFileAsync(_cachedPlaylists);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            _cachedPlaylists ??= await LoadFromFileAsync();
            var removed = _cachedPlaylists.RemoveAll(p => p.Id == id);
            if (removed > 0)
            {
                await SaveToFileAsync(_cachedPlaylists);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<Playlist>> LoadFromFileAsync()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                using var stream = File.OpenRead(_filePath);
                var playlists = await JsonSerializer.DeserializeAsync<List<Playlist>>(stream);
                return playlists ?? new List<Playlist>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlaylistRepository] Error loading from file: {ex.Message}");
        }

        return new List<Playlist>();
    }

    private async Task SaveToFileAsync(List<Playlist> playlists)
    {
        try
        {
            var tempPath = _filePath + ".tmp";
            using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, playlists, new JsonSerializerOptions { WriteIndented = true });
            }

            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
            File.Move(tempPath, _filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlaylistRepository] Error saving to file: {ex.Message}");
        }
    }
}
