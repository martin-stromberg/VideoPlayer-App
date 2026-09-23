using Android.App;
using Android.Content.PM;
using Android.OS;

namespace VideoWebPlayer.Maui
{
    /// <summary>
    /// Haupt-Activity der Android-App (Launcher-Einstiegspunkt).
    /// </summary>
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
