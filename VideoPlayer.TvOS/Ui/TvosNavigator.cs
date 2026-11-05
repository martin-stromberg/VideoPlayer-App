using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Modellgetriebene Navigation des tvOS-Seitenrahmens — Pendant zur
    /// <c>NavigateAsync</c>-Logik des <c>ShellAppEffectHandler</c>: bildet
    /// <see cref="AppModel.CurrentPage"/> und den Verbindungszustand auf den
    /// Fensterinhalt ab (Ladeanzeige, Verbindungs-Zustandsseiten oder den
    /// Tab-Rahmen). Alle Darstellungswechsel sind idempotent.
    /// </summary>
    public sealed class TvosNavigator
    {
        private readonly Func<AppMessage, Task> dispatch;

        private MainViewController? main;
        private MenuTabBarController? tabBar;
        private ConnectionStatePageViewController? serverSetupPage;
        private ConnectionStatePageViewController? loginPage;

        /// <summary>Initialisiert eine neue Instanz der <see cref="TvosNavigator"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        public TvosNavigator(Func<AppMessage, Task> dispatch)
        {
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
        }

        /// <summary>Verbindet den Navigator mit dem Fensterrahmen.</summary>
        /// <param name="main">Der containerisierende Haupt-ViewController.</param>
        public void Attach(MainViewController main)
        {
            this.main = main ?? throw new ArgumentNullException(nameof(main));
        }

        /// <summary>
        /// Stellt die angegebene Seite dar: <see cref="AppPage.Loading"/>,
        /// <see cref="AppPage.ServerSetup"/> und <see cref="AppPage.Login"/>
        /// als Einzelansicht (ohne Menü), Menüseiten als Tab-Auswahl im
        /// <see cref="MenuTabBarController"/>.
        /// </summary>
        /// <param name="page">Die darzustellende Seite.</param>
        /// <returns>Eine abgeschlossene Aufgabe.</returns>
        public Task NavigateTo(AppPage page)
        {
            if (main is null)
            {
                return Task.CompletedTask;
            }

            switch (page)
            {
                case AppPage.Loading:
                    main.ShowLoading();
                    return Task.CompletedTask;
                case AppPage.ServerSetup:
                    serverSetupPage ??= new ConnectionStatePageViewController(
                        "Server einrichten", RetryStartup);
                    main.Show(serverSetupPage);
                    return Task.CompletedTask;
                case AppPage.Login:
                    loginPage ??= new ConnectionStatePageViewController(
                        "Anmelden", RetryStartup);
                    main.Show(loginPage);
                    return Task.CompletedTask;
                default:
                    tabBar ??= new MenuTabBarController(dispatch);
                    if (tabBar.Select(page))
                    {
                        main.Show(tabBar);
                    }

                    return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Spiegelt den aktuellen Modellstand auf die sichtbare Seite:
        /// stellt <see cref="AppModel.CurrentPage"/> dar (idempotent) und
        /// aktualisiert Status-/Fehlertexte der Zustandsseiten.
        /// </summary>
        /// <param name="model">Das aktuelle App-Modell.</param>
        public void Render(AppModel model)
        {
            _ = NavigateTo(model.CurrentPage);
            main?.UpdateLoadingStatus(model.StartupStatusText, model.StorageNotice);
            serverSetupPage?.UpdateStatus(ServerSetupStatusText(model), model.ServerSetupError ?? model.PairingError ?? model.BootstrapError);
            loginPage?.UpdateStatus(LoginStatusText(model), model.LoginError);
        }

        private static string ServerSetupStatusText(AppModel model)
        {
            if (model.IsBootstrapRunning)
            {
                return "Kopplung mit dem Server läuft …";
            }

            if (model.IsPairingRunning)
            {
                return "Geräte-Kopplung läuft …";
            }

            if (model.IsServerCheckRunning)
            {
                return "Verbindung zum Server wird geprüft …";
            }

            if (model.IsDiscoveryRunning)
            {
                return "Suche nach Servern im Netzwerk läuft …";
            }

            return model.IsPairingRequired
                ? "Der Server verlangt eine Geräte-Kopplung."
                : "Es ist noch kein Server eingerichtet.";
        }

        private static string LoginStatusText(AppModel model)
        {
            return model.IsLoginRunning
                ? "Anmeldung beim Server läuft …"
                : "Anmeldung am Server erforderlich.";
        }

        private void RetryStartup() =>
            _ = dispatch(new StartupRequestedMessage());
    }
}
