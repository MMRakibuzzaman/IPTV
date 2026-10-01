using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;

namespace IPTV.Android
{
    [Application]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            App.ConfigureServices(services =>
            {
                services.AddSingleton<IPTV.Services.Platform.IPlatformService>(new IPTV.Android.Services.AndroidPlatformService(null!));
            });

            return base.CustomizeAppBuilder(builder)
            .WithInterFont();
        }
    }
}
