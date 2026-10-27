# Bestandsaufnahme — Datenmodell und Ablage

## `DownloadManifestEntry`

Datei: `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs` — Tabelle
`download_manifest` in der `CacheDatabase` (nicht backup-pflichtig).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` | Primärschlüssel (AutoIncrement) |
| `VideoId` | `int` | Id des heruntergeladenen Videos (`[Indexed]`) |
| `VideoType` | `string` | Medientyp (`movie`, `episode`, …) |
| `FilePath` | `string` | Pfad der lokalen Videodatei |
| `SizeBytes` | `long` | Dateigröße in Bytes |
| `State` | `string` | Download-Status als String (`"completed"` wird von `PlaybackSourceService` gelesen) |
| `UpdatedAtUtc` | `DateTime` | Letzte Aktualisierung (UTC) |

Fehlend für die Anforderung: `Title` (Anzeige), `DownloadedBytes`/
`TotalBytes` (Fortschritt), `Error` (Fehlertext), ggf. `CreatedAtUtc`/
Sortierung. Erweiterung via `ALTER TABLE`-freier sqlite-net-Erweiterung:
`CreateTableAsync` ergänzt neue Spalten automatisch (`sqlite-net` fügt
fehlende Spalten per `ALTER TABLE` hinzu — gängiges Verhalten der
eingesetzten Bibliothek; zu verifizieren).

## `AppSettings`

Datei: `VideoPlayer.Storage/AppSettings.cs` — Record, persistiert über
`AppSettingsService`/`ISettingsStore` (Preferences).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `AutoDownloadEnabled` | `bool` | Automatische Downloads an/aus (Default `false`) |
| `PreloadEpisodeCount` | `int` | Vorzuladende Episoden (1–10, Default 3) |
| `AutoResumeEnabled` | `bool` | Auto-Fortsetzung (Default `true`) |
| `AutoResumeRemainingSeconds` | `int` | Zeitfenster Auto-Fortsetzung (30–3600, Default 300) |
| `WifiOnlyDownloads` | `bool` | Downloads nur im WLAN (Default `true`) |
| `WatchedEndWindowSeconds` | `int` | „Angesehen"-Fenster (30–3600, Default 300) |
| `DownloadQuotaGb` | `int` | Speicherplatzkontingent GB (1–500, Default `DefaultDownloadQuotaGb` = 5) |
| `AutoDeleteWatchedEpisodes` | `bool` | Gesehene Episoden automatisch löschen (Default `false`) |
| `Normalize()` | Methode | Klammert numerische Werte auf die Schranken |

Alle für Schritt 11 benötigten Einstellungen sind bereits vorhanden und
auf der Einstellungsseite verdrahtet.

## `AppStorageLayout`

Datei: `VideoPlayer.Storage/AppStorageLayout.cs`.

| Eigenschaft | Zweck |
|-------------|-------|
| `NoBackupDirectory` | Nicht backup-pflichtige Wurzel (iOS: `SetSkipBackupAttribute` via `AppStorageLayoutProvider`) |
| `VideosDirectory` | `NoBackupDirectory/videos` — Ziel der Downloads |
| `CacheDatabasePath` | `videoplayer-cache.db3` im NoBackup-Verzeichnis |
| `EnsureDirectoriesExist()` | Legt alle Verzeichnisse an |

## `ContinueWatchingEntry` (API-Modell)

Datei: `VideoPlayer.ApiClient/Models/ContinueWatching.cs`.

| Eigenschaft | Typ | Zweck |
|-------------|-----|-------|
| `MediaType` | `string` | Server-Konvention (`movie`, `episode`, `show`, `season`, `collection`) |
| `Entry` | `MediaEntry?` | Eingebetteter Medieneintrag (polymorph; `TvShowEpisode` trägt `Season.Show`) |
| `PositionSeconds` | `long` | Gemerkte Position |
| `DurationSeconds` | `long?` | Gesamtdauer |
| `Title` | `string` | Anzeigetitel |
| `PosterPictureId` | `long?` | Poster-Id |

Grundlage der Auto-Download-Ermittlung: Episoden-Einträge tragen über
`Entry` (`TvShowEpisode.Season.Show.Id`) den Serienbezug; die nächsten
Episoden kommen aus `GetTvShowAsync` (`TvShow.Seasons[].Episodes[]`,
sortiert nach `Number`).

## `PlaybackSource`/`PlaybackSourceKind`

Datei: `VideoPlayer.Mvu/PlaybackSource.cs`,
`VideoPlayer.Mvu/PlaybackSourceKind.cs` — Wiedergabequelle
(`ServerStream`/`LocalFile` + `Location` + `ResumePosition`).
`PlaybackSourceService` liefert `LocalFile`, sobald ein `completed`-
Manifesteintrag mit vorhandener Datei existiert — Wiedergabe
heruntergeladener Videos ist fertig verdrahtet.
