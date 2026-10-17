# Bestandsaufnahme: Datenmodell und Ablage

## `AppSettings`
Datei: `VideoPlayer.Storage/AppSettings.cs`

Unveränderlicher Record (`sealed record`), alle Werte `init`-only, `Normalize()` klammert numerische Werte.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `DefaultDownloadQuotaGb` | `const int` = 5 | Standard des Download-Kontingents |
| `AutoDownloadEnabled` | `bool` | Automatische Downloads (Standard `false`) |
| `PreloadEpisodeCount` | `int` = 3 | Vorzuladende Episoden (1–10) |
| `AutoResumeEnabled` | `bool` = true | Auto-Fortsetzung |
| `AutoResumeRemainingSeconds` | `int` = 300 | Auto-Fortsetzungs-Fenster (30–3600) |
| `WifiOnlyDownloads` | `bool` = true | Downloads nur im WLAN |
| `WatchedEndWindowSeconds` | `int` = 300 | „Angesehen"-Fenster (30–3600) |
| `DownloadQuotaGb` | `int` = 5 | Kontingent (1–500) |
| `AutoDeleteWatchedEpisodes` | `bool` | Gesehene Episoden löschen (Standard `false`) |
| `HomeHeroEnabled` | `bool` = true | Hero-Kopfbereich (positiv formuliert) |

Neu: `LoggingEnabled` (`bool`, Standard `false`) — kein `Normalize()`-Eingriff nötig (boolesch).

## `AppSettingsService`
Datei: `VideoPlayer.Storage/AppSettingsService.cs`

| Methode/Element | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `{Name}Key`-Konstanten | `public const string` | Ablageschlüssel je Einstellung (9 vorhanden) |
| `AppSettingsService(ISettingsStore, AppProfile)` | public ctor | DI-fähig |
| `Load()` | public | Liest alle Schlüssel mit Defaults, `Normalize()` |
| `Save(AppSettings)` | public | Normalisiert, schreibt alle Schlüssel |

Konvention: neuer Schlüssel `LoggingEnabledKey = "LoggingEnabled"`, Eintrag in `Load()` und `Save()`.

## `ISettingsStore` / `PreferencesSettingsStore`
Dateien: `VideoPlayer.Storage/ISettingsStore.cs`, `VideoPlayer.Maui/Services/PreferencesSettingsStore.cs`

Interface: `Contains`, `GetString`, `GetInt`, `GetBool`, `Set(string/int/bool)`, `Remove`. Adapter kapselt `IPreferences` — synchron nutzbar (wichtig für das Einlesen des Startzustands in `MauiProgram`).

## `AppStorageLayout`
Datei: `VideoPlayer.Storage/AppStorageLayout.cs`

| Eigenschaft/Methode | Zweck |
|---------------------|-------|
| `BackupDirectory` | Backup-pflichtig (Windows: `%APPDATA%\VideoPlayer\{profil}`; iOS/Android: `AppDataDirectory/data`) |
| `DataDatabasePath` | `videoplayer.db3` |
| `NoBackupDirectory` | Nicht backup-pflichtig (Windows unpackaged: `%LOCALAPPDATA%\User Name\de.martinstromberg.videos\Data\nobackup\{profil}` — empirisch verifiziert) |
| `CacheDatabasePath` | Cache-DB |
| `VideosDirectory` / `PreviewsDirectory` | Offline-Videos / Vorschaubilder |
| `TempDirectory` | Temp-Cache (`CacheDirectory`) |
| `EnsureDirectoriesExist()` | Legt alle Verzeichnisse an |

Neu: `LogsDirectory` = `Path.Combine(NoBackupDirectory, "logs")`; Anlage in `EnsureDirectoriesExist()` plus defensive Anlage beim Schreiben.

## `AppProfile`
Datei: `VideoPlayer.Storage/AppProfile.cs`

`EnvironmentVariable = "VIDEOPLAYER_PROFILE"`; `PrefixKey`/`DirectoryName` isolieren Schlüssel und Dateiablagen je Profil — dadurch ist das Protokoll automatisch profil-isoliert und E2E-seitig unterscheidbar.

## `AppModel` (Auszug, betroffene Bereiche)
Datei: `VideoPlayer.Mvu/AppModel.cs`

- `Settings` (`AppSettings`) — wird über `LoadSettingsEffect` → `SettingsLoadedMessage` beim Start befüllt.
- `StorageNotice` (`string?`) — Vorbild-Muster für Hinweistexte: Modell-Feld → Anzeige (`HomePage` StorageNoticeBanner). Für den Protokollversand-Fehler kommt ein analoges Feld in Betracht.
