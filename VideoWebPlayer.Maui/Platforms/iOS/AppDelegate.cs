using Foundation;

namespace VideoWebPlayer.Maui
{
    /// <summary>
    /// Application-Delegate der iOS-App; erstellt die MAUI-Anwendung.
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        /// <inheritdoc/>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
