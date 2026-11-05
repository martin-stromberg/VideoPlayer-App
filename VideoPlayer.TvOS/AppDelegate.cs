using Foundation;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Ui;

namespace VideoPlayer.TvOS
{
    /// <summary>
    /// tvOS-App-Delegate: baut die <see cref="AppComposition"/> auf,
    /// verdrahtet den MVU-Zyklus mit dem App-Lebenszyklus
    /// (<see cref="MvuHost"/> + <see cref="Services.TvosAppEffectHandler"/>)
    /// und stößt beim Start den Start-Workflow
    /// (<see cref="StartupRequestedMessage"/>) sowie bei jeder Rückkehr in
    /// den Vordergrund das Nachladen der Startseiten-Listen an.
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private AppComposition? composition;
        private MvuHost? host;

        /// <inheritdoc/>
        public override UIWindow? Window { get; set; }

        /// <inheritdoc/>
        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            composition = AppComposition.Create();

            var main = new MainViewController();
            host = composition.CreateRuntime(main);

            Window = new UIWindow(UIScreen.MainScreen.Bounds)
            {
                RootViewController = main
            };
            Window.MakeKeyAndVisible();

            // Start-Workflow: Ablage, Einstellungen, Serververwaltung,
            // Netzwerk-Monitoring und Cache-First-Listen — der Abschluss
            // navigiert modellgetrieben (Verbindungszustand → Seite).
            _ = host.DispatchAsync(new StartupRequestedMessage());
            return true;
        }

        /// <inheritdoc/>
        public override void OnActivated(UIApplication application)
        {
            base.OnActivated(application);
            // Rückkehr in den Vordergrund: Startseiten-Listen neu laden
            // (Pendant zum window.Resumed-Dispatch der MAUI-App).
            if (host is not null)
            {
                _ = host.DispatchAsync(new HomeListsReloadRequestedMessage());
            }
        }
    }
}
