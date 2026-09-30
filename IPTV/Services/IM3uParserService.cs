using IPTV.Models;

namespace IPTV.Services;

public interface IM3uParserService
{
    List<Channel> Parse(string m3uContent);
}
