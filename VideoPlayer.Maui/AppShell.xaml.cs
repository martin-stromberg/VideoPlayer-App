using VideoPlayer.Mvu;

namespace VideoPlayer.Maui
{
    /// <summary>
    /// Shell-Navigation der App (Hauptmenü mit Flyout-Einträgen). Registriert
    /// die Routen für Server-Einrichtung, Anmeldemaske, Medien-Details und
    /// Quellen-Browsing und synchronisiert Shell-Navigationen als
    /// <see cref="NavigatedMessage"/> in das MVU-Model.
    /// </summary>
    public partial class AppShell : Shell
    {
        private readonly MvuHost mvuHost;

        /// <summary>Initialisiert eine neue Instanz der <see cref="AppShell"/>-Klasse.</summary>
        /// <param name="mvuHost">MVU-Host für den Dispatch der Navigations-Nachrichten.</param>
        public AppShell(MvuHost mvuHost)
        {
            InitializeComponent();
            this.mvuHost = mvuHost;

            // Server-Einrichtung/-verwaltung: kein FlyoutItem, sondern eine
            // auf den Navigationsstack geschobene Route („Server wechseln").
            Routing.RegisterRoute("serversetup", typeof(Pages.ServerSetupPage));

            // Anmeldemaske: ebenfalls ohne Menüeintrag — wird bei
            // Sitzungsablauf über die Shell-Route erreicht (beim Start als
            // Window-Root statt der Shell).
            Routing.RegisterRoute("login", typeof(Pages.LoginPage));

            // Detailseite eines Mediums:
            // „mediadetails?mediaType=…&id=…&title=…[&show=…&season=…&episode=…]",
            // geschoben auf den Stack.
            Routing.RegisterRoute("mediadetails", typeof(Pages.MediaDetailsPage));

            // Quellen-Browsing (Genre-Filter, Suche, seitenweises Nachladen):
            // „sourceoverview?id=…&name=…", geschoben auf den Stack.
            Routing.RegisterRoute("sourceoverview", typeof(Pages.SourceOverviewPage));

            // Wiedergabeseite: „player", geschoben auf den Stack — der
            // Player-State wurde bereits durch PlayRequestedMessage belegt.
            Routing.RegisterRoute("player", typeof(Pages.PlayerPage));

            Navigated += OnShellNavigated;
        }

        private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
        {
            // Nutzeraktionen im Flyout laufen nativ über Shell; das MVU-Model wird
            // über eine NavigatedMessage synchronisiert.
            if (AppRoutes.TryGetPage(e.Current?.Location?.OriginalString, out var page))
            {
                _ = DispatchNavigatedAsync(page);
            }
        }

        private async Task DispatchNavigatedAsync(AppPage page)
        {
            try
            {
                await mvuHost.DispatchAsync(new NavigatedMessage(page));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] Navigated-Dispatch fehlgeschlagen: {ex.Message}");
            }
        }
    }
}
