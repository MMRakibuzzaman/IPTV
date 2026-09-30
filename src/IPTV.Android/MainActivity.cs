using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using IPTV.Android.Services;
using IPTV.Services.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace IPTV.Android;

[Activity(
    Label = "IPTV",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        App.ConfigureServices(services =>
        {
            services.AddSingleton<IPlatformService>(new AndroidPlatformService(this));
        });

        base.OnCreate(savedInstanceState);
    }
}
