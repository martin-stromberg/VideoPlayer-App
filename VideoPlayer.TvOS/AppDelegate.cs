using Foundation;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;
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
            // Hintergrund-Session herstellen, damit Transfers, die die
            // App überlebt haben, ihre Delegate-Rückrufe erhalten
            // (Fortsetzung der Downloads nach Suspension).
            composition?.DownloadBackend.EnsureSession();

            // Rückkehr in den Vordergrund: Startseiten-Listen neu laden
            // (Pendant zum window.Resumed-Dispatch der MAUI-App).
            if (host is not null)
            {
                _ = host.DispatchAsync(new HomeListsReloadRequestedMessage());
                // Ablauf-/Abgleichslauf des Download-Manifests
                // (abgelaufene/fehlende Dateien, Waisen) sowie die
                // Neu-Einplanung verwaister Aufträge.
                _ = host.DispatchAsync(new AppActivatedMessage());
            }
        }

        /// <summary>
        /// tvOS startet die App für abgeschlossene
        /// Hintergrund-Session-Events neu — der Aufruf wird an das
        /// Download-Backend weitergereicht (Pendant zum iOS-Delegate
        /// der MAUI-App; das Binding trägt den Selector nicht als
        /// Basistyp-Member — Registrierung über <see cref="ExportAttribute"/>).
        /// </summary>
        /// <param name="application">Die laufende Anwendung.</param>
        /// <param name="sessionIdentifier">Kennung der Hintergrund-Session.</param>
        /// <param name="completionHandler">Der aufzurufende System-Handler.</param>
        [Export("application:handleEventsForBackgroundURLSession:completionHandler:")]
        public void HandleEventsForBackgroundUrlSession(
            UIApplication application, string sessionIdentifier, Action completionHandler)
        {
            if (TvosBackgroundDownloadBackend.Shared is { } backend)
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
