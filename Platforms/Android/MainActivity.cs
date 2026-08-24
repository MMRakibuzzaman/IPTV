using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;
using IPTV.Services;

namespace IPTV
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Window != null)
            {
                WindowCompat.SetDecorFitsSystemWindows(Window, false);
                var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
                if (controller != null)
                {
                    // Hide both status and navigation bars for true immersive mode
                    controller.Hide(WindowInsetsCompat.Type.SystemBars());
                    controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
                }
            }
        }
    }
}
