using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace IPTV
{
    [Activity(
        Theme = "@style/Maui.SplashTheme", 
        MainLauncher = true, 
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            ApplyImmersiveMode();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
            {
                ApplyImmersiveMode();
            }
        }

        private void ApplyImmersiveMode()
        {
            try
            {
                if (Window != null && Window.DecorView != null)
                {
                    WindowCompat.SetDecorFitsSystemWindows(Window, false);
                    var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
                    if (controller != null)
                    {
                        controller.Hide(WindowInsetsCompat.Type.SystemBars());
                        controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Ensure system bars/window insets configuration never causes startup crash
                System.Diagnostics.Debug.WriteLine($"[MainActivity] Immersive mode configuration skipped: {ex.Message}");
            }
        }
    }
}
