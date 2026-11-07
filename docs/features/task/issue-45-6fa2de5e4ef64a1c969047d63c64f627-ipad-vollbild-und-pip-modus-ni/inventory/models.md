# Datenmodell — Bestandsaufnahme

## `PlayerState`

Datei: `VideoPlayer.Mvu/PlayerState.cs` (`public sealed record`,
Ausgangszustand `PlayerState.Empty`). Für die Anforderung relevant sind
v. a. die Eigenschaften, die `PlayerPage.UpdateUi` zur
Overlay-Sichtbarkeit nutzt.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` | `string?` | Medientyp des abzuspielenden Mediums (normalisiert) |
| `MediaId` | `long` | Id des abzuspielenden Mediums |
| `Title` | `string?` | Anzeigetitel → `TitleLabel` im `OverlayChrome`-Band |
| `Source` | `PlaybackSource?` | Aufgelöste Quelle; `null` während Auflösung/Fehlschlag |
| `IsResolving` | `bool` | Quelle wird aufgelöst → `LoadingIndicator` |
| `Attempted` | `bool` | Seite geöffnet, Wiedergabeversuch gestartet |
| `Position` / `Duration` | `TimeSpan` | Wiedergabeposition/-dauer (Takt) |
| `IsPlaying` | `bool` | Wiedergabe läuft |
| `IsBuffering` | `bool` | Zwischenpufferung → `LoadingIndicator` |
| `IsWatched` | `bool` | „angesehen"-Markierung → `WatchedBadge` |
| `IsPictureInPicture` | `bool` | Mini-Player aktiv → blendet `OverlayChrome`, `PlaylistNavigationBar`, `ErrorPanel` aus |
| `UpcomingEpisodes` | `IReadOnlyList<PlayerEpisodeRef>` | Folge-Episoden (Auto-Fortsetzung/Fehler-Rückfall) |
| `HasNextEpisode` | `bool` (berechnet) | `UpcomingEpisodes.Count > 0` |
| `PlaylistId` | `long?` | Playlist-Kontext; `null` = Serien-/Einzelwiedergabe |
| `PlaylistName` | `string?` | Name der Playlist |
| `PlaylistEntryId` | `long` | Id des aktuell gespielten Eintrags |
| `PlaylistPosition` / `PlaylistTotalCount` | `int` | Position/Gesamtzahl in der Playlist |
| `PlaylistNextExhausted` | `bool` | Server meldete keinen Folgeeintrag (204) |
| `IsPlaylistPlayback` | `bool` (berechnet) | `PlaylistId is > 0 && PlaylistEntryId > 0` → Sichtbarkeit `PlaylistNavigationBar` + `PlaylistContextLabel` |
| `CanNavigateNext` / `CanNavigatePrevious` | `bool` (berechnet) | `IsEnabled` der Buttons (`Next`: Playlistkontext, nicht exhausted, `Position < Total`; `Previous`: `Position > 1`) |
| `PlaylistLabel` | `string` (berechnet) | Kontextzeile „„Name" · Eintrag n/m" → `PlaylistContextLabel` |
| `Error` / `HasError` | `string?` / `bool` (berechnet) | Fehlertext → `ErrorPanel` |
| `LastReportedPositionSeconds` | `double` | Throttling der Fortschrittsmeldungen (Default `-1`) |
| `IsActive` | `bool` (berechnet) | `MediaId > 0 && MediaType is not null` |

Keine der Overlay-relevanten Eigenschaften fehlt — die
Sichtbarkeitslogik der Anforderung ist vollständig vorhanden; eine
Änderung am Modell ist nicht erwartet.

## `PlaybackSource`

Datei: `VideoPlayer.Mvu/PlaybackSource.cs` (`public sealed record`).
Kontext — wird von `PlayerPage.UpdateUi` beim Setzen der
`MediaElement`-Quelle verwendet; nicht direkt Teil der
Overlay-Problematik.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Kind` | `PlaybackSourceKind` (required) | `ServerStream` oder `LocalFile` |
| `Location` | `string` (required) | Stream-URL bzw. lokaler Dateipfad |
| `ResumePosition` | `TimeSpan?` | Fortsetzungsposition; `null` = Start am Anfang |

Zugehöriger Enum `PlaybackSourceKind`
(`VideoPlayer.Mvu/PlaybackSourceKind.cs`): `ServerStream`
(Stream-Endpunkt `api/items/{typ}/{id}/stream`), `LocalFile` (Datei aus
dem Download-Manifest).
