using System.Text.RegularExpressions;
using IPTV.Models;

namespace IPTV.Services;

public class M3uParserService
{
    public List<Channel> Parse(string m3uContent)
    {
        var channels = new List<Channel>();
        var lines = m3uContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        Channel? currentChannel = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("#EXTINF:"))
            {
                currentChannel = new Channel();
                // Parse channel info
                var nameStartIndex = line.LastIndexOf(',') + 1;
                if (nameStartIndex > 0 && nameStartIndex < line.Length)
                {
                    currentChannel.Name = line.Substring(nameStartIndex).Trim();
                }

                // Extract group-title
                var groupMatch = System.Text.RegularExpressions.Regex.Match(line, @"group-title=""([^""]*)""");
                if (groupMatch.Success)
                {
                    currentChannel.Group = groupMatch.Groups[1].Value;
                }
                
                // Extract Logo
                var logoMatch = Regex.Match(line, @"tvg-logo=""(.*?)""");
                if (logoMatch.Success)
                {
                    currentChannel.LogoUrl = logoMatch.Groups[1].Value.Trim();
                }
            }
            else if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
            {
                if (currentChannel != null)
                {
                    // It's a stream URL
                    currentChannel.StreamUrl = line.Trim();
                    currentChannel.Id = Guid.NewGuid().ToString();
                    
                    channels.Add(currentChannel);
                    currentChannel = null; // Reset for next channel
                }
            }
        }

        return channels;
    }
}
