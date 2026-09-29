using IPTV.Models;

namespace IPTV.Repositories;

public interface IPlaylistRepository
{
    Task<List<Playlist>> GetAllAsync();
    Task<Playlist?> GetByIdAsync(string id);
    Task AddAsync(Playlist playlist);
    Task UpdateAsync(Playlist playlist);
    Task DeleteAsync(string id);
}
