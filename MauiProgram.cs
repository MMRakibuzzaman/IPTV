using CommunityToolkit.Maui;
using IPTV.Repositories;
using IPTV.Services;
using Microsoft.Extensions.Logging;

namespace IPTV
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkitMediaElement()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // HTTP Client
            builder.Services.AddHttpClient();

            // Register repositories and application services
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<IPlaylistRepository, PlaylistRepository>();
            builder.Services.AddSingleton<IM3uParserService, M3uParserService>();
            builder.Services.AddSingleton<IHlsParserService, HlsParserService>();
            builder.Services.AddSingleton<IPlayerService, PlayerService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
