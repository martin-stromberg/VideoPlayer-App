using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Führt MVU-Effekte aus: Navigation über die Shell, Server-Erreichbarkeits-
/// prüfung über <see cref="IServerHealthProbe"/> sowie Ablage-Initialisierung
/// und Einstellungs-Persistenz über <see cref="VideoPlayer.Storage"/>.
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
