using VideoPlayer.ApiClient.Discovery;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Führt MVU-Effekte aus: Navigation über die Shell, Server-Erreichbarkeits-
/// prüfung über <see cref="IServerHealthProbe"/>, Ablage-Initialisierung und
/// Einstellungs-Persistenz über <see cref="VideoPlayer.Storage"/> sowie die
/// Serververwaltung (Laden, Discovery, Kandidaten-Prüfung, Speichern).
/// </summary>
public sealed class ShellAppEffectHandler : IAppEffectHandler
{
    private readonly IServiceProvider services;
    private readonly IServerHealthProbe healthProbe;

    public ShellAppEffectHandler(IServiceProvider services, IServerHealthProbe healthProbe)
    {
        this.services = services;
        this.healthProbe = healthProbe;
    }

    public async Task HandleAsync(AppEffect effect, CancellationToken cancellationToken = default)
    {
        switch (effect)
        {
            case CheckServerHealthEffect:
                await RunHealthCheckAsync(cancellationToken);
                break;
            case NavigateToPageEffect navigate:
                await MainThread.InvokeOnMainThreadAsync(() => NavigateAsync(navigate.Page));
                break;
            case InitializeStorageEffect:
                await InitializeStorageAsync();
                break;
            case LoadSettingsEffect:
                await LoadSettingsAsync();
                break;
            case PersistSettingsEffect persist:
                SaveSettings(persist.Settings);
                break;
            case LoadServersEffect:
                await LoadServersAsync(cancellationToken);
                break;
            case DiscoverServersEffect:
                await DiscoverServersAsync(cancellationToken);
                break;
            case CheckCandidateServerEffect check:
                await CheckCandidateServerAsync(check.Address, check.DisplayName, cancellationToken);
                break;
            case PersistActiveServerEffect persist:
                await PersistActiveServerAsync(persist.Address, persist.DisplayName);
                break;
        }
    }

    private async Task InitializeStorageAsync()
    {
        // Fehler der Ablage-Initialisierung dürfen den Start nicht blockieren —
        // die App läuft mit Standardwerten weiter.
        try
        {
            // Die Auflösung des Layouts legt die Verzeichnisse an und wendet den
            // plattformseitigen Backup-Ausschluss an (AppStorageLayoutProvider).
            _ = services.GetRequiredService<AppStorageLayout>();
            await services.GetRequiredService<BackupDatabase>().InitializeAsync();
            await services.GetRequiredService<CacheDatabase>().InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Ablage-Initialisierung fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task LoadSettingsAsync()
    {
        AppSettings settings;
        try
        {
            settings = services.GetRequiredService<AppSettingsService>().Load();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Einstellungen laden fehlgeschlagen: {ex.Message}");
            settings = new AppSettings();
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new SettingsLoadedMessage(settings)));
    }

    private void SaveSettings(AppSettings settings)
    {
        try
        {
            services.GetRequiredService<AppSettingsService>().Save(settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Einstellungen speichern fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task LoadServersAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<KnownServer> servers = [];
        string? activeAddress = null;
        try
        {
            var knownServers = services.GetRequiredService<KnownServersService>();
            await knownServers.MigrateLegacyServerAddressAsync();
            servers = await knownServers.GetAllAsync();

            var configured = await services
                .GetRequiredService<IServerAddressProvider>()
                .GetConfiguredServerAddressAsync(cancellationToken);
            activeAddress = configured?.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Serververwaltung laden fehlgeschlagen: {ex.Message}");
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServersLoadedMessage(servers, activeAddress)));
    }

    private async Task DiscoverServersAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DiscoveredServer> found = [];
        try
        {
            found = await services
                .GetRequiredService<ServerDiscoveryService>()
                .DiscoverAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Server-Erkennung fehlgeschlagen: {ex.Message}");
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServerDiscoveryCompletedMessage(found)));
    }

    private async Task CheckCandidateServerAsync(string address, string? displayName, CancellationToken cancellationToken)
    {
        var success = false;
        try
        {
            success = await healthProbe.CheckAsync(new Uri(address), cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Kandidatenprüfung fehlgeschlagen: {ex.Message}");
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServerCheckCompletedMessage(address, displayName, success)));
    }

    private async Task PersistActiveServerAsync(string address, string? displayName)
    {
        try
        {
            await services.GetRequiredService<KnownServersService>()
                .SetActiveServerAsync(address, displayName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Server speichern fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task RunHealthCheckAsync(CancellationToken cancellationToken)
    {
        // Jeder Fehler der Prüfung führt zu „nicht erreichbar" — die App darf
        // nicht auf der Ladeanzeige hängen bleiben.
        bool reachable;
        try
        {
            reachable = await healthProbe.CheckAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Erreichbarkeitsprüfung fehlgeschlagen: {ex.Message}");
            reachable = false;
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServerHealthCheckCompletedMessage(reachable)));
    }

    private async Task NavigateAsync(AppPage page)
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is null)
        {
            return;
        }

        // Die Server-Einrichtung/-verwaltung hat keinen Menüeintrag: beim
        // Erststart wird sie als Window-Root gesetzt (die Shell existiert noch
        // nicht), aus den Einstellungen heraus als geschobene Shell-Route.
        if (page == AppPage.ServerSetup)
        {
            if (window.Page is AppShell && Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("serversetup");
            }
            else if (window.Page is not Pages.ServerSetupPage)
            {
                window.Page = services.GetRequiredService<Pages.ServerSetupPage>();
            }

            return;
        }

        // Übergang von der Start-Ladeanzeige zur Hauptnavigation.
        if (window.Page is not AppShell)
        {
            window.Page = services.GetRequiredService<AppShell>();

            // Shell.Current wird erst beim Anhängen der Shell gesetzt.
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (Shell.Current is null && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }
        }

        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync($"//{AppRoutes.ToRoute(page)}");
        }
    }
}
