using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SecureStorageSecretStore> logger;

    /// <summary>Initialisiert eine neue Instanz der <see cref="SecureStorageSecretStore"/>-Klasse.</summary>
    /// <param name="secureStorage">Sichere Plattformablage (<c>SecureStorage.Default</c>).</param>
    /// <param name="logger">Logger für das Programmprotokoll.</param>
    public SecureStorageSecretStore(
        ISecureStorage secureStorage,
        ILogger<SecureStorageSecretStore> logger)
    {
        this.secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
        this.logger = logger;
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
            logger.LogWarning(ex, "Lesen von '{Key}' fehlgeschlagen.", key);
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
