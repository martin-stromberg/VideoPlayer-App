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
    Task<Uri?> GetConfiguredServerAddressAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Die effektiv zu verwendende Basisadresse: hinterlegter Server →
    /// Umgebungsvariable → lokaler IIS-Default. Liefert nie <c>null</c>.
    /// </summary>
    Task<Uri> GetServerAddressAsync(CancellationToken cancellationToken = default);
}
