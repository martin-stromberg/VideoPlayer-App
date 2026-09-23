using Foundation;

namespace VideoPlayer.Maui
{
    /// <summary>Mac-Catalyst-App-Delegate; erzeugt die MAUI-App über <see cref="MauiProgram"/>.</summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        /// <inheritdoc/>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
