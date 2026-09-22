using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Führt MVU-Effekte aus: Navigation über die Shell und die
/// Server-Erreichbarkeitsprüfung über <see cref="IServerHealthProbe"/>.
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
