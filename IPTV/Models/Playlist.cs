namespace IPTV.Models;

public class Playlist
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<Channel> Channels { get; set; } = new();
}
