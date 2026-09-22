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
    public const string UsernameKey = "Username";
    public const string PasswordKey = "Passkey";
    public const string AuthTokenKey = "AuthToken";

    private readonly ISettingsStore settings;
    private readonly ISecretStore secrets;
    private readonly AppProfile profile;

    public AppSecretsService(ISettingsStore settings, ISecretStore secrets, AppProfile profile)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    /// <summary>Benutzername aus der normalen Ablage (kein Geheimnis).</summary>
    public string GetUsername() => settings.GetString(profile.PrefixKey(UsernameKey), string.Empty);

    /// <summary>Passwort aus der sicheren Ablage (inkl. Legacy-Migration aus der normalen Ablage).</summary>
    public async Task<string> GetPasswordAsync()
    {
        return await GetMigratedSecretAsync(PasswordKey);
    }

    /// <summary>Prüft, ob Benutzername und Passwort vorhanden sind.</summary>
    public async Task<bool> HasCredentialsAsync()
    {
        return !string.IsNullOrWhiteSpace(GetUsername())
            && !string.IsNullOrWhiteSpace(await GetPasswordAsync());
    }

    /// <summary>Speichert Benutzername (normale Ablage) und Passwort (sichere Ablage).</summary>
    public async Task SetCredentialsAsync(string username, string password)
    {
        settings.Set(profile.PrefixKey(UsernameKey), username ?? string.Empty);
        await secrets.SetAsync(profile.PrefixKey(PasswordKey), password ?? string.Empty);
    }

    /// <summary>Entfernt Benutzername, Passwort und Token des aktiven Profils.</summary>
    public async Task ClearCredentialsAsync()
    {
        settings.Remove(profile.PrefixKey(UsernameKey));
        await secrets.RemoveAsync(profile.PrefixKey(PasswordKey));
        await ClearAuthTokenAsync();
    }

    /// <summary>Anmelde-Token aus der sicheren Ablage (inkl. Legacy-Migration).</summary>
    public async Task<string?> GetAuthTokenAsync()
    {
        var token = await GetMigratedSecretAsync(AuthTokenKey);
        return string.IsNullOrEmpty(token) ? null : token;
    }

    /// <summary>Speichert das Anmelde-Token sicher und entfernt etwaige Reste in der normalen Ablage.</summary>
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

    /// <summary>
    /// Liest ein Geheimnis aus der sicheren Ablage; liegt es nur als Legacy-Wert
    /// in der normalen Ablage vor, wird es migriert und dort entfernt.
    /// </summary>
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
