namespace VideoPlayer.Storage;

/// <summary>
/// Sichere Ablage für Geheimnisse (Plattform-Äquivalent von
/// <c>SecureStorage</c>: iOS-Keychain, Windows-Credential-Ablage). Inhalte
/// dieser Ablage gelangen nicht ins Cloud-Backup und nicht in die normale
/// Einstellungsablage.
/// </summary>
public interface ISecretStore
{
    /// <summary>Liest ein Geheimnis aus der sicheren Ablage.</summary>
    /// <param name="key">Schlüssel des Geheimnisses.</param>
    /// <returns>Der gespeicherte Wert oder <c>null</c>, wenn nicht vorhanden.</returns>
    Task<string?> GetAsync(string key);

    /// <summary>Speichert ein Geheimnis in der sicheren Ablage.</summary>
    /// <param name="key">Schlüssel des Geheimnisses.</param>
    /// <param name="value">Zu speichernder Wert.</param>
    Task SetAsync(string key, string value);

    /// <summary>Entfernt ein Geheimnis aus der sicheren Ablage.</summary>
    /// <param name="key">Schlüssel des Geheimnisses.</param>
    /// <returns><c>true</c>, wenn ein Eintrag entfernt wurde.</returns>
    Task<bool> RemoveAsync(string key);
}
