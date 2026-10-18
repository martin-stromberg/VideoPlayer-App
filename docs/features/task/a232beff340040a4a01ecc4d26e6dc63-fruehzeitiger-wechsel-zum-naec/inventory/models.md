# Datenmodell und Meldungen

## `PlayerState`
Datei: `VideoPlayer.Mvu/PlayerState.cs` (130 Zeilen) — `sealed record`, Ausgangszustand `PlayerState.Empty` (Z. 13).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` | `string?` | Medientyp des abzuspielenden Mediums (Z. 16). |
| `MediaId` | `long` | Id des Mediums (Z. 19). |
| `Title` | `string?` | Anzeigetitel (Z. 22). |
| `Source` | `PlaybackSource?` | Aufgelöste Quelle; `null` während Auflösung/Fehlschlag (Z. 25). |
| `IsResolving` | `bool` | Quellauflösung läuft (Z. 28). |
| `Attempted` | `bool` | Seite geöffnet, Wiedergabeversuch gestartet (Z. 31). |
| `Position` | `TimeSpan` | Letzte gemeldete Wiedergabeposition (Z. 34). |
| `Duration` | `TimeSpan` | Letzte gemeldete Gesamtdauer (Z. 37). |
| `IsPlaying` | `bool` | Wiedergabe läuft (Z. 40). |
| `IsBuffering` | `bool` | Player puffert — **wird vom Fenstertest nicht ausgewertet** (Z. 43). |
| `IsWatched` | `bool` | „Angesehen"-Markierung (Endfenster erreicht bzw. Ende-Ereignis, Z. 50). |
| `IsPictureInPicture` | `bool` | PiP-/Mini-Player-Modus (Z. 53). |
| `UpcomingEpisodes` | `IReadOnlyList<PlayerEpisodeRef>` | Fortsetzungsliste im Serienkontext (Z. 59). |
| `HasNextEpisode` | `bool` (abgeleitet) | `UpcomingEpisodes.Count > 0` (Z. 62). |
| `PlaylistId` / `PlaylistName` / `PlaylistEntryId` / `PlaylistPosition` / `PlaylistTotalCount` | `long?` / `string?` / `long` / `int` / `int` | Playlist-Kontext (Z. 68–80). |
| `PlaylistNextExhausted` | `bool` | Server meldete 204 — unterdrückt Vorwärts-Versuche (Z. 87). |
| `IsPlaylistPlayback` | `bool` (abgeleitet) | `PlaylistId > 0 && PlaylistEntryId > 0` (Z. 90). |
| `CanNavigateNext` / `CanNavigatePrevious` | `bool` (abgeleitet) | Manuelle Playlist-Navigation (Z. 93–97). |
| `PlaylistLabel` | `string` (abgeleitet) | Kontextzeile (Z. 103–106). |
| `ProgressReportPlaylistId` | `long?` (abgeleitet) | Playlist-Bezug der Fortschrittsmeldung (Z. 114). |
| `Error` / `HasError` | `string?` / `bool` | Fehlertext (Z. 117–120). |
| `LastReportedPositionSeconds` | `double` (Default −1) | Throttling der Server-Fortschrittsmeldung (Z. 126) — **einzige taktbezogene Merkgröße; es existiert kein Feld für Fenster-Streak, vorherige Dauer oder Positionsmonotonie.** |
| `IsActive` | `bool` (abgeleitet) | `MediaId > 0 && MediaType is not null` (Z. 129). |

## `AppSettings`
Datei: `VideoPlayer.Storage/AppSettings.cs` (68 Zeilen) — `sealed record`, `Normalize()` klammert numerische Werte (Z. 61–67).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `AutoResumeEnabled` | `bool` (Default `true`) | Auto-Fortsetzung an/aus (Z. 21). |
| `AutoResumeRemainingSeconds` | `int` (Default 300, Clamp 30–3600) | Restlaufzeit-Fenster der Auto-Fortsetzung (Z. 27). |
| `WatchedEndWindowSeconds` | `int` (Default 300, Clamp 30–3600) | „Angesehen"-Endfenster (Z. 36). |
| `AutoDeleteWatchedEpisodes` | `bool` (Default `false`) | Gesehene Episoden-Downloads automatisch löschen (Z. 42). |
| `AutoDownloadEnabled`, `PreloadEpisodeCount`, `WifiOnlyDownloads`, `DownloadQuotaGb`, `HomeHeroEnabled`, `LoggingEnabled` | – | Übrige Einstellungen (Z. 15–57), für den Vorfall nicht relevant. |

## Meldungen (`AppMessage`)
Datei: `VideoPlayer.Mvu/AppMessage.cs`.

| Meldung | Zeile | Trägt |
|---------|-------|-------|
| `PlaybackProgressMessage` | 1003–1007 | `Position`, `Duration`, `IsPlaying`, `IsBuffering` — **kein** Medien-Bezug, kein Stall-/Reason-Flag. |
| `PlaybackEndedMessage` | 1010 | parameterlos. |
| `PlaybackFailedMessage` | 1013 | parameterlos. |
| `PlayerClosedMessage` | 1030 | parameterlos. |
| `DownloadDeletedMessage` | 1118 | `MediaType`, `MediaId`. |

## Effekte (`AppEffect`)
Datei: `VideoPlayer.Mvu/AppEffect.cs`.

| Effekt | Zeile | Zweck |
|--------|-------|-------|
| `ReportPlaybackProgressEffect` | 652–657 | `MediaType`, `MediaId`, `Position`, `Duration`, `PlaylistId` → `ProgressReportService.ReportProgressAsync` (`ShellAppEffectHandler` Z. 485–505; Offline → `pending_progress`-Warteschlange). |
| `DeleteWatchedDownloadEffect` | 771 | `MediaType`, `MediaId` → `DownloadService.DeleteIfCompletedAsync` (nur `Completed`-Downloads). |
| `AdvancePlaylistEntryEffect` | 406–408 | `PlaylistId`, `CurrentEntryId` → `POST api/playlists/{id}/play/advance` → `PlaylistNavigationCompletedMessage`. |
| `ResolvePlaybackSourceEffect` | – | Quellauflösung → `PlaybackSourceResolvedMessage`. |
| `NavigateToPlayerEffect` | – | Navigation zur Wiedergabeseite. |

## Konstanten (`AppUpdate`)
Datei: `VideoPlayer.Mvu/AppUpdate.cs`.

| Konstante | Zeile | Wert / Zweck |
|-----------|-------|--------------|
| `PlaybackEndedToleranceSeconds` | 88 | `300` — Toleranzfenster der Ende-Plausibilitätsprüfung (`IsPlaybackEndPlausible`); bewusst Konstante statt Einstellung. |
| `PlaybackUnavailableError` | 75 | Fehlertext des Wiedergabe-Rückfalls. |
