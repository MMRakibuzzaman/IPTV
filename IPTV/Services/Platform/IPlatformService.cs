namespace IPTV.Services.Platform;

public interface IPlatformService
{
    Task<string?> PickFileAsync();
    void SetOrientation(bool isLandscape);
    void SetFullscreen(bool isFullscreen);
    bool CanChangeOrientation { get; }

    IDisposable? InstallPlayerPointerHook(
        Func<bool> isSidebarOpen,
        Func<bool> areControlsShowing,
        Func<bool> isFullscreen,
        Action onPointerMoved,
        Action onVideoClicked,
        Action onChannelsClicked) => null;

    Avalonia.Controls.Control? CreateVideoView(object mediaPlayer, Action? onVideoTapped = null) => null;
}
