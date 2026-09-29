using System.Text.RegularExpressions;
using IPTV.Models;

namespace IPTV.Services;

public class HlsParserService : IHlsParserService
{
    private readonly HttpClient _httpClient;
    private static readonly Regex ResolutionRegex = new(@"RESOLUTION=(\d+x\d+)", RegexOptions.Compiled);
    private static readonly Regex BandwidthRegex = new(@"BANDWIDTH=(\d+)", RegexOptions.Compiled);

    public HlsParserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }
    }

    public async Task<List<StreamQuality>> GetAvailableQualitiesAsync(string masterUrl)
    {
        var qualities = new List<StreamQuality>
        {
            new() { Name = "Auto", Url = masterUrl, Bandwidth = int.MaxValue }
        };

        if (string.IsNullOrWhiteSpace(masterUrl) || !masterUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return qualities;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync(masterUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return qualities;
            }

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            if (string.IsNullOrWhiteSpace(content) || !content.Contains("#EXTM3U"))
            {
                return qualities;
            }

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var baseUrl = masterUrl;
            var queryIndex = baseUrl.IndexOf('?');
            if (queryIndex > -1)
            {
                baseUrl = baseUrl.Substring(0, queryIndex);
            }
            baseUrl = baseUrl.Substring(0, baseUrl.LastIndexOf('/') + 1);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("#EXT-X-STREAM-INF:", StringComparison.OrdinalIgnoreCase))
                {
                    var nextLine = (i + 1 < lines.Length) ? lines[i + 1].Trim() : string.Empty;

                    if (!string.IsNullOrWhiteSpace(nextLine) && !nextLine.StartsWith('#'))
                    {
                        var resolutionMatch = ResolutionRegex.Match(line);
                        var bandwidthMatch = BandwidthRegex.Match(line);

                        var resolution = resolutionMatch.Success ? resolutionMatch.Groups[1].Value : "Unknown";
                        var bandwidth = bandwidthMatch.Success && int.TryParse(bandwidthMatch.Groups[1].Value, out var bw) ? bw : 0;

                        var streamUrl = nextLine.StartsWith("http", StringComparison.OrdinalIgnoreCase) 
                            ? nextLine 
                            : new Uri(new Uri(baseUrl), nextLine).ToString();

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

            var auto = qualities.First();
            var sorted = qualities.Skip(1).OrderByDescending(q => q.Bandwidth).ToList();
            var result = new List<StreamQuality> { auto };

            foreach (var q in sorted)
            {
                if (!result.Any(existing => existing.Name == q.Name))
                {
                    result.Add(q);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HlsParserService] Error parsing HLS qualities: {ex.Message}");
            return qualities;
        }
    }
}
