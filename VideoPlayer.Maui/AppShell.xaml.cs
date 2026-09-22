using VideoPlayer.Mvu;

namespace VideoPlayer.Maui
{
    public partial class AppShell : Shell
    {
        private readonly MvuHost mvuHost;

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
