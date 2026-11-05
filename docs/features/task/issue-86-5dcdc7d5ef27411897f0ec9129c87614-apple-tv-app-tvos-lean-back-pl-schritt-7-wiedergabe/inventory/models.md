# Modelle und Zustand (Wiedergabe)

## `PlayerState`
Datei: `VideoPlayer.Mvu/PlayerState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Empty` | `PlayerState` (statisch) | Ausgangszustand ohne Wiedergabe |
| `MediaType` | `string?` | Medientyp (normalisiert) |
| `MediaId` | `long` | Id des Mediums |
| `Title` | `string?` | Anzeigetitel |
| `Source` | `PlaybackSource?` | Aufgelöste Quelle; `null` während Auflösung/Fehlschlag |
| `IsResolving` | `bool` | Quellauflösung läuft (sperrt Fensterwirkung) |
| `Attempted` | `bool` | Wiedergabeversuch gestartet |
| `Position` / `Duration` | `TimeSpan` | Letzter Takt / Gesamtdauer |
| `IsPlaying` / `IsBuffering` | `bool` | Laufzustand / Pufferung |
| `IsWatched` | `bool` | „angesehen"-Markierung (bestätigtes Endfenster) |
| `IsPictureInPicture` | `bool` | PiP-/Mini-Player (tvOS: nicht verdrahtet) |
| `UpcomingEpisodes` | `IReadOnlyList<PlayerEpisodeRef>` | Serien-Fortsetzungsliste (Auto-Fortsetzung + Rückfall) |
| `HasNextEpisode` | `bool` (abgeleitet) | Folge-Episode bereit |
| `PlaylistId`/`PlaylistName`/`PlaylistEntryId` | `long?`/`string?`/`long` | Playlist-Kontext |
| `PlaylistPosition`/`PlaylistTotalCount` | `int` | Positionsanzeige (1-basiert / Gesamtzahl) |
| `PlaylistNextExhausted` | `bool` | Server meldete 204 — Vorwärts-Versuche gesperrt |
| `IsPlaylistPlayback` / `CanNavigateNext` / `CanNavigatePrevious` | `bool` (abgeleitet) | Playlist-Kontext-Guards |
| `PlaylistLabel` | `string` (abgeleitet) | „„Name“ · Eintrag n/m" |
| `ProgressReportPlaylistId` | `long?` (abgeleitet) | Playlist-Bezug der Fortschrittsmeldung |
| `Error` / `HasError` | `string?` / `bool` | Fehlertext / Fehlerzustand |
| `LastReportedPositionSeconds` | `double` | Throttling (Init `-1`) |
| `EndWindowStreak` | `int` | Bestätigungs-Streak des Endfensters |
| `DownloadExpiryProtected` | `bool` | Einmal-Semantik des Wiedergabe-Schutzes |
| `IsActive` | `bool` (abgeleitet) | `MediaId > 0 && MediaType != null` |

## `PlaybackSource`
Datei: `VideoPlayer.Mvu/PlaybackSource.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Kind` | `PlaybackSourceKind` | `ServerStream` oder `LocalFile` |
| `Location` | `string` | Stream-URL (mit `access_token`) bzw. Dateipfad |
| `ResumePosition` | `TimeSpan?` | Fortsetzungsposition; `null` = ab Anfang |

## `PlayerEpisodeRef`
Datei: `VideoPlayer.Mvu/PlayerEpisodeRef.cs`

- `MediaId` (`long`), `Title` (`string?`) — Fortsetzungslisten-Eintrag.

## `AppModel`-Bezüge
Datei: `VideoPlayer.Mvu/AppModel.cs`

| Bezug | Zweck |
|-------|-------|
| `model.Player` | Der Player-Zustand (s. o.) |
| `model.IsOffline` | `ConnectionState == Offline` — steuert `allowServerStream` der Quellauflösung |
| `model.Settings.AutoResumeEnabled` | Auto-Fortsetzungs-Schalter |
| `model.Settings.WatchedEndWindowSeconds` | „angesehen"-Endfenster (Standard 300 s) |
| `model.Settings.AutoDeletePlayedDownloads` / `DeleteAfterPlaybackDelayMinutes` | Download-Aufräumen gesehener Medien |
| `model.MediaDetails.Details` | Serienkontext der `NextEpisodeQueue`-Ermittlung |
