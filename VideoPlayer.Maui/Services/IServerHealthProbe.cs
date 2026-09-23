namespace VideoPlayer.Maui.Services;

/// <summary>Prüft die Erreichbarkeit eines VideoPlayer-Servers.</summary>
public interface IServerHealthProbe
{
    /// <summary>Prüft die aktuell aufgelöste Serveradresse.</summary>
    /// <param name="cancellationToken">Bricht die Prüfung ab.</param>
    /// <returns><c>true</c>, wenn der Server erreichbar ist.</returns>
    Task<bool> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>Prüft eine konkrete Kandidatenadresse (Einrichtung, Wechsel, Discovery).</summary>
    /// <param name="baseAddress">Zu prüfende Server-Basisadresse.</param>
    /// <param name="cancellationToken">Bricht die Prüfung ab.</param>
    /// <returns><c>true</c>, wenn der Server erreichbar ist.</returns>
    Task<bool> CheckAsync(Uri baseAddress, CancellationToken cancellationToken = default);
}
