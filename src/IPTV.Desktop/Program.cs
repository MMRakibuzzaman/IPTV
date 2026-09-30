using System;
using Avalonia;
using IPTV.Services.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace IPTV.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.ConfigureServices(services =>
        {
            services.AddSingleton<IPlatformService, Services.DesktopPlatformService>();
        });

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
