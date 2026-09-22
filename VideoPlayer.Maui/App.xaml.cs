using VideoPlayer.Mvu;

namespace VideoPlayer.Maui
{
    public partial class App : Application
    {
        private readonly MvuHost mvuHost;
        private readonly IServiceProvider services;
        private bool startupDispatched;

        public App(MvuHost mvuHost, IServiceProvider services)
        {
            InitializeComponent();
            this.mvuHost = mvuHost;
            this.services = services;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Startphase: Ladeanzeige statt Shell; der Start-Workflow (MVU) tauscht
            // nach der Erreichbarkeitsprüfung die Root-Page gegen die AppShell.
            var window = new Window(services.GetRequiredService<Pages.LoadingPage>())
            {
                Title = "VideoPlayer"
            };

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
                await mvuHost.DispatchAsync(new StartupRequestedMessage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Startup-Dispatch fehlgeschlagen: {ex.Message}");
            }
        }
    }
}
