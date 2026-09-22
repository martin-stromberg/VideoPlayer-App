namespace VideoWebPlayer.Maui.Services;

/// <summary>
/// Dienst zur Verwaltung der Benutzeranmeldung und der gespeicherten Zugangsdaten.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Prüft, ob Zugangsdaten gespeichert sind.
    /// </summary>
    /// <returns><c>true</c>, wenn Benutzername und Passwort vorhanden sind; sonst <c>false</c>.</returns>
    bool HasCredentials();

    /// <summary>
    /// Ruft die gespeicherten Zugangsdaten ab.
    /// </summary>
    /// <param name="username">Der gespeicherte Benutzername (Element des Rückgabetupels).</param>
    /// <param name="password">Das gespeicherte Passwort (Element des Rückgabetupels).</param>
    /// <returns>Ein Tupel aus Benutzername und Passwort.</returns>
    (string username, string password) GetCredentials();

    /// <summary>
    /// Versucht eine Anmeldung und gibt ein Tupel (success, errorMessage) zurück. Bei Erfolg ist errorMessage null.
    /// </summary>
    /// <param name="username">Der Benutzername für die Anmeldung.</param>
    /// <param name="password">Das Passwort für die Anmeldung.</param>
    /// <param name="cancellationToken">Abbruchtoken für den Login-Vorgang.</param>
    /// <returns>Ein Tupel aus Erfolgsflag und optionaler Fehlermeldung.</returns>
    Task<(bool success, string? errorMessage)> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Löscht die gespeicherten Zugangsdaten.
    /// </summary>
    void ClearCredentials();
}
