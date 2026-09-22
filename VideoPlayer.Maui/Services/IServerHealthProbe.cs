namespace VideoPlayer.Maui.Services;

/// <summary>Prüft die Erreichbarkeit eines VideoPlayer-Servers.</summary>
public interface IServerHealthProbe
{
    /// <summary>Prüft die aktuell aufgelöste Serveradresse.</summary>
    Task<bool> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>Prüft eine konkrete Kandidatenadresse (Einrichtung, Wechsel, Discovery).</summary>
    Task<bool> CheckAsync(Uri baseAddress, CancellationToken cancellationToken = default);
}
