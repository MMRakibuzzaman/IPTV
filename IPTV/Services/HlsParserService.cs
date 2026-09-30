using System.Text.RegularExpressions;
using IPTV.Models;

namespace IPTV.Services;

public class HlsParserService : IHlsParserService
{
    private readonly HttpClient _httpClient;
    private static readonly Regex ResolutionRegex = new(@"RESOLUTION=(\d+x\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BandwidthRegex = new(@"BANDWIDTH=(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NameRegex = new(@"NAME=""?([^"",\r\n]+)""?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync(masterUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return qualities;
            }

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            if (string.IsNullOrWhiteSpace(content) || !content.Contains("#EXTM3U", StringComparison.OrdinalIgnoreCase))
            {
                return qualities;
            }

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var effectiveUrl = response.RequestMessage?.RequestUri?.ToString() ?? masterUrl;
            var baseUrl = effectiveUrl;
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
                    // Scan forward to locate the stream URI (skipping empty lines or comments)
                    int urlIdx = i + 1;
                    while (urlIdx < lines.Length && (string.IsNullOrWhiteSpace(lines[urlIdx]) || lines[urlIdx].Trim().StartsWith('#')))
                    {
                        urlIdx++;
                    }

                    if (urlIdx < lines.Length)
                    {
                        var streamLine = lines[urlIdx].Trim();
                        var resolutionMatch = ResolutionRegex.Match(line);
                        var bandwidthMatch = BandwidthRegex.Match(line);
                        var nameMatch = NameRegex.Match(line);

                        var resolution = resolutionMatch.Success ? resolutionMatch.Groups[1].Value : "Unknown";
                        var bandwidth = bandwidthMatch.Success && int.TryParse(bandwidthMatch.Groups[1].Value, out var bw) ? bw : 0;

                        var streamUrl = streamLine.StartsWith("http", StringComparison.OrdinalIgnoreCase) 
                            ? streamLine 
                            : new Uri(new Uri(baseUrl), streamLine).ToString();

                        string name;
                        if (resolution != "Unknown")
                        {
                            name = resolution.Split(new[] { 'x', 'X' }).Last() + "p";
                        }
                        else if (nameMatch.Success)
                        {
                            name = nameMatch.Groups[1].Value.Trim();
                        }
                        else if (bandwidth > 0)
                        {
                            name = $"{bandwidth / 1000}k";
                        }
                        else
                        {
                            name = $"Stream {qualities.Count}";
                        }

                        qualities.Add(new StreamQuality
                        {
                            Name = name,
                            Url = streamUrl,
                            Bandwidth = bandwidth
                        });
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
