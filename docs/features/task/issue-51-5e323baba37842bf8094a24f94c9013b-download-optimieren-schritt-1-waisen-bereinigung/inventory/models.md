# Datenmodell

## `DownloadManifestEntry`
Datei: `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` | Primärschlüssel; Auftragsreihenfolge |
| `VideoId` | `int` | Medien-Id (indiziert) |
| `VideoType` | `string` | `movie`/`tvshowepisode` |
| `FilePath` | `string` | `{typ}_{mediaId}.mp4` im Video-Verzeichnis |
| `SizeBytes` | `long` | Dateigröße bei abgeschlossenen Downloads |
| `Title` | `string` | Anzeigetitel |
| `DownloadedBytes` / `TotalBytes` | `long` | Fortschrittsstand / erwartete Gesamtgröße |
| `Error` | `string?` | Fehlercode (`quota_exceeded`, `network`, `io`, `http_*`) |
| `State` | `string` | `queued`/`downloading`/`completed`/`failed`/`cancelled` |
| `ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber` | nullable | Serienkontext |
| `PlaylistId`, `PlaylistEntryId`, `PlaylistSortOrder` | nullable | Playlist-Bezug |
| `UpdatedAtUtc` | `DateTime` | Letzte Änderung |

## `DownloadListItem`
Datei: `VideoPlayer.Mvu/DownloadListItem.cs`

Anzeige-Snapshot; trägt `EntryId`, `MediaType`, `MediaId`, `Title`, `State`,
`DownloadedBytes`, `TotalBytes`, `Error` plus abgeleitete Anzeige-Props. Kein
`SizeBytes` — Dateigröße wird aktuell nicht angezeigt (Schritt 2 des Projekts).

## `DownloadsPageState`
Datei: `VideoPlayer.Mvu/DownloadsPageState.cs`

`Items`, `UsedBytes` (Dateisystem-Summe via `GetUsedBytes`), `QuotaGb`,
`QuotaText` („x GB von y GB belegt"), Lade-/Fehlerzustand.

## `AppStorageLayout`
Datei: `VideoPlayer.Storage/AppStorageLayout.cs`

`VideosDirectory` = `{NoBackupDirectory}/videos` (iOS: unter
`FileSystem.AppDataDirectory/nobackup`, vom Backup ausgenommen). Manifest in
`videoplayer-cache.db3` (Cache-DB, nicht backup-pflichtig) — DB-Verlust erzeugt
verwaiste Dateien.
