# Datenmodell und Persistenz

## `AppSettings`
Datei: `VideoPlayer.Storage/AppSettings.cs` (`sealed record`, Z. 9–80)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `DefaultDownloadQuotaGb` | `const int` = 5 | Standard des Speicherplatzkontingents (Z. 12) |
| `AutoDownloadEnabled` | `bool` | Automatische Downloads an/aus (Z. 15) |
| `PreloadEpisodeCount` | `int` = 3 | Anzahl vorzuladender Episoden, Clamp 1–10 (Z. 18, 75) |
| `AutoResumeEnabled` | `bool` = `true` | Auto-Fortsetzung an/aus — **bleibt**, steuert den Wechsel (Z. 21) |
| `AutoResumeRemainingSeconds` | `int` = 300 | Zeitfenster der Auto-Fortsetzung (Restlaufzeit, Clamp 30–3600) — **entfällt** (Z. 23–27, Clamp Z. 76) |
| `WifiOnlyDownloads` | `bool` = `true` | Downloads nur im WLAN (Z. 30) |
| `WatchedEndWindowSeconds` | `int` = 300 | „Als angesehen"-Fenster in Sekunden, Clamp 30–3600 — **bleibt** (Z. 32–36, Clamp Z. 77) |
| `EndWindow` | `TimeSpan` (abgeleitet) | `max(WatchedEndWindowSeconds, AutoResumeRemainingSeconds)` als `TimeSpan` — gemeinsames Streak-/Diagnosefenster; Konsumenten: `AppUpdate.EvaluateEndWindow` Z. 3883, `PlayerPage.ReportPosition` Z. 190 (Z. 38–48) |
| `DownloadQuotaGb` | `int` = 5 | Speicherplatzkontingent in GB, Clamp 1–500 (Z. 51, 78) |
| `AutoDeleteWatchedEpisodes` | `bool` | Gesehene Episoden automatisch löschen (Z. 54) |
| `HomeHeroEnabled` | `bool` = `true` | Hero-Kopfbereich der Startseite (Z. 61) |
| `LoggingEnabled` | `bool` | Protokollierung an/aus (Z. 69) |
| `Normalize()` | `AppSettings` | Klammert numerische Werte auf Schranken; Zeile 76 (`AutoResumeRemainingSeconds`-Clamp) entfällt mit der Property (Z. 73–79) |

Persistenz: `AppSettingsService` (`VideoPlayer.Storage/AppSettingsService.cs`) schreibt/liest jede Einstellung über `ISettingsStore` mit Profil-Präfix (`AppProfile.PrefixKey`). Schlüssel-Konstanten Z. 11–38, darunter `AutoResumeRemainingSecondsKey = "AutoResumeRemainingSeconds"` (Z. 19–20) mit `Load()`-Zeile Z. 62 und `Save()`-Zeile Z. 82. Der persistierte Schlüssel bleibt nach der Entfernung verwaist (per Vorgabe keine Migration).

## `PlayerState`
Datei: `VideoPlayer.Mvu/PlayerState.cs` (`sealed record`, Z. 9–138)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Empty` | `static PlayerState` | Ausgangszustand (Z. 13) |
| `MediaType` / `MediaId` / `Title` | `string?` / `long` / `string?` | Identität des abzuspielenden Mediums (Z. 16–22) |
| `Source` | `PlaybackSource?` | Aufgelöste Quelle; `null` während Auflösung/Fehlschlag (Z. 25) |
| `IsResolving` | `bool` | Quellauflösung läuft — sperrt jede Fensterwirkung (Z. 28) |
| `Attempted` | `bool` | Wiedergabeversuch gestartet (Z. 31) |
| `Position` / `Duration` | `TimeSpan` | Letzter gemeldeter Wiedergabestand (Z. 34–37) |
| `IsPlaying` / `IsBuffering` | `bool` | Lauf-/Pufferzustand (Z. 40–43) |
| `IsWatched` | `bool` | „Angesehen"-Markierung (Z. 46–50) |
| `IsPictureInPicture` | `bool` | PiP-/Mini-Player-Modus (Z. 53) |
| `UpcomingEpisodes` | `IReadOnlyList<PlayerEpisodeRef>` | Fortsetzungsliste (Serienkontext) — Basis von `AdvanceToNextEpisode` (Z. 59) |
| `HasNextEpisode` | `bool` (abgeleitet) | `UpcomingEpisodes.Count > 0` (Z. 62) |
| `PlaylistId` / `PlaylistName` / `PlaylistEntryId` / `PlaylistPosition` / `PlaylistTotalCount` | `long?` / `string?` / `long` / `int` / `int` | Playlist-Kontext (Z. 68–80) |
| `PlaylistNextExhausted` | `bool` | Server meldete keinen Folgeeintrag — sperrt weitere Advance-Versuche (Z. 87) |
| `IsPlaylistPlayback` | `bool` (abgeleitet) | `PlaylistId > 0 && PlaylistEntryId > 0` (Z. 90) |
| `CanNavigateNext` / `CanNavigatePrevious` | `bool` (abgeleitet) | Manuelle Playlist-Navigation (Z. 93–97) |
| `PlaylistLabel` | `string` (abgeleitet) | Kontextzeile der Playlist-Wiedergabe (Z. 103–106) |
| `ProgressReportPlaylistId` | `long?` (abgeleitet) | Playlist-Bezug für `ReportPlaybackProgressEffect` (Z. 114) |
| `Error` / `HasError` | `string?` / `bool` | Fehlertext bzw. -zustand (Z. 117–120) |
| `LastReportedPositionSeconds` | `double` = -1 | Throttling der Fortschrittsmeldungen (Z. 126) |
| `EndWindowStreak` | `int` | Aufeinanderfolgende plausible Fenstertakte — Bestätigungsgrundlage von „angesehen" + Auto-Fortsetzung; **bleibt** für die Watched-Markierung (Z. 128–134) |
| `IsActive` | `bool` (abgeleitet) | `MediaId > 0 && MediaType is not null` (Z. 137) |

## `PlaybackProgressMessage`
Datei: `VideoPlayer.Mvu/AppMessage.cs` (Z. 1009–1014)

| Parameter | Typ | Beschreibung / Zweck |
|-----------|-----|----------------------|
| `Position` | `TimeSpan` | Aktuelle Wiedergabeposition |
| `Duration` | `TimeSpan` | Gesamtdauer |
| `IsPlaying` | `bool` | Wiedergabe läuft |
| `IsBuffering` | `bool` | Player puffert |
| `MediaId` | `long` | Zuletzt am Element angewendete Medien-Id — Identitäts-Guard gegen nachlaufende Takte des Vorgängermediums; **bleibt** |

## `PlaybackEndedMessage`
Datei: `VideoPlayer.Mvu/AppMessage.cs` (Z. 1017)

Parameterlose Nachricht — wird von `PlayerPage.OnMediaEnded` (`MediaEnded`-Event des `MediaElement`) dispatcht; nach der Änderung **einziger** Auslöser der Auto-Fortsetzung.

## `AppModel.Settings`
Datei: `VideoPlayer.Mvu/AppModel.cs` (Z. 57–61)

`AppSettings`-Instanz im MVU-Model — wird beim Start über `LoadSettingsEffect` → `SettingsLoadedMessage` geladen und über `SaveSettingsRequestedMessage` → `PersistSettingsEffect` gespeichert.
