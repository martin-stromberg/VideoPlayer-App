using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// <see cref="ISecretStore"/>-Adapter auf die sichere Plattformablage
/// (<c>SecureStorage</c>: iOS-Keychain, Windows-Credential-Ablage). Inhalte
/// gelangen nicht ins Cloud-Backup und nicht in die normale
/// Einstellungsablage.
/// </summary>
public sealed class SecureStorageSecretStore : ISecretStore
{
    private readonly ISecureStorage secureStorage;

    /// <summary>Initialisiert eine neue Instanz der <see cref="SecureStorageSecretStore"/>-Klasse.</summary>
    /// <param name="secureStorage">Sichere Plattformablage (<c>SecureStorage.Default</c>).</param>
    public SecureStorageSecretStore(ISecureStorage secureStorage)
    {
        this.secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
    }

    /// <inheritdoc/>
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await secureStorage.GetAsync(key);
        }
        catch (Exception ex)
        {
            // SecureStorage kann auf manchen Plattformen/Laufzeiten nicht
            // verfügbar sein — dann verhält sich die Ablage wie „leer".
            System.Diagnostics.Debug.WriteLine($"[SecureStorageSecretStore] Lesen von '{key}' fehlgeschlagen: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync(string key, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            secureStorage.Remove(key);
            return;
        }

        await secureStorage.SetAsync(key, value);
    }

    /// <inheritdoc/>
    public Task<bool> RemoveAsync(string key)
    {
        return Task.FromResult(secureStorage.Remove(key));
    }
}
