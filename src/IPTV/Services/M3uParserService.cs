using System.Text.RegularExpressions;
using IPTV.Models;

namespace IPTV.Services;

public class M3uParserService : IM3uParserService
{
    private static readonly Regex GroupRegex = new(@"group-title=""([^""]*)""", RegexOptions.Compiled);
    private static readonly Regex LogoRegex = new(@"tvg-logo=""(.*?)""", RegexOptions.Compiled);

    public List<Channel> Parse(string m3uContent)
    {
        var channels = new List<Channel>();
        if (string.IsNullOrWhiteSpace(m3uContent))
        {
            return channels;
        }

        var lines = m3uContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Channel? currentChannel = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                currentChannel = new Channel();
                
                // Parse channel name after the last comma
                var nameStartIndex = trimmed.LastIndexOf(',') + 1;
                if (nameStartIndex > 0 && nameStartIndex < trimmed.Length)
                {
                    currentChannel.Name = trimmed.Substring(nameStartIndex).Trim();
                }

                // Extract group-title
                var groupMatch = GroupRegex.Match(trimmed);
                if (groupMatch.Success)
                {
                    currentChannel.Group = groupMatch.Groups[1].Value.Trim();
                }

                // Extract tvg-logo
                var logoMatch = LogoRegex.Match(trimmed);
                if (logoMatch.Success)
                {
                    currentChannel.LogoUrl = logoMatch.Groups[1].Value.Trim();
                }
            }
            else if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith('#'))
            {
                if (currentChannel != null)
                {
                    currentChannel.StreamUrl = trimmed;
                    currentChannel.Id = Guid.NewGuid().ToString();

                    if (string.IsNullOrWhiteSpace(currentChannel.Name))
                    {
                        currentChannel.Name = $"Channel {channels.Count + 1}";
                    }

                    channels.Add(currentChannel);
                    currentChannel = null;
                }
            }
        }

        return channels;
    }
}
