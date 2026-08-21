using IPTV.Services;
using CommunityToolkit.Maui.Views;

namespace IPTV
{
    public partial class MainPage : ContentPage
    {
        public MainPage(PlayerService playerService)
        {
            InitializeComponent();
            playerService.OnPlayStream += (url) => 
            {
                MainThread.BeginInvokeOnMainThread(async () => 
                {
#if ANDROID
                    if (url.StartsWith("/") || url.StartsWith("file://") || url.StartsWith("content://"))
                    {
                        try
                        {
                            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
                            {
                                if (await Permissions.CheckStatusAsync<Permissions.Media>() != PermissionStatus.Granted)
                                    await Permissions.RequestAsync<Permissions.Media>();
                            }
                            else
                            {
                                if (await Permissions.CheckStatusAsync<Permissions.StorageRead>() != PermissionStatus.Granted)
                                    await Permissions.RequestAsync<Permissions.StorageRead>();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error requesting permissions: {ex.Message}");
                        }
                    }
#endif
                    try 
                    {
                        mediaElement.IsVisible = true;
                        
                        if (url.StartsWith("/") || url.StartsWith("file://"))
                        {
                            string path = url.StartsWith("file://") ? url.Substring(7) : url;
                            mediaElement.Source = MediaSource.FromFile(path);
                        }
                        else
                        {
                            mediaElement.Source = MediaSource.FromUri(url);
                        }
                        
                        mediaElement.Play();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error playing media: {ex.Message}");
                    }
                    await Task.CompletedTask;
                });
            };

            playerService.OnStopStream += () => 
            {
                MainThread.BeginInvokeOnMainThread(() => 
                {
                    mediaElement.Stop();
                    mediaElement.Source = null;
                    mediaElement.IsVisible = false;
                });
            };

            playerService.OnSetVolume += (vol) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    mediaElement.Volume = vol;
                });
            };

            playerService.OnSetAspect += (aspect) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    mediaElement.Aspect = aspect switch
                    {
                        "Fill" => Aspect.Fill,
                        "AspectFill" => Aspect.AspectFill,
                        _ => Aspect.AspectFit
                    };
                });
            };

            playerService.OnSetOrientation += (isLandscape) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
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
                });
            };

            playerService.OnTogglePlayPause += () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (mediaElement.CurrentState == CommunityToolkit.Maui.Core.Primitives.MediaElementState.Playing)
                    {
                        mediaElement.Pause();
                    }
                    else
                    {
                        mediaElement.Play();
                    }
                });
            };

            mediaElement.StateChanged += (sender, e) =>
            {
                bool isPlaying = e.NewState == CommunityToolkit.Maui.Core.Primitives.MediaElementState.Playing || 
                                 e.NewState == CommunityToolkit.Maui.Core.Primitives.MediaElementState.Buffering;
                playerService.NotifyPlayState(isPlaying);
            };

            TimeSpan lastDuration = TimeSpan.Zero;
            mediaElement.PositionChanged += (sender, e) =>
            {
                playerService.NotifyPositionChanged(e.Position);
                
                if (mediaElement.Duration > TimeSpan.Zero && mediaElement.Duration != lastDuration)
                {
                    lastDuration = mediaElement.Duration;
                    playerService.NotifyDurationChanged(mediaElement.Duration);
                }
            };

            mediaElement.MediaOpened += (sender, e) =>
            {
                if (mediaElement.Duration > TimeSpan.Zero)
                {
                    lastDuration = mediaElement.Duration;
                    playerService.NotifyDurationChanged(mediaElement.Duration);
                }
            };

            playerService.OnSetPosition += (position) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    mediaElement.SeekTo(position);
                });
            };
        }
    }
}
