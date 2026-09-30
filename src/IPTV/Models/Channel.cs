namespace IPTV.Models;

public class Channel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string StreamUrl { get; set; } = string.Empty;
}
