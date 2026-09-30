using Foundation;
using UIKit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.iOS;
using IPTV.iOS.Services;
using IPTV.Services.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace IPTV.iOS;

[Register("AppDelegate")]
#pragma warning disable CA1711
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        App.ConfigureServices(services =>
        {
            services.AddSingleton<IPlatformService, IosPlatformService>();
        });

        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
