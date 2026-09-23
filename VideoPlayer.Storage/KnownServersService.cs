using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Storage;

/// <summary>
/// Verwaltet die Liste bekannter Server und den zuletzt verbundenen Server in
/// der backup-pflichtigen <see cref="BackupDatabase"/> (Tabellen
/// <c>known_servers</c> und <c>backup_state</c>). Enthält die einmalige
/// Migration des bisherigen <c>ServerAddress</c>-Schlüssels aus der
/// <see cref="ISettingsStore"/>-Ablage (Schritt 3).
/// </summary>
public sealed class KnownServersService
{
    /// <summary>Legacy-Ablageschlüssel aus Schritt 3 (profil-präfixiert).</summary>
    public const string LegacyServerAddressKey = "ServerAddress";

    private readonly BackupDatabase database;
    private readonly ISettingsStore settingsStore;
    private readonly AppProfile profile;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="database">Backup-pflichtige Datenbank für die Serverliste.</param>
    /// <param name="settingsStore">Nicht-geheime Ablage (Legacy-Schlüssel der Migration).</param>
    /// <param name="profile">Aktives Profil für die Schlüssel-Präfixe.</param>
    public KnownServersService(BackupDatabase database, ISettingsStore settingsStore, AppProfile profile)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    /// <summary>Alle bekannten Server, zuletzt verwendet zuerst.</summary>
    /// <returns>Liste der bekannten Server (ggf. leer).</returns>
    public async Task<IReadOnlyList<KnownServer>> GetAllAsync()
    {
        var entries = await database.Connection
            .Table<KnownServerEntry>()
            .OrderByDescending(e => e.LastUsedAtUtc)
            .ToListAsync();

        return entries
            .Select(e => new KnownServer
            {
                Address = e.Address,
                DisplayName = e.DisplayName,
                LastUsedAtUtc = e.LastUsedAtUtc
            })
            .ToList();
    }

    /// <summary>Adresse des zuletzt verbundenen Servers oder <c>null</c>.</summary>
    /// <returns>Die kanonische Adresse oder <c>null</c>, wenn noch keine Verbindung bestand.</returns>
    public async Task<string?> GetLastConnectedAddressAsync()
    {
        var state = await database.Connection
            .FindAsync<BackupStateEntry>(BackupStateEntry.LastConnectedServerKey);
        return string.IsNullOrWhiteSpace(state?.Value) ? null : state!.Value;
    }

    /// <summary>
    /// Speichert eine erfolgreich geprüfte Verbindung: Upsert in
    /// <c>known_servers</c> (inkl. <c>LastUsedAtUtc</c>) und Setzen des
    /// zuletzt verbundenen Servers in <c>backup_state</c>.
    /// </summary>
    /// <param name="address">Serveradresse (wird normalisiert).</param>
    /// <param name="displayName">Optionaler Anzeigename; leere Werte lassen den bisherigen Namen unverändert.</param>
    /// <returns>Der gespeicherte bekannte Server.</returns>
    public async Task<KnownServer> SetActiveServerAsync(string address, string? displayName = null)
    {
        var normalized = ServerAddress.Normalize(address);
        var now = DateTime.UtcNow;

        var entry = await database.Connection
            .Table<KnownServerEntry>()
            .Where(e => e.Address == normalized)
            .FirstOrDefaultAsync();

        if (entry is null)
        {
            entry = new KnownServerEntry { Address = normalized };
        }

        entry.LastUsedAtUtc = now;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            entry.DisplayName = displayName.Trim();
        }

        if (entry.Id == 0)
        {
            await database.Connection.InsertAsync(entry);
        }
        else
        {
            await database.Connection.UpdateAsync(entry);
        }

        await database.Connection.InsertOrReplaceAsync(new BackupStateEntry
        {
            Key = BackupStateEntry.LastConnectedServerKey,
            Value = normalized
        });

        return new KnownServer
        {
            Address = normalized,
            DisplayName = entry.DisplayName,
            LastUsedAtUtc = now
        };
    }

    /// <summary>
    /// Übernimmt eine in der <see cref="ISettingsStore"/>-Ablage hinterlegte
    /// <c>ServerAddress</c> (Schritt 3) einmalig in die Datenbank: der Server
    /// wird in die Liste aufgenommen und — sofern noch kein zuletzt
    /// verbundener Server existiert — als aktiver Server gesetzt. Der
    /// Legacy-Schlüssel wird anschließend entfernt.
    /// </summary>
    public async Task MigrateLegacyServerAddressAsync()
    {
        var legacyKey = profile.PrefixKey(LegacyServerAddressKey);
        var legacyAddress = settingsStore.GetString(legacyKey, string.Empty);
        if (string.IsNullOrWhiteSpace(legacyAddress))
        {
            return;
        }

        if (ServerAddress.TryNormalize(legacyAddress, out var normalized))
        {
            var existing = await database.Connection
                .Table<KnownServerEntry>()
                .Where(e => e.Address == normalized)
                .FirstOrDefaultAsync();
            if (existing is null)
            {
                await database.Connection.InsertAsync(new KnownServerEntry
                {
                    Address = normalized,
                    LastUsedAtUtc = DateTime.UtcNow
                });
            }

            if (await GetLastConnectedAddressAsync() is null)
            {
                await database.Connection.InsertOrReplaceAsync(new BackupStateEntry
                {
                    Key = BackupStateEntry.LastConnectedServerKey,
                    Value = normalized
                });
            }
        }

        settingsStore.Remove(legacyKey);
    }
}
