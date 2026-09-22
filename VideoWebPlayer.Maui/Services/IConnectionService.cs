namespace VideoWebPlayer.Maui.Services;

/// <summary>
/// Dienst zum Aufbau und Prüfen der Verbindung zum VideoWebPlayer-Server.
/// </summary>
public interface IConnectionService
{
    /// <summary>
    /// Versucht, eine Verbindung zum Server unter der angegebenen Basisadresse herzustellen.
    /// </summary>
    /// <param name="baseAddress">Die Basisadresse des Servers.</param>
    /// <param name="cancellationToken">Abbruchtoken für den Verbindungsversuch.</param>
    /// <returns><c>true</c>, wenn der Server antwortet; sonst <c>false</c>.</returns>
    Task<bool> TryConnectAsync(string baseAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Startet den Verbindungsworkflow und aktualisiert das übergebene HomePageViewModel
    /// mit Sichtbarkeits-/Verbindungs-Flags.
    /// </summary>
    /// <param name="viewModel">Das zu aktualisierende ViewModel der Startseite.</param>
    /// <param name="services">Der ServiceProvider zum Auflösen der benötigten Dienste.</param>
    /// <param name="cancellationToken">Abbruchtoken für den Workflow.</param>
    /// <returns>Ein <see cref="ConnectionState"/>, der die nächste Aktion beschreibt.</returns>
    Task<ConnectionState> StartConnectionWorkflowAsync(ViewModels.HomePageViewModel viewModel, IServiceProvider services, CancellationToken cancellationToken = default);
}

/// <summary>
/// Beschreibt das Ergebnis des Verbindungsworkflows.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Die Verbindung wurde hergestellt und der Benutzer ist angemeldet.
    /// </summary>
    Connected,

    /// <summary>
    /// Es ist keine Serveradresse hinterlegt; der Server muss eingerichtet werden.
    /// </summary>
    NeedsServerSetup,

    /// <summary>
    /// Der Server ist erreichbar, aber eine Anmeldung ist erforderlich.
    /// </summary>
    NeedsLogin,

    /// <summary>
    /// Der Server ist nicht erreichbar; die App läuft im Offline-Modus.
    /// </summary>
    Offline
}
