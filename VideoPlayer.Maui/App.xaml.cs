using Microsoft.Extensions.Logging;
using VideoPlayer.Mvu;

namespace VideoPlayer.Maui
{
    /// <summary>
    /// Hauptanwendungsklasse der App. Erzeugt das Startfenster mit der
    /// Ladeanzeige und stößt einmalig den MVU-Start-Workflow an, der nach
    /// der Erreichbarkeitsprüfung die Root-Page gegen die AppShell tauscht.
    /// </summary>
    public partial class App : Application
    {
        private readonly MvuHost mvuHost;
        private readonly IServiceProvider services;
        private readonly ILogger<App> logger;
        private bool startupDispatched;

        /// <summary>Initialisiert eine neue Instanz der <see cref="App"/>-Klasse.</summary>
        /// <param name="mvuHost">MVU-Host für den Dispatch der Start-Nachricht.</param>
        /// <param name="services">Dienstanbieter zum Auflösen der Ladeanzeige.</param>
        /// <param name="logger">Logger für das Programmprotokoll.</param>
        public App(MvuHost mvuHost, IServiceProvider services, ILogger<App> logger)
        {
            InitializeComponent();
            this.mvuHost = mvuHost;
            this.services = services;
            this.logger = logger;

            // Die App ist dunkel-only wie die Webanwendung: alle Farben
            // kommen aus den Vp*-Design-Tokens, ein System-Light-Theme darf
            // keine Restwerte aktivieren.
            UserAppTheme = AppTheme.Dark;
        }

        /// <inheritdoc/>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Startphase: Ladeanzeige statt Shell; der Start-Workflow (MVU) tauscht
            // nach der Erreichbarkeitsprüfung die Root-Page gegen die AppShell.
            var window = new Window(services.GetRequiredService<Pages.LoadingPage>())
            {
                Title = "VideoPlayer"
            };
            window.Resumed += (_, _) => _ = mvuHost.DispatchAsync(new HomeListsReloadRequestedMessage());

#if DEBUG || DEV_API
            // Developer-API (nur Debug oder explizites Opt-in via DEV_API-Define):
            // lokaler HTTP-Server auf 127.0.0.1; Port steht in devapi.json
            // neben der Exe. Läuft mit dem Fenster-Lebenszyklus.
            var devApi = services.GetRequiredService<DevApi.DevApiService>();
            devApi.Start();
            window.Destroying += (_, _) => devApi.Dispose();
#endif

            if (!startupDispatched)
            {
                startupDispatched = true;
                _ = RunStartupAsync();
            }

            return window;
        }

        private async Task RunStartupAsync()
        {
            try
            {
                logger.LogInformation("App-Start: Start-Workflow ausgelöst.");
                await mvuHost.DispatchAsync(new StartupRequestedMessage());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Startup-Dispatch fehlgeschlagen.");
            }
        }
    }
}
