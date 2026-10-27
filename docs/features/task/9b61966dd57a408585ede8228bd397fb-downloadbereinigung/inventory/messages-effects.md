# MVU-Nachrichten und -Effekte (download-/wiedergaberelevant)

Basistypen: `AppMessage` (`VideoPlayer.Mvu/AppMessage.cs`), `AppEffect` (`VideoPlayer.Mvu/AppEffect.cs`) — beide abstrakte Records; Ausführung der Effekte über `IAppEffectHandler`/`ShellAppEffectHandler`.

## `AppEffect`-Records (Downloads)

Datei: `VideoPlayer.Mvu/AppEffect.cs`

| Record | Parameter / Eigenschaften | Ergebnis-Nachricht / Zweck |
|--------|---------------------------|-----------------------------|
| `LoadDownloadsEffect` | `Settings` | `DownloadsLoadedMessage`; lädt Manifest-Liste + Belegung (im Handler: `ReconcileAsync` zuerst) |
| `EnqueueDownloadEffect` | `MediaType`, `MediaId`, `Title`, `Settings`; `ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber`, `PictureId` | `DownloadEnqueueCompletedMessage`; Einreihen/Re-Enqueue — **kein Herkunftsparameter, kein Playlist-Kontext** |
| `CancelDownloadEffect` | `MediaType`, `MediaId` | Download abbrechen (`cancelled`) |
| `DeleteDownloadEffect` | `MediaType`, `MediaId` | `DownloadDeletedMessage` (via `ItemRemoved`); vollständiges Löschen |
| `DeleteAllDownloadsEffect` | `Settings` | Liste neu laden; alle Downloads + Manifest entfernen |
| `PurgeOrphanedDownloadsEffect` | `Settings` | Liste neu laden; Restdaten ohne Manifest-Eintrag löschen |
| `DeleteFailedDownloadsEffect` | `Settings` | Liste neu laden; `failed`/`cancelled`-Einträge entfernen |
| `ResumeDownloadsEffect` | `Settings` | Kein Ergebnis-Record; `ReconcileAsync` + `ResumeInterruptedAsync` im Handler — Auslöser: `EnterConnected`, Online-Rückkehr |
| `RunAutoDownloadsEffect` | `Settings`, `Force` | Kein Ergebnis-Record; `AutoDownloadService.RunAsync` — Auslöser: Sync-Abschluss, Cache-Änderung, Einschalten der Einstellung |
| `DeleteWatchedDownloadEffect` | `MediaType`, `MediaId` | `DownloadDeletedMessage` (via `ItemRemoved`); entfernt den Download nur bei `completed` (`DeleteIfCompletedAsync`) — Träger der Episoden-Sofortlöschung |

## `AppEffect`-Records (Wiedergabe, Nebenbezug)

| Record | Parameter | Zweck |
|--------|-----------|-------|
| `ResolvePlaybackSourceEffect` | `MediaType`, `MediaId`, `StartPositionSeconds` | `PlaybackSourceResolvedMessage`; Quellauflösung (lokal vor Stream) |
| `ReportPlaybackProgressEffect` | `MediaType`, `MediaId`, `Position`, `Duration`, `PlaylistId` | Fortschrittsmeldung online bzw. `pending_progress`-Outbox |
| `NavigateToPlayerEffect` | — | Navigation zur Wiedergabeseite |

## `AppMessage`-Records (Downloads)

Datei: `VideoPlayer.Mvu/AppMessage.cs` (Zeilen 1051–1159)

| Record | Parameter / Eigenschaften | Zweck |
|--------|---------------------------|-------|
| `DownloadsPageOpenedMessage` | — | Downloads-Seite geöffnet → `LoadDownloadsEffect` |
| `DownloadsLoadedMessage` | `Items`, `UsedBytes`, `QuotaGb`, `Error`, `OrphanedBytes` | Ladeergebnis der Download-Liste |
| `DownloadRequestedMessage` | `MediaType`, `MediaId`, `Title`; `ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber`, `PictureId` | Manuelle Download-Anforderung (Detailseite, Episodenliste) → `EnqueueDownloadEffect` — **kein Herkunftsbegriff** |
| `DownloadEnqueueCompletedMessage` | `Item`, `Notice`, `IsError`, `MediaType`, `MediaId` | Einreihe-Ergebnis (Hinweis auf der Detailseite) |
| `DownloadItemChangedMessage` | `Item` | Zustands-/Fortschrittswechsel (via `ItemChanged`-Event) |
| `DownloadDeletedMessage` | `MediaType`, `MediaId` | Eintrag entfernt (via `ItemRemoved`-Event — Löschen durch Anwender oder automatische Läufe) |
| `DownloadCancelRequestedMessage` | `Item` | Abbruch anfordern → `CancelDownloadEffect` |
| `DownloadRetryRequestedMessage` | `Item` | Erneut einreihen anfordern → `EnqueueDownloadEffect` (nur `PictureId`, kein Serienkontext) |
| `DownloadDeleteRequestedMessage` | `Item` | Löschen anfordern → `DeleteDownloadEffect` |
| `DownloadsDeleteAllRequestedMessage` | — | Alle Downloads löschen (UI-Rückfrage) |
| `DownloadsPurgeOrphansRequestedMessage` | — | Restdaten bereinigen (UI-Rückfrage) |
| `DownloadsDeleteFailedRequestedMessage` | — | Fehlerhafte/abgebrochene löschen (UI-Rückfrage) |

## `AppMessage`-Records (Wiedergabe, Nebenbezug)

| Record | Parameter | Zweck |
|--------|-----------|-------|
| `PlaybackSourceResolvedMessage` | `MediaType`, `MediaId`, `Source`, `Error` | Aufgelöste Quelle (inkl. `PlaybackSourceKind.LocalFile`) — derzeit ohne Ablauf-/Schutz-Wirkung |
| `PlaybackProgressMessage` | `MediaId`, `Position`, `Duration`, `IsPlaying`, `IsBuffering` | Positions-Takt der Wiedergabeseite → Endfenster-Auswertung |
| `PlaybackEndedMessage` | — | Wiedergabeende → `UpdatePlaybackEnded` |
| `PlayRequestedMessage` | `MediaType`, `MediaId`, `Title`, `StartPositionSeconds` | Wiedergabestart → `ResolvePlaybackSourceEffect` |
