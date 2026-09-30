using IPTV.Models;

namespace IPTV.Services;

public interface IHlsParserService
{
    Task<List<StreamQuality>> GetAvailableQualitiesAsync(string masterUrl);
}
