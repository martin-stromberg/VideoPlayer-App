namespace VideoPlayer.Storage;

/// <summary>
/// Sichere Ablage für Geheimnisse (Plattform-Äquivalent von
/// <c>SecureStorage</c>: iOS-Keychain, Windows-Credential-Ablage). Inhalte
/// dieser Ablage gelangen nicht ins Cloud-Backup und nicht in die normale
/// Einstellungsablage.
/// </summary>
public interface ISecretStore
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<bool> RemoveAsync(string key);
}
