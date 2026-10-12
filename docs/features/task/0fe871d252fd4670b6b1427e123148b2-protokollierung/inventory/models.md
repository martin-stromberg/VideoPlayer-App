# Datenmodell und MVU-Nachrichten

Betroffene Modellklassen, Records und MVU-Nachrichten/Effekte des Protokoll- und Wiedergabe-/Sync-Kontexts.

## `AppSettings`
Datei: `VideoPlayer.Storage/AppSettings.cs`

Unveränderliches Record aller benutzerbezogenen Einstellungen; Persistenz über `AppSettingsService`/`ISettingsStore`; `Normalize()` klammert numerische Werte.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `LoggingEnabled` | `bool` | Protokollierung an/aus — steuert `AppLogService`/`FileLogger.IsEnabled`; Standard `false`; Schalter `LoggingSwitch` auf `SettingsPage` |
| `AutoDownloadEnabled` | `bool` | Automatischer Vorlauf (`AutoDownloadService.RunAsync`-Gate) |
| `WifiOnlyDownloads` | `bool` | Downloads nur im WLAN (Standard `true`) |
| `PreloadEpisodeCount` | `int` | Folge-Videos pro Weiterschauen-Eintrag (1–10, Standard 3) |
| `AutoResumeEnabled` / `AutoResumeRemainingSeconds` | `bool` / `int` | Auto-Fortsetzung nahe Videoende |
| `WatchedEndWindowSeconds` | `int` | „Als angesehen"-Fenster (30–3600, Standard 300) |
| `DownloadQuotaGb` | `int` | Speicherkontingent (1–500, Standard 5) |
| `AutoDeleteWatchedEpisodes` | `bool` | Gesehene Episoden automatisch löschen |
| `HomeHeroEnabled` | `bool` | Hero-Kopfbereich der Startseite (Standard `true`) |

## `AppModel` (Protokoll-Felder)
Datei: `VideoPlayer.Mvu/AppModel.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Settings` | `AppSettings` | Aktuelle Einstellungen (u. a. `LoggingEnabled`) |
| `LogSendError` | `string?` | Anzeigbarer Fehlertext des Protokollversands (Z. 242); gesetzt/geräumt durch `SendLogProtocolCompletedMessage` |
| `Player` | `PlayerState` | Wiedergabezustand (`Source`, `ResumePosition`, `HasError`, …) |

DevApi-Spiegelung für E2E: `DevApiModelSnapshot.SettingsLoggingEnabled`/`LogSendError` (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs` Z. 87/90, gemappt Z. 270–271); E2E-Modelle `settingsLoggingEnabled`/`logSendError` (`VideoPlayer.E2E.Tests/Support/DevApiModels.cs` Z. 87/90).

## `PlaybackSource`
Datei: `VideoPlayer.Mvu/PlaybackSource.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Kind` | `PlaybackSourceKind` | `ServerStream` oder `LocalFile` — relevantes, geheimnisfreies Protokollmerkmal |
| `Location` | `string` | URL bzw. Dateipfad — **enthält bei ServerStream den `access_token`-Query** (Geheimnis!) |
| `ResumePosition` | `TimeSpan?` | Fortsetzungsposition |

## `SyncResult`
Datei: `VideoPlayer.Mvu/ServerSyncService.cs` (Z. 16–21)

Record-Ergebnis von `ServerSyncService.SynchronizeAsync`: `SentProgressReports` (`int`), `RemainingProgressReports` (`int`), `MetadataSynced`/`ContinueWatchingSynced`/`FavoritesSynced` (`bool`) — die von der Anforderung genannten Sync-Zähler.

## `AuthAttempt`
Datei: `VideoPlayer.Mvu/AuthAttempt.cs`

Record `AuthAttempt(AuthAttemptStatus Status, string? Username = null)` mit Hilfs-Property `Succeeded` — Ergebnis von `AuthSessionService.TryStoredLoginAsync`/`LoginAsync`.

## `DownloadEnqueueResult`
Datei: `VideoPlayer.Mvu/DownloadService.cs` (Z. 29–39)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Item` | `DownloadListItem?` | Manifest-Snapshot; `null` bei ungültiger Anforderung |
| `Notice` | `string?` | Anzeigbarer Hinweis (`NoticeEnqueued`, `NoticeQuotaExceeded`, …) |
| `IsError` | `bool` | Hinweis beschreibt Ablehnung/Fehler |

## Protokoll-Nachrichten und -Effekte

| Typ | Datei:Zeile | Inhalt / Zweck |
|-----|-------------|----------------|
| `SendLogProtocolRequestedMessage` | `VideoPlayer.Mvu/AppMessage.cs:1137` | Anwender klickt „Protokoll senden" (`SettingsPage.OnSendLogClicked`) |
| `SendLogProtocolCompletedMessage(string? Error)` | `AppMessage.cs:1146` | Versand-Abschluss; `Error` = `LogProtocolEmptyError`/`LogSendFailedError`/`null` |
| `SendLogProtocolEffect` | `VideoPlayer.Mvu/AppEffect.cs:759` | Versand-Effekt → `ShellAppEffectHandler.SendLogProtocolAsync` |
| `SaveSettingsRequestedMessage` | `AppMessage.cs` | Trägt geänderte `AppSettings` → `PersistSettingsEffect` |
| `AppUpdate.LogProtocolEmptyError` | `AppUpdate.cs:78` | „Es ist noch kein Protokoll vorhanden." |
| `AppUpdate.LogSendFailedError` | `AppUpdate.cs:80` | Verständlicher Fehlertext (weder Mailentwurf noch Teilen möglich) |
| `UpdateSendLogProtocolRequested` | `AppUpdate.cs:344` | Guard `!model.Settings.LoggingEnabled` → No-Op; sonst Effekt + `LogSendError=null` |
| `UpdateSendLogProtocolCompleted` | `AppUpdate.cs:359` | Setzt `LogSendError = message.Error` |

Weitere für Breadcrumbs relevante Effekte (Auswahl, vollständige Liste in `AppEffect.cs`, 73 Records): `InitializeStorageEffect`, `CheckServerHealthEffect`, `AttemptAutoLoginEffect`/`AttemptLoginEffect`, `ConnectServerEventsEffect`/`DisconnectServerEventsEffect`, `RunFullSyncEffect`/`SyncContinueWatchingEffect`/`SyncFavoritesEffect`/`SyncMetadataEffect`, `ResolvePlaybackSourceEffect`, `ReportPlaybackProgressEffect`, `EnqueueDownloadEffect`/`CancelDownloadEffect`/`DeleteDownloadEffect`/`ResumeDownloadsEffect`/`RunAutoDownloadsEffect`, `LogoutEffect`, `TogglePictureInPictureEffect`, `SendLogProtocolEffect`.
