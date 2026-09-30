using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using IPTV.Repositories;
using IPTV.Services;
using IPTV.Services.Platform;
using IPTV.ViewModels;
using IPTV.Views;
using Microsoft.Extensions.DependencyInjection;

namespace IPTV;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public static void ConfigureServices(Action<IServiceCollection>? configurePlatform = null)
    {
        var services = new ServiceCollection();

        services.AddHttpClient();
        services.AddSingleton<IPlaylistRepository, PlaylistRepository>();
        services.AddSingleton<IM3uParserService, M3uParserService>();
        services.AddSingleton<IHlsParserService, HlsParserService>();
        services.AddSingleton<IPlayerService, PlayerService>();
        services.AddSingleton<MainViewModel>();

        // Custom platform service registration
        configurePlatform?.Invoke(services);

        // Fallback default platform service if not registered
        services.AddSingleton<IPlatformService, DefaultPlatformService>();

        Services = services.BuildServiceProvider();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (Services == null)
        {
            ConfigureServices();
        }

        var mainVm = Services!.GetRequiredService<MainViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm
            };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView { DataContext = mainVm };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = mainVm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}