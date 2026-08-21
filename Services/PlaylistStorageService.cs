using System.Text.Json;
using IPTV.Models;
using Microsoft.Maui.Storage;

namespace IPTV.Services;

public class PlaylistStorageService
{
    private const string PlaylistsKey = "IPTV_Playlists";

    public List<Playlist> GetPlaylists()
    {
        string json = Preferences.Default.Get(PlaylistsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return new List<Playlist>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<Playlist>>(json) ?? new List<Playlist>();
        }
        catch
        {
            return new List<Playlist>();
        }
    }

    public void SavePlaylists(List<Playlist> playlists)
    {
        string json = JsonSerializer.Serialize(playlists);
        Preferences.Default.Set(PlaylistsKey, json);
    }
}
