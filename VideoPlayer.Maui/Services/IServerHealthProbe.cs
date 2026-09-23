using VideoPlayer.ApiClient;

namespace VideoPlayer.Maui.Services;

/// <summary>Prüft die Erreichbarkeit eines VideoPlayer-Servers.</summary>
public interface IServerHealthProbe
{
    /// <summary>Prüft die aktuell aufgelöste Serveradresse.</summary>
    /// <param name="cancellationToken">Bricht die Prüfung ab.</param>
    /// <returns>Ergebnis der Prüfung (erreichbar, Gate-Ablehnung, unerreichbar).</returns>
    Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>Prüft eine konkrete Kandidatenadresse (Einrichtung, Wechsel, Discovery).</summary>
    /// <param name="baseAddress">Zu prüfende Server-Basisadresse.</param>
    /// <param name="cancellationToken">Bricht die Prüfung ab.</param>
    /// <returns>Ergebnis der Prüfung (erreichbar, Gate-Ablehnung, unerreichbar).</returns>
    Task<HealthCheckResult> CheckAsync(Uri baseAddress, CancellationToken cancellationToken = default);
}
