using System.Security.Cryptography;
using System.Text;
using Foundation;
using Microsoft.Extensions.Logging;
using Security;
using VideoPlayer.Storage;

namespace VideoPlayer.TvOS.Services
{
    /// <summary>
    /// <see cref="ISecretStore"/>-Adapter auf den tvOS-Schlüsselbund
    /// (<see cref="SecKeyChain"/> mit <see cref="SecKind.GenericPassword"/> —
    /// das Pendant zu <c>SecureStorage</c> der MAUI-App). Geheimnisse gelangen
    /// weder in die normale Einstellungsablage noch ins Cloud-Backup.
    /// Scheitert der Schlüsselbund ohne wirksame Entitlement
    /// (<c>errSecMissingEntitlement</c> — z. B. adhoc-signierte
    /// Simulator-Builds, denen die <c>keychain-access-groups</c>-Entitlement
    /// nicht gewaehrt wird), fallen die Schreib-/Lesezugriffe auf eine
    /// Dateiablage unterhalb des No-Backup-Verzeichnisses zurueck: Sie ist
    /// durch die App-Sandbox geschuetzt und fuer Entwicklungs-Builds
    /// ausreichend; auf signierten Geraete-Builds bleibt der Schlüsselbund
    /// der primaere Weg.
    /// </summary>
    public sealed class KeychainSecretStore : ISecretStore
    {
        /// <summary>Service-Attribut der Schlüsselbund-Einträge (= Bundle-Id der App).</summary>
        private const string ServiceName = "de.martinstromberg.videos.tv";

        private readonly ILogger<KeychainSecretStore> logger;
        private readonly string fallbackDirectory;

        /// <summary>Initialisiert eine neue Instanz der <see cref="KeychainSecretStore"/>-Klasse.</summary>
        /// <param name="logger">Logger für das Programmprotokoll.</param>
        /// <param name="layout">Ablage-Layout (liefert das No-Backup-Verzeichnis für den Fallback).</param>
        public KeychainSecretStore(ILogger<KeychainSecretStore> logger, AppStorageLayout layout)
        {
            this.logger = logger;
            ArgumentNullException.ThrowIfNull(layout);
            fallbackDirectory = Path.Combine(layout.NoBackupDirectory, "secrets");
        }

        /// <inheritdoc/>
        public Task<string?> GetAsync(string key)
        {
            try
            {
                var match = SecKeyChain.QueryAsRecord(Query(key), out var status);
                if (status == SecStatusCode.Success && match?.ValueData is not null)
                {
                    return Task.FromResult<string?>(
                        NSString.FromData(match.ValueData, NSStringEncoding.UTF8));
                }
            }
            catch (Exception ex)
            {
                // Ein Lesefehler der Ablage verhält sich wie „nicht vorhanden"
                // (Muster: SecureStorageSecretStore) — nie werfen.
                logger.LogWarning(ex, "Lesen von '{Key}' fehlgeschlagen.", key);
            }

            return Task.FromResult(ReadFallback(key));
        }

        /// <inheritdoc/>
        public Task SetAsync(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                SecKeyChain.Remove(Query(key));
                RemoveFallback(key);
                return Task.CompletedTask;
            }

            // Delete-then-Add: ein vorhandener Eintrag würde Add sonst mit
            // DuplicateItem ablehnen (Update ist hier unnoetig).
            SecKeyChain.Remove(Query(key));
            var record = Query(key);
            record.ValueData = NSData.FromString(value, NSStringEncoding.UTF8);
            var status = SecKeyChain.Add(record);
            if (status != SecStatusCode.Success)
            {
                logger.LogWarning("Schlüsselbund-Schreiben von '{Key}' fehlgeschlagen: {Status}.", key, status);
                WriteFallback(key, value);
            }
            else
            {
                // Schlüsselbund-Erfolg: ein etwaiger Fallback-Eintrag aus
                // früheren Läufen ist veraltet und wird entfernt.
                RemoveFallback(key);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> RemoveAsync(string key)
        {
            var status = SecKeyChain.Remove(Query(key));
            var fileRemoved = RemoveFallback(key);
            return Task.FromResult(status == SecStatusCode.Success || fileRemoved);
        }

        private string? ReadFallback(string key)
        {
            try
            {
                var path = FallbackPath(key);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Fallback-Lesen von '{Key}' fehlgeschlagen.", key);
                return null;
            }
        }

        private void WriteFallback(string key, string value)
        {
            try
            {
                Directory.CreateDirectory(fallbackDirectory);
                File.WriteAllText(FallbackPath(key), value);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Fallback-Schreiben von '{Key}' fehlgeschlagen.", key);
            }
        }

        private bool RemoveFallback(string key)
        {
            try
            {
                var path = FallbackPath(key);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Fallback-Löschen von '{Key}' fehlgeschlagen.", key);
            }

            return false;
        }

        private string FallbackPath(string key) =>
            Path.Combine(fallbackDirectory,
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))));

        private static SecRecord Query(string key) =>
            new(SecKind.GenericPassword)
            {
                Service = ServiceName,
                Account = key
            };
    }
}
