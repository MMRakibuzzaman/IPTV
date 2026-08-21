using System.Text.RegularExpressions;

namespace IPTV.Services;

public class StreamQuality
{
    public string Name { get; set; } = "Auto";
    public string Url { get; set; } = "";
    public int Bandwidth { get; set; }
}

public class HlsParserService
{
    private readonly HttpClient _httpClient;

    public HlsParserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }
    }

    public async Task<List<StreamQuality>> GetAvailableQualitiesAsync(string masterUrl)
    {
        var qualities = new List<StreamQuality>();
        // Auto is always an option, pointing to the master playlist
        qualities.Add(new StreamQuality { Name = "Auto", Url = masterUrl, Bandwidth = int.MaxValue });

        try
        {
            if (string.IsNullOrWhiteSpace(masterUrl) || !masterUrl.StartsWith("http"))
                return qualities;

            var content = await _httpClient.GetStringAsync(masterUrl);
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Simple base URL extraction for relative paths
            var baseUrl = masterUrl;
            var queryIndex = baseUrl.IndexOf('?');
            if (queryIndex > -1) baseUrl = baseUrl.Substring(0, queryIndex);
            baseUrl = baseUrl.Substring(0, baseUrl.LastIndexOf('/') + 1);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("#EXT-X-STREAM-INF:"))
                {
                    var infoLine = lines[i];
                    var nextLine = (i + 1 < lines.Length) ? lines[i + 1] : "";

                    if (!string.IsNullOrWhiteSpace(nextLine) && !nextLine.StartsWith("#"))
                    {
                        var resolutionMatch = Regex.Match(infoLine, @"RESOLUTION=(\d+x\d+)");
                        var bandwidthMatch = Regex.Match(infoLine, @"BANDWIDTH=(\d+)");
                        
                        var resolution = resolutionMatch.Success ? resolutionMatch.Groups[1].Value : "Unknown";
                        var bandwidth = bandwidthMatch.Success && int.TryParse(bandwidthMatch.Groups[1].Value, out var bw) ? bw : 0;

                        // Ensure absolute URL
                        var streamUrl = nextLine.StartsWith("http") ? nextLine : new Uri(new Uri(baseUrl), nextLine).ToString();

                        if (resolution != "Unknown" || bandwidth > 0)
                        {
                            var name = resolution != "Unknown" ? resolution.Split('x').Last() + "p" : $"{bandwidth / 1000}k";
                            qualities.Add(new StreamQuality 
                            { 
                                Name = name, 
                                Url = streamUrl, 
                                Bandwidth = bandwidth 
                            });
                        }
                    }
                }
            }
            
            // Sort by bandwidth descending
            var auto = qualities.First();
            var sorted = qualities.Skip(1).OrderByDescending(q => q.Bandwidth).ToList();
            qualities = new List<StreamQuality> { auto };
            
            // Deduplicate names (sometimes multiple streams have same resolution)
            foreach (var q in sorted)
            {
                if (!qualities.Any(existing => existing.Name == q.Name))
                {
                    qualities.Add(q);
                }
            }

            return qualities;
        }
        catch
        {
            // If it fails (network error, not an m3u8), just return Auto
            return qualities;
        }
    }
}
