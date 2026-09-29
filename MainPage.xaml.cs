using CommunityToolkit.Maui.Views;
using IPTV.Services;

namespace IPTV
{
    public partial class MainPage : ContentPage
    {
        public MainPage(IPlayerService playerService)
        {
            InitializeComponent();

            playerService.OnPlayStream += (url) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(url))
                        {
                            return;
                        }

                        mediaElement.IsVisible = true;
                        mediaElement.Source = MediaSource.FromUri(url);
                        mediaElement.Play();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainPage] Error playing media: {ex.Message}");
                    }
                });
            };

            playerService.OnStopStream += () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        mediaElement.Stop();
                        mediaElement.IsVisible = false;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainPage] Error stopping media: {ex.Message}");
                    }
                });
            };

            playerService.OnSetVolume += (vol) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        mediaElement.Volume = Math.Clamp(vol, 0.0, 1.0);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainPage] Error setting volume: {ex.Message}");
                    }
                });
            };

            playerService.OnSetAspect += (aspect) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        mediaElement.Aspect = aspect switch
                        {
                            "Fill" => Aspect.Fill,
                            "AspectFill" => Aspect.AspectFill,
                            _ => Aspect.AspectFit
                        };
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainPage] Error setting aspect: {ex.Message}");
                    }
                });
            };

            playerService.OnSetOrientation += (isLandscape) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
#if ANDROID
                        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                        if (activity != null)
                        {
                            activity.RequestedOrientation = isLandscape 
                                ? Android.Content.PM.ScreenOrientation.Landscape 
                                : Android.Content.PM.ScreenOrientation.Unspecified;
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainPage] Error setting orientation: {ex.Message}");
                    }
                });
            };

            playerService.OnTogglePlayPause += () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (mediaElement.CurrentState == CommunityToolkit.Maui.Core.Primitives.MediaElementState.Playing)
                        {
                            mediaElement.Pause();
                        }
                        else
                        {
                            mediaElement.Play();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainPage] Error toggling play/pause: {ex.Message}");
                    }
                });
            };

            playerService.OnSetPosition += (position) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        mediaElement.SeekTo(position);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainPage] Error seeking: {ex.Message}");
                    }
                });
            };

            mediaElement.StateChanged += (sender, e) =>
            {
                try
                {
                    bool isPlaying = e.NewState == CommunityToolkit.Maui.Core.Primitives.MediaElementState.Playing || 
                                     e.NewState == CommunityToolkit.Maui.Core.Primitives.MediaElementState.Buffering;
                    playerService.NotifyPlayState(isPlaying);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] StateChanged error: {ex.Message}");
                }
            };

            TimeSpan lastDuration = TimeSpan.Zero;

            mediaElement.PositionChanged += (sender, e) =>
            {
                try
                {
                    playerService.NotifyPositionChanged(e.Position);

                    if (mediaElement.Duration > TimeSpan.Zero && mediaElement.Duration != lastDuration)
                    {
                        lastDuration = mediaElement.Duration;
                        playerService.NotifyDurationChanged(mediaElement.Duration);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] PositionChanged error: {ex.Message}");
                }
            };

            mediaElement.MediaOpened += (sender, e) =>
            {
                try
                {
                    if (mediaElement.Duration > TimeSpan.Zero)
                    {
                        lastDuration = mediaElement.Duration;
                        playerService.NotifyDurationChanged(mediaElement.Duration);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] MediaOpened error: {ex.Message}");
                }
            };
        }
    }
}
