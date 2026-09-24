namespace VideoPlayer.Storage;

/// <summary>
/// Verwaltet Geheimnisse (Zugangsdaten, Anmelde-Token) in der sicheren
/// Plattformablage (<see cref="ISecretStore"/>) sowie den Benutzernamen in der
/// normalen Ablage — jeweils mit Profil-Präfix. Geheimnisse gelangen weder in
/// die normale Einstellungsablage noch ins Cloud-Backup; nach einer
/// Wiederherstellung ist daher ggf. eine erneute Anmeldung erforderlich.
/// </summary>
public sealed class AppSecretsService
{
    /// <summary>Ablageschlüssel des Benutzernamens (normale Ablage).</summary>
    public const string UsernameKey = "Username";

    /// <summary>Ablageschlüssel des Passworts (sichere Ablage).</summary>
    public const string PasswordKey = "Passkey";

    /// <summary>Ablageschlüssel des Anmelde-Tokens (sichere Ablage).</summary>
    public const string AuthTokenKey = "AuthToken";

    /// <summary>
    /// Ablageschlüssel des Refresh-Tokens (sichere Ablage) — gehört wie das
    /// Anmelde-Token zur aktiven Sitzung des Profils, nicht zu einem Server.
    /// </summary>
    public const string RefreshTokenKey = "RefreshToken";

    /// <summary>
    /// Basis-Schlüssel der Geräte-Tokens aus dem Pairing (sichere Ablage);
    /// der effektive Schlüssel hängt zusätzlich von der normalisierten
    /// Serveradresse ab.
    /// </summary>
    public const string DeviceTokenKey = "DeviceToken";

    private readonly ISettingsStore settings;
    private readonly ISecretStore secrets;
    private readonly AppProfile profile;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="settings">Nicht-geheime Ablage (Benutzername, Legacy-Werte).</param>
    /// <param name="secrets">Sichere Ablage (Passwort, Token).</param>
    /// <param name="profile">Aktives Profil für die Schlüssel-Präfixe.</param>
    public AppSecretsService(ISettingsStore settings, ISecretStore secrets, AppProfile profile)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    /// <summary>Benutzername aus der normalen Ablage (kein Geheimnis).</summary>
    /// <returns>Der gespeicherte Benutzername oder eine leere Zeichenfolge.</returns>
    public string GetUsername() => settings.GetString(profile.PrefixKey(UsernameKey), string.Empty);

    /// <summary>Passwort aus der sicheren Ablage (inkl. Legacy-Migration aus der normalen Ablage).</summary>
    /// <returns>Das gespeicherte Passwort oder eine leere Zeichenfolge.</returns>
    public async Task<string> GetPasswordAsync()
    {
        return await GetMigratedSecretAsync(PasswordKey);
    }

    /// <summary>Prüft, ob Benutzername und Passwort vorhanden sind.</summary>
    /// <returns><c>true</c>, wenn beide Werte nicht leer sind.</returns>
    public async Task<bool> HasCredentialsAsync()
    {
        return !string.IsNullOrWhiteSpace(GetUsername())
            && !string.IsNullOrWhiteSpace(await GetPasswordAsync());
    }

    /// <summary>Speichert Benutzername (normale Ablage) und Passwort (sichere Ablage).</summary>
    /// <param name="username">Zu speichernder Benutzername.</param>
    /// <param name="password">Zu speicherndes Passwort.</param>
    public async Task SetCredentialsAsync(string username, string password)
    {
        settings.Set(profile.PrefixKey(UsernameKey), username ?? string.Empty);
        await secrets.SetAsync(profile.PrefixKey(PasswordKey), password ?? string.Empty);
    }

    /// <summary>Entfernt Benutzername, Passwort, Token und Refresh-Token des aktiven Profils.</summary>
    public async Task ClearCredentialsAsync()
    {
        settings.Remove(profile.PrefixKey(UsernameKey));
        await secrets.RemoveAsync(profile.PrefixKey(PasswordKey));
        await ClearAuthTokenAsync();
        await ClearRefreshTokenAsync();
    }

    /// <summary>Anmelde-Token aus der sicheren Ablage (inkl. Legacy-Migration).</summary>
    /// <returns>Das gespeicherte Token oder <c>null</c>.</returns>
    public async Task<string?> GetAuthTokenAsync()
    {
        var token = await GetMigratedSecretAsync(AuthTokenKey);
        return string.IsNullOrEmpty(token) ? null : token;
    }

    /// <summary>Speichert das Anmelde-Token sicher und entfernt etwaige Reste in der normalen Ablage.</summary>
    /// <param name="token">Zu speicherndes Anmelde-Token.</param>
    public async Task SetAuthTokenAsync(string token)
    {
        await secrets.SetAsync(profile.PrefixKey(AuthTokenKey), token ?? string.Empty);
        settings.Remove(profile.PrefixKey(AuthTokenKey));
    }

