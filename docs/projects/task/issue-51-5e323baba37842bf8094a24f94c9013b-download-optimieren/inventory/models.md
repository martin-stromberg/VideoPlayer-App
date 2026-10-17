# Datenmodell

## `DownloadManifestEntry`
Datei: `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs`

Persistenter Manifest-Eintrag (SQLite-Tabelle `download_manifest` in der
Cache-Datenbank `videoplayer-cache.db3`, nicht backup-pflichtig).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` | Primärschlüssel, AutoIncrement; bestimmt die Auftragsreihenfolge |
| `VideoId` | `int` | Id des Mediums (indiziert) |
| `VideoType` | `string` | Medientyp (`movie`, `tvshowepisode`) |
| `FilePath` | `string` | Pfad der lokalen Videodatei (`{typ}_{id}.mp4` im Video-Verzeichnis) |
| `SizeBytes` | `long` | Größe der Datei bei abgeschlossenen Downloads |
| `Title` | `string` | Anzeigetitel (offline verfügbar) |
| `DownloadedBytes` | `long` | Bereits übertragene Bytes des laufenden/letzten Laufs |
| `TotalBytes` | `long` | Erwartete Gesamtgröße (`Content-Length`); 0 = unbekannt |
| `Error` | `string?` | Fehlercode des letzten Fehlschlags (`quota_exceeded`, `network`, `http_*` …) |
| `State` | `string` | Persistierter Zustand (`queued`/`downloading`/`completed`/`failed`/`cancelled`) |
| `ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber` | nullable | Serienkontext bei Episoden-Downloads |
| `PlaylistId`, `PlaylistEntryId`, `PlaylistSortOrder` | nullable | Playlist-Bezug playlist-gebundener Downloads |
| `UpdatedAtUtc` | `DateTime` | Letzte Aktualisierung (UTC) |

## `DownloadListItem`
Datei: `VideoPlayer.Mvu/DownloadListItem.cs`

Anzeige-Snapshot je Manifest-Eintrag für die Downloads-Seite.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `EntryId` | `int` | Manifest-Id (Sortierung) |
| `MediaType` | `string` | Normalisierter Medientyp |
| `MediaId` | `long` | Id des Mediums |
| `Title` | `string` | Anzeigetitel |
| `State` | `DownloadState` | Zustand |
| `DownloadedBytes`, `TotalBytes` | `long` | Fortschritt |
| `Error` | `string?` | Fehlercode |
| `IsActive`, `CanCancel`, `CanRetry`, `CanDelete`, `ShowProgress`, `HasError`, `Progress`, `ProgressPercentText`, `StateText`, `ErrorText`, `MediaTypeText` | abgeleitet | Anzeige-/Aktionslogik der Seite |

Hinweis: `SizeBytes` aus dem Manifest wird **nicht** in das Anzeige-Item
übernommen — eine Größenanzeige pro Download existiert derzeit nicht.

## `DownloadItemViewModel`
Datei: `VideoPlayer.Mvu/DownloadItemViewModel.cs`

Bindbares ViewModel um `DownloadListItem` (`Update(item)` für in-place
Aktualisierung ohne CollectionChanged).

## `DownloadsPageState`
Datei: `VideoPlayer.Mvu/DownloadsPageState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `IsLoading`, `Attempted`, `Error` | `bool`/`string?` | Lade-/Fehlerzustand |
| `Items` | `IReadOnlyList<DownloadListItem>` | Manifest-Stand |
| `UsedBytes` | `long` | Belegter Speicher (Summe der Dateien im Video-Verzeichnis) |
| `QuotaGb` | `int` | Konfiguriertes Kontingent |
| `QuotaText` | `string` | z. B. „13,1 GB von 50 GB belegt" |

## `AppSettings` (relevante Auszüge)
Datei: `VideoPlayer.Storage/AppSettings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `AutoDownloadEnabled` | `bool` | Automatische Downloads an/aus |
| `WifiOnlyDownloads` | `bool` | Downloads nur im WLAN (Standard: an) |
| `DownloadQuotaGb` | `int` | Kontingent 1–500 GB (Standard 5) |
