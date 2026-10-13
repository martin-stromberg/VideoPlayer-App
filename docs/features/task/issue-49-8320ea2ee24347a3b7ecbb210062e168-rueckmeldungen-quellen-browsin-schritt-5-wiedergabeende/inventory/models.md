# Datenmodell — Bestandsaufnahme

## `PlayerState`

Datei: `VideoPlayer.Mvu/PlayerState.cs` (`public sealed record`, Ausgangszustand `Empty`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` / `MediaId` | `string?` / `long` | Identität des Mediums (Grundlage `IsActive`) |
| `Position` | `TimeSpan` | Aktuelle Wiedergabeposition — letzter gemeldeter Takt |
| `Duration` | `TimeSpan` | Gesamtdauer, soweit vom Player bekannt |
| `IsPlaying` / `IsBuffering` | `bool` | Lauf-/Pufferzustand |
| `IsWatched` | `bool` | „angesehen"-Markierung |
| `LastReportedPositionSeconds` | `double` | Throttling der Fortschrittsmeldungen |
| `UpcomingEpisodes` / `HasNextEpisode` | `IReadOnlyList<PlayerEpisodeRef>` / `bool` | Serien-Fortsetzungsliste |
| `PlaylistId` / `PlaylistEntryId` / `PlaylistPosition` / `PlaylistTotalCount` / `PlaylistNextExhausted` | `long?` / `long` / `int` / `int` / `bool` | Playlist-Kontext |
| `IsPlaylistPlayback` | `bool` | `PlaylistId > 0 && PlaylistEntryId > 0` |
| `IsActive` | `bool` | `MediaId > 0 && MediaType is not null` — einziger Guard von `UpdatePlaybackEnded` |
| `Source` / `IsResolving` / `Attempted` / `Error` / `IsPictureInPicture` | … | Quellenauflösung und Anzeigezustand |

## `PlaybackEndedMessage`

Datei: `VideoPlayer.Mvu/AppMessage.cs:1010`

Parameterlose Nachricht (`public sealed record PlaybackEndedMessage : AppMessage`). Einziger Dispatcher: `PlayerPage.OnMediaEnded`. Einziger Handler: `AppUpdate.UpdatePlaybackEnded`.

## `PlaybackProgressMessage`

Datei: `VideoPlayer.Mvu/AppMessage.cs:1003` — trägt `Position`, `Duration`, `IsPlaying`, `IsBuffering` (Vorbild für positionsbasierte Auswertung).

## `AppSettings` (Orientierungswerte, `VideoPlayer.Storage/AppSettings.cs`)

| Eigenschaft | Typ | Standard | Schranke (`Normalize`) |
|-------------|-----|----------|------------------------|
| `WatchedEndWindowSeconds` | `int` | 300 | 30–3600 — Angesehen-Endfenster, Orientierung des Toleranzwerts |
| `AutoResumeRemainingSeconds` | `int` | 300 | 30–3600 — Auto-Vorlauffenster im Fortschrittstakt |
| `AutoResumeEnabled` | `bool` | true | — |
| `AutoDeleteWatchedEpisodes` | `bool` | false | — |

Keine neue Einstellung geplant (Projektklärung: Schwelle als Konstante).
