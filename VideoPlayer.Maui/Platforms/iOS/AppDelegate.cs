using Foundation;

namespace VideoPlayer.Maui
{
    /// <summary>iOS-App-Delegate; erzeugt die MAUI-App über <see cref="MauiProgram"/>.</summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        /// <inheritdoc/>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
