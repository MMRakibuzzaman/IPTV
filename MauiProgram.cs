using CommunityToolkit.Maui;
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

            // Register our services
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<M3uParserService>();
            builder.Services.AddSingleton<PlaylistStorageService>();
            builder.Services.AddSingleton<PlayerService>();
            builder.Services.AddSingleton<HlsParserService>();
            builder.Services.AddSingleton<ExternalIntentService>();
            builder.Services.AddSingleton<HttpClient>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
