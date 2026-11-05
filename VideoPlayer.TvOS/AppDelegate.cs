using Foundation;
using UIKit;

namespace VideoPlayer.TvOS
{
    /// <summary>
    /// tvOS-App-Delegate; erzeugt das Hauptfenster und zeigt die leere
    /// Startansicht (<see cref="MainViewController"/>) an.
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        /// <inheritdoc/>
        public override UIWindow? Window { get; set; }

        /// <inheritdoc/>
        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            Window = new UIWindow(UIScreen.MainScreen.Bounds)
            {
                RootViewController = new MainViewController()
            };
            Window.MakeKeyAndVisible();
            return true;
        }
    }
}
