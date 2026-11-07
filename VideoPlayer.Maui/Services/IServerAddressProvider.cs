namespace VideoPlayer.Maui.Services;

/// <summary>Liefert die Basisadresse des VideoPlayer-Servers.</summary>
public interface IServerAddressProvider
{
    /// <summary>
    /// Die vom Anwender hinterlegte bzw. per Umgebungsvariable konfigurierte
    /// Serveradresse — <c>null</c>, wenn kein Server hinterlegt ist
    /// (Erststart-Erkennung). Der implizite IIS-Default gilt hier nicht als
    /// „hinterlegt".
    /// </summary>
    /// <param name="cancellationToken">Bricht die Auflösung ab.</param>
    /// <returns>Die hinterlegte Serveradresse oder <c>null</c>.</returns>
    Task<Uri?> GetConfiguredServerAddressAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Die effektiv zu verwendende Basisadresse: hinterlegter Server →
    /// Umgebungsvariable → lokaler IIS-Default. Liefert nie <c>null</c>.
    /// </summary>
    /// <param name="cancellationToken">Bricht die Auflösung ab.</param>
    /// <returns>Die effektiv zu verwendende Server-Basisadresse.</returns>
    Task<Uri> GetServerAddressAsync(CancellationToken cancellationToken = default);
}