    /// <summary>Entfernt das Anmelde-Token aus beiden Ablagen.</summary>
    public async Task ClearAuthTokenAsync()
    {
        settings.Remove(profile.PrefixKey(AuthTokenKey));
        await secrets.RemoveAsync(profile.PrefixKey(AuthTokenKey));
    }

    /// <summary>Refresh-Token der aktiven Sitzung aus der sicheren Ablage.</summary>
    /// <returns>Das gespeicherte Refresh-Token oder <c>null</c>.</returns>
    public async Task<string?> GetRefreshTokenAsync()
    {
        var token = await secrets.GetAsync(profile.PrefixKey(RefreshTokenKey));
        return string.IsNullOrEmpty(token) ? null : token;
    }

    /// <summary>Speichert das Refresh-Token sicher (Rotation: ersetzt den Vorgänger).</summary>
    /// <param name="token">Zu speicherndes Refresh-Token.</param>
    public async Task SetRefreshTokenAsync(string token)
    {
        await secrets.SetAsync(profile.PrefixKey(RefreshTokenKey), token ?? string.Empty);
    }

    /// <summary>Entfernt das Refresh-Token aus der sicheren Ablage.</summary>
    public async Task ClearRefreshTokenAsync()
    {
        await secrets.RemoveAsync(profile.PrefixKey(RefreshTokenKey));
    }

    /// <summary>
    /// Geräte-Token eines Servers aus der sicheren Ablage. Die Tokens sind
    /// profil- und serveradress-spezifisch hinterlegt.
    /// </summary>
    /// <param name="serverAddress">Serveradresse (wird normalisiert).</param>
    /// <returns>Das gespeicherte Geräte-Token oder <c>null</c>.</returns>
    public async Task<string?> GetDeviceTokenAsync(string serverAddress)
    {
        var token = await secrets.GetAsync(DeviceTokenSecretKey(serverAddress));
        return string.IsNullOrEmpty(token) ? null : token;
    }

    /// <summary>Prüft, ob für den Server ein Geräte-Token hinterlegt ist.</summary>
    /// <param name="serverAddress">Serveradresse (wird normalisiert).</param>
    /// <returns><c>true</c>, wenn ein Token in der sicheren Ablage liegt.</returns>
    public async Task<bool> HasDeviceTokenAsync(string serverAddress)
    {
        return await GetDeviceTokenAsync(serverAddress) is not null;
    }

    /// <summary>Speichert ein Geräte-Token ausschließlich in der sicheren Ablage.</summary>
    /// <param name="serverAddress">Serveradresse (wird normalisiert).</param>
    /// <param name="token">Zu speicherndes Geräte-Token.</param>
    public async Task SetDeviceTokenAsync(string serverAddress, string token)
    {
        await secrets.SetAsync(DeviceTokenSecretKey(serverAddress), token ?? string.Empty);
    }

    /// <summary>Entfernt das Geräte-Token eines Servers aus der sicheren Ablage.</summary>
    /// <param name="serverAddress">Serveradresse (wird normalisiert).</param>
    public async Task ClearDeviceTokenAsync(string serverAddress)
    {
        await secrets.RemoveAsync(DeviceTokenSecretKey(serverAddress));
    }

    /// <summary>
    /// Effektiver Ablageschlüssel eines Geräte-Tokens: Profil-Präfix plus
    /// normalisierte Serveradresse als Unterscheidungsmerkmal.
    /// </summary>
    /// <param name="serverAddress">Serveradresse (wird normalisiert).</param>
    /// <returns>Der vollständige Ablageschlüssel.</returns>
    private string DeviceTokenSecretKey(string serverAddress)
    {
        var normalized = ServerAddress.TryNormalize(serverAddress, out var value)
            ? value
            : serverAddress?.Trim() ?? string.Empty;
        return profile.PrefixKey($"{DeviceTokenKey}:{normalized}");
    }

    /// <summary>
    /// Liest ein Geheimnis aus der sicheren Ablage; liegt es nur als Legacy-Wert
    /// in der normalen Ablage vor, wird es migriert und dort entfernt.
    /// </summary>
    /// <param name="key">Schlüssel ohne Profil-Präfix.</param>
    /// <returns>Das Geheimnis oder eine leere Zeichenfolge.</returns>
    private async Task<string> GetMigratedSecretAsync(string key)
    {
        var prefixed = profile.PrefixKey(key);
        var secureValue = await secrets.GetAsync(prefixed);
        if (!string.IsNullOrEmpty(secureValue))
        {
            return secureValue;
        }

        var legacyValue = settings.GetString(prefixed, string.Empty);
        if (string.IsNullOrEmpty(legacyValue))
        {
            return string.Empty;
        }

        await secrets.SetAsync(prefixed, legacyValue);
        settings.Remove(prefixed);
        return legacyValue;
    }
}
