using Foundation;
using UIKit;
using VideoPlayer.Maui.Services;

namespace VideoPlayer.Maui
{
    /// <summary>iOS-App-Delegate; erzeugt die MAUI-App über <see cref="MauiProgram"/>.</summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        /// <inheritdoc/>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        /// <summary>
        /// iOS startet die App für abgeschlossene Hintergrund-Session-Events
        /// neu — der Aufruf wird an das Download-Backend weitergereicht.
        /// </summary>
        /// <param name="application">Die laufende Anwendung.</param>
        /// <param name="sessionIdentifier">Kennung der Hintergrund-Session.</param>
        /// <param name="completionHandler">Der aufzurufende System-Handler.</param>
        [Export("application:handleEventsForBackgroundURLSession:completionHandler:")]
        public void HandleEventsForBackgroundUrlSession(
            UIApplication application, string sessionIdentifier, Action completionHandler)
        {
            if (IosBackgroundDownloadBackend.Shared is { } backend)
            {
                backend.HandleEventsForBackgroundUrlSession(sessionIdentifier, completionHandler);
            }
            else
            {
                completionHandler();
            }
        }
    }
}
