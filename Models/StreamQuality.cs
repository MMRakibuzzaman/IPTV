namespace IPTV.Models;

public class StreamQuality
{
    public string Name { get; set; } = "Auto";
    public string Url { get; set; } = string.Empty;
    public int Bandwidth { get; set; }
}
