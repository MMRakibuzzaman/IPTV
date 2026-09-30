namespace IPTV.Services.Platform;

public interface IPlatformService
{
    Task<string?> PickFileAsync();
    void SetOrientation(bool isLandscape);
    void SetFullscreen(bool isFullscreen);
    bool CanChangeOrientation { get; }
}
