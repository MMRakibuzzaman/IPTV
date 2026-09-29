using System.Text.Json;
using IPTV.Models;
using Microsoft.Maui.Storage;

namespace IPTV.Repositories;

public class PlaylistRepository : IPlaylistRepository
{
    private const string LegacyPreferencesKey = "IPTV_Playlists";
    private const string FileName = "playlists.json";
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<Playlist>? _cachedPlaylists;

    public PlaylistRepository()
    {
        _filePath = Path.Combine(FileSystem.AppDataDirectory, FileName);
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
            if (index >= 0)
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
            var countRemoved = _cachedPlaylists.RemoveAll(p => p.Id == id);
            if (countRemoved > 0)
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
            // First check if dedicated file exists
            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    return JsonSerializer.Deserialize<List<Playlist>>(json) ?? new List<Playlist>();
                }
            }

            // Legacy migration: check if playlists were stored in Preferences
            if (Preferences.Default.ContainsKey(LegacyPreferencesKey))
            {
                var legacyJson = Preferences.Default.Get(LegacyPreferencesKey, string.Empty);
                if (!string.IsNullOrEmpty(legacyJson))
                {
                    var migrated = JsonSerializer.Deserialize<List<Playlist>>(legacyJson) ?? new List<Playlist>();
                    if (migrated.Count > 0)
                    {
                        await SaveToFileAsync(migrated);
                        // Clean up preferences to prevent memory bloat on mobile
                        Preferences.Default.Remove(LegacyPreferencesKey);
                        return migrated;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlaylistRepository] Error loading playlists: {ex.Message}");
        }

        return new List<Playlist>();
    }

    private async Task SaveToFileAsync(List<Playlist> playlists)
    {
        try
        {
            var json = JsonSerializer.Serialize(playlists, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlaylistRepository] Error saving playlists: {ex.Message}");
        }
    }
}
