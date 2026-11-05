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
    /// </summary>
    public sealed class KeychainSecretStore : ISecretStore
    {
        /// <summary>Service-Attribut der Schlüsselbund-Einträge (= Bundle-Id der App).</summary>
        private const string ServiceName = "de.martinstromberg.videos.tv";

        private readonly ILogger<KeychainSecretStore> logger;

        /// <summary>Initialisiert eine neue Instanz der <see cref="KeychainSecretStore"/>-Klasse.</summary>
        /// <param name="logger">Logger für das Programmprotokoll.</param>
        public KeychainSecretStore(ILogger<KeychainSecretStore> logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public Task<string?> GetAsync(string key)
        {
            try
            {
                var match = SecKeyChain.QueryAsRecord(Query(key), out var status);
                if (status != SecStatusCode.Success || match?.ValueData is null)
                {
                    return Task.FromResult<string?>(null);
                }

                return Task.FromResult<string?>(
                    NSString.FromData(match.ValueData, NSStringEncoding.UTF8));
            }
            catch (Exception ex)
            {
                // Ein Lesefehler der Ablage verhält sich wie „nicht vorhanden"
                // (Muster: SecureStorageSecretStore) — nie werfen.
                logger.LogWarning(ex, "Lesen von '{Key}' fehlgeschlagen.", key);
                return Task.FromResult<string?>(null);
            }
        }

        /// <inheritdoc/>
        public Task SetAsync(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                SecKeyChain.Remove(Query(key));
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
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> RemoveAsync(string key)
        {
            var status = SecKeyChain.Remove(Query(key));
            return Task.FromResult(status == SecStatusCode.Success);
        }

        private static SecRecord Query(string key) =>
            new(SecKind.GenericPassword)
            {
                Service = ServiceName,
                Account = key
            };
    }
}
