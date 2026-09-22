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
