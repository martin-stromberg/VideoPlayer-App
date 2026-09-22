# Datenmodell

## `AppModel`

Datei: `VideoPlayer.Mvu/AppModel.cs` — unveränderliches Gesamtmodell (record).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `CurrentPage` | `AppPage` | Aktuell angezeigte Seite; Startwert `Loading` |
| `IsStartupRunning` | `bool` | Start-Workflow (Erreichbarkeitsprüfung) läuft; Default `true` |
| `ServerHealth` | `ServerHealthStatus` | Ergebnis der Server-Erreichbarkeitsprüfung; Default `Unknown` |
| `Settings` | `AppSettings` | Geladene Einstellungen (bis zum Laden Defaults) |
| `KnownServers` | `IReadOnlyList<KnownServer>` | Bekannte Server, zuletzt verwendet zuerst |
| `ActiveServerAddress` | `string?` | Kanonische Adresse des aktiven Servers; `null` → Erststart-Einrichtung |
| `DiscoveredServers` | `IReadOnlyList<DiscoveredServer>` | Per Netzwerk-Erkennung gefundene Kandidaten |
| `IsDiscoveryRunning` | `bool` | Netzwerk-Erkennung läuft |
| `DiscoveryAttempted` | `bool` | Erkennung lief mindestens einmal |
| `IsServerCheckRunning` | `bool` | Verbindungsprüfung eines Kandidaten läuft |
| `ServerSetupError` | `string?` | Inline-Fehlertext der Server-Einrichtung |
| `ServerHealthText` | `string` (berechnet) | Anzeigetext zum Serverstatus |

Kein Verbindungszustand (verbunden/Anmeldung/Servereinrichtung/offline),
kein Anmelde-UI-Zustand, kein Offline-Daten-Flag vorhanden.

## `KnownServer`

Datei: `VideoPlayer.Storage/KnownServer.cs` — Domänenmodell (record).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Address` | `string` (required) | Kanonische Serveradresse |
| `DisplayName` | `string` | Anzeigename, leer wenn keiner vergeben |
| `LastUsedAtUtc` | `DateTime` | Zeitpunkt der letzten Nutzung (UTC) |
| `NameOrAddress` | `string` (berechnet) | Anzeigename oder Adresse (Listenanzeige) |

## `AuthToken`

Datei: `VideoPlayer.ApiClient/Models/AuthToken.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Token` | `string` | JWT der Serveranmeldung |
| `Expires` | `DateTime` | Ablaufzeitpunkt |

## `LoginRequest`

Datei: `VideoPlayer.ApiClient/Models/LoginRequest.cs` — Wire-Modell für
`POST api/auth/login` (`Email`, `Password`).

## `BackupStateEntry`

Datei: `VideoPlayer.Storage/Entities/BackupStateEntry.cs` — Schlüssel-Wert-Tabelle
`backup_state` in der backup-pflichtigen `BackupDatabase`; Schlüssel
`LastConnectedServerKey` hält die zuletzt verbundene Serveradresse (kommt ins
Cloud-Backup → automatische Wiederverbindung nach Wiederherstellung).

## `KnownServerEntry`

Datei: `VideoPlayer.Storage/Entities/KnownServerEntry.cs` — Tabelle
`known_servers` (`Address` Unique, `DisplayName`, `LastUsedAtUtc`),
backup-pflichtig.

## `CachedListEntry` / `CachedFavoritesEntry` / `DownloadManifestEntry`

Dateien: `VideoPlayer.Storage/Entities/` — Tabellen `cached_lists`,
`cached_favorites`, `download_manifest` in der nicht backup-pflichtigen
`CacheDatabase`. Aktuell leer (Befüllung erst in Schritten 7/11); Grundlage
für die Bewertung „Offline-Daten vorhanden".

## `AppSecretsService`-Schlüssel (Ablageorte)

| Schlüssel | Ablage | Backup-relevant |
|-----------|--------|-----------------|
| `Username` | `ISettingsStore` (normal, nicht geheim) | bleibt ggf. erhalten → Vorbefüllung der Anmeldemaske |
| `Passkey` | `ISecretStore` (SecureStorage) | **nicht** im Cloud-Backup |
| `AuthToken` | `ISecretStore` (SecureStorage) | **nicht** im Cloud-Backup |
