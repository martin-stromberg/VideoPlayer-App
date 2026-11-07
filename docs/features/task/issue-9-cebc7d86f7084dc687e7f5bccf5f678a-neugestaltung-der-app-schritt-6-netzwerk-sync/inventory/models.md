# Modelle und Ablage

## `AppModel` (`VideoPlayer.Mvu/AppModel.cs`)

Unveränderliches Gesamtmodell (record, `init`-Properties). Relevante
Felder: `CurrentPage`, `IsStartupRunning`, `ServerHealth`,
`ConnectionState` (Start `Connecting`), `HasOfflineData`, abgeleitet
`IsOffline` (`ConnectionState == Offline`), `ActiveServerAddress`,
`LoginUsername`, `Settings`, `KnownServers`. **Fehlt:** Netzwerkstatus
(`IsOnline`), Synchronisationszustand.

## `PendingProgressSyncEntry` (`VideoPlayer.Storage/Entities/PendingProgressSyncEntry.cs`)

`[Table("pending_progress")]`, backup-pflichtig (`videoplayer.db3`):

| Spalte | Typ | Bemerkung |
|--------|-----|-----------|
| `Id` | `int` | `[PrimaryKey, AutoIncrement]` |
| `VideoId` | `int` | `[Indexed]` |
| `VideoType` | `string` | |
| `PositionTicks` | `long` | `TimeSpan.Ticks` |
| `UpdatedAtUtc` | `DateTime` | |

**Lücke:** keine `DurationTicks`-Spalte — der Server verlangt
`durationSeconds`. `sqlite-net`'s `CreateTableAsync` ergänzt fehlende
Spalten in bestehenden Tabellen nicht → explizite Migration
(`PRAGMA table_info` + `ALTER TABLE … ADD COLUMN`) in
`BackupDatabase.InitializeAsync` nötig.

## `BackupDatabase` (`VideoPlayer.Storage/BackupDatabase.cs`)

Lazy `SQLiteAsyncConnection` auf `layout.DataDatabasePath`;
`InitializeAsync()` legt `KnownServerEntry`, `BackupStateEntry`,
`PendingProgressSyncEntry` an (idempotent). Keine Migrationslogik
vorhanden.

## `CacheDatabase` (`VideoPlayer.Storage/CacheDatabase.cs`)

Lazy `SQLiteAsyncConnection` auf `layout.CacheDatabasePath`;
`InitializeAsync()` legt `CachedListEntry` (`cached_lists`),
`CachedFavoritesEntry` (`cached_favorites`), `DownloadManifestEntry`
(`download_manifest`) an; `HasOfflineDataAsync()` prüft die drei Tabellen.
Keine Schreib-/Lese-Methoden für den Metadaten-Cache vorhanden — direkter
Tabellenzugriff über `Connection` ist der Bestand.

## `CachedListEntry` / `CachedFavoritesEntry` / `DownloadManifestEntry`

`cached_lists`: `Key` (PK), `Json`, `UpdatedAtUtc` — generischer
JSON-Listen-Cache. `cached_favorites`: `Key` (PK), `VideoId`,
`VideoType`, `Json`, `UpdatedAtUtc`. `download_manifest`: Download-
Zuordnung (Schritt 11), für diese Anforderung nicht relevant.

## Serverseitige Ereignisse (Server-Repo `martin-stromberg/VideoPlayer`)

`MediaUpdateHub` (`VideoWebPlayer/Hubs/MediaUpdateHub.cs`), gemappt unter
`/hubs/mediaupdate` mit `RequireAuthorization(JwtBearer)`. Client-seitig
empfangene Methoden:

| Hub-Methode | Argumente | Auslöser (Server) |
|-------------|-----------|-------------------|
| `ContinueWatchingUpdated` | — | `MediaUpdateNotificationService` nach Fortschritts-/Weiterschauen-Änderungen (userbezogen via `Clients.User`) |
| `FavoritesChanged` | — | Favoritenänderungen (userbezogen) |
| `NewVideosScanned` | `long sourceId`, `int count` | Scan mit `count > 0` (alle Clients) |
| `StatusChanged` | `string message` | Statusmeldungen (alle Clients) |

Authentifizierung: Bearer-Token, bei WebSockets über `access_token` im
Query (serverseitig im `JwtBearerEvents.OnMessageReceived` verdrahtet) —
`AccessTokenProvider` des SignalR-Clients erzeugt genau diesen Parameter.
