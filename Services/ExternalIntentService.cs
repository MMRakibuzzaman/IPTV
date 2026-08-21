namespace IPTV.Services;

public class ExternalIntentService
{
    public event Action<string>? OnExternalVideoReceived;
    public string? PendingExternalUrl { get; set; }

    public void NotifyVideoIntent(string url)
    {
        PendingExternalUrl = url;
        OnExternalVideoReceived?.Invoke(url);
    }
}
