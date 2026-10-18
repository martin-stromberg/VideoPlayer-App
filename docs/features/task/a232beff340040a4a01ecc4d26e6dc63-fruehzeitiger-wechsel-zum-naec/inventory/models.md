# Datenmodell und MVU-Records

## `PlayerState`
Datei: `VideoPlayer.Mvu/PlayerState.cs`

Zustand der Wiedergabeseite; Ausgangszustand `PlayerState.Empty`. Wird von `AppUpdate` gelesen/geschrieben und von `PlayerPage` (über `MvuHost.Model`) gerendert.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Empty` | `static PlayerState` | Leerer Ausgangszustand (keine Wiedergabe angefordert) |
| `MediaType` | `string?` | Medientyp des abzuspielenden Mediums (normalisiert) |
| `MediaId` | `long` | Id des abzuspielenden Mediums |
| `Title` | `string?` | Anzeigetitel |
| `Source` | `PlaybackSource?` | Aufgelöste Wiedergabequelle; `null` während Auflösung/Fehlschlag |
| `IsResolving` | `bool` | Quellauflösung läuft |
| `Attempted` | `bool` | Seite geöffnet, Wiedergabeversuch gestartet (durch `PlayerPageOpenedMessage`) |
| `Position` | `TimeSpan` | Letzte gemeldete Wiedergabeposition |
| `Duration` | `TimeSpan` | Gesamtdauer (soweit bekannt) |
| `IsPlaying` | `bool` | Wiedergabe läuft |
| `IsBuffering` | `bool` | Player puffert |
| `IsWatched` | `bool` | „Angesehen"-Schwelle erreicht |
| `IsPictureInPicture` | `bool` | PiP-/Mini-Player-Modus aktiv |
| `UpcomingEpisodes` | `IReadOnlyList<PlayerEpisodeRef>` | Fortsetzungsliste — Grundlage von Auto-Fortsetzung **und** Fehler-Rückfall (`AdvanceToNextEpisode`) |
| `HasNextEpisode` | `bool` (abgeleitet) | `UpcomingEpisodes.Count > 0` |
| `PlaylistId` | `long?` | Playlist-Kontext der Wiedergabe; `null` = Serien-/Einzelkontext |
| `PlaylistName` | `string?` | Name der Playlist |
| `PlaylistEntryId` | `long` | Aktuell gespielter Playlist-Eintrag |
| `PlaylistPosition` | `int` | 1-basierte Position in der Playlist-Reihenfolge |
| `PlaylistTotalCount` | `int` | Gesamtzahl der Playlist-Einträge |
| `PlaylistNextExhausted` | `bool` | Server meldete keinen Folgeeintrag (204) — unterdrückt weitere Vorwärts-Versuche |
| `IsPlaylistPlayback` | `bool` (abgeleitet) | `PlaylistId is > 0 && PlaylistEntryId > 0` — steuert, ob `UpdatePlaybackFailed`/`UpdatePlaybackSourceResolved` über `AdvancePlaylistEntry` oder `AdvanceToNextEpisode` zurückfallen |
| `CanNavigateNext` / `CanNavigatePrevious` | `bool` (abgeleitet) | Heuristik für die manuelle Playlist-Navigation |
| `PlaylistLabel` | `string` (abgeleitet) | Kontextzeile („„Name" · Eintrag n/m") |
| `ProgressReportPlaylistId` | `long?` (abgeleitet) | Playlist-Bezug für Fortschrittsmeldungen |
| `Error` | `string?` | Fehlertext der letzten Quell-/Wiedergabe-Störung |
| `HasError` | `bool` (abgeleitet) | `Error is not null` → Fehlerbanner in `PlayerPage` |
| `LastReportedPositionSeconds` | `double` | Throttling der Fortschrittsmeldungen (Start -1) |
| `IsActive` | `bool` (abgeleitet) | `MediaId > 0 && MediaType is not null` — Guard in fast allen Player-Update-Funktionen |

## `PlaybackSource`
Datei: `VideoPlayer.Mvu/PlaybackSource.cs`

Aufgelöste Wiedergabequelle; erzeugt von `PlaybackSourceService.ResolveAsync`, über `PlaybackSourceResolvedMessage` in den `PlayerState` übernommen und von `PlayerPage` an das `MediaElement` gesetzt.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Kind` | `PlaybackSourceKind` (required) | Quellenart (`ServerStream`/`LocalFile`) |
| `Location` | `string` (required) | Stream-URL inkl. `access_token`-Query bzw. lokaler Dateipfad |
| `ResumePosition` | `TimeSpan?` | Fortsetzungsposition — von `PlayerPage.OnMediaOpened` als `pendingResumePosition` gemerkt und per `ApplyResumeSeekAsync` angesprungen; `null` = Start am Anfang |

## `PlayerEpisodeRef`
Datei: `VideoPlayer.Mvu/PlayerEpisodeRef.cs`

Verweis auf eine Folge-Episode in `PlayerState.UpcomingEpisodes`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaId` | `long` (required) | Id der Episode |
| `Title` | `string` (required) | Anzeigetitel |

## `PlaylistPlaybackStartInfo` und `PlaylistNavigationItem`
Datei: `VideoPlayer.Mvu/PlaylistService.cs` (Zeilen 35 bzw. 69)

Antwort-Records des Playlist-Dienstes für Start (`play`) und Navigation (`play/next|previous|advance`). `PlaylistNavigationItem` ist das Ergebnis des Fehler-Rückfalls im Playlist-Kontext (`AdvancePlaylistEntryEffect` → `PlaylistService.AdvanceAsync` → `PlaylistNavigationCompletedMessage`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `PlaylistId` / `PlaylistName` / `TotalCount` / `CurrentPosition` / `CurrentEntryId` | div. | Nur `PlaylistPlaybackStartInfo`: Playlist-Kontext des Starts |
| `EntryId` / `Position` | `long` / `int` | Nur `PlaylistNavigationItem`: Identität/Position des Zieleintrags |
| `MediaType` / `MediaId` / `Title` | `string` / `long` / `string` | Medium des Eintrags |
| `StartPositionSeconds` | `long` | Fortsetzungsposition des Eintrags (Weiterschauen-Stand); wird als `StartPositionSeconds`-Override in `ResolvePlaybackSourceEffect` weitergereicht |

## Relevante `AppMessage`-Records
Datei: `VideoPlayer.Mvu/AppMessage.cs`

| Record | Parameter | Rolle im Wiedergabepfad |
|--------|-----------|--------------------------|
| `PlayRequestedMessage` (Z. 968) | `MediaType`, `MediaId`, `Title` | Wiedergabe-Anforderung → `StartPlayback` |
| `PlayerPageOpenedMessage` (Z. 974) | — | Einmalig beim Erscheinen der Seite (setzt `Attempted`) |
| `PlaybackSourceResolvedMessage` (Z. 986) | `MediaType`, `MediaId`, `Source`, `Error` | Ergebnis der Quellauflösung; `Source = null` → Rückfall wie `PlaybackFailedMessage` |
| `PlaybackProgressMessage` (Z. 1003) | `Position`, `Duration`, `IsPlaying`, `IsBuffering` | Positions-Takt; triggert „angesehen" und Auto-Fortsetzung |
| `PlaybackEndedMessage` (Z. 1010) | — | Reguläres Ende; plausibilitätsgeprüft (`IsPlaybackEndPlausible`) |
| `PlaybackFailedMessage` (Z. 1013) | — | **Einzige Fehler-Meldung** — wird von `PlayerPage` an drei Stellen dispatched (Quellen-Reset/-Setzung über `FailPlaybackStart`, `OnMediaFailed`, **und** dem Seek-catch in `ApplyResumeSeekAsync`); löst in `UpdatePlaybackFailed` stets den Rückfall aus. Kein Parameter, keine Unterscheidung der Fehlerursache. |
| `PlayerClosedMessage` (Z. 1030) | — | Seite verlassen → Abschluss-Fortschritt + Reset |
| `PictureInPictureRequestedMessage` / `PictureInPictureChangedMessage` | — / `IsActive` | PiP-Umschaltung |
| `PlaylistNextRequestedMessage` / `PlaylistPreviousRequestedMessage` (Z. 595/603) | — | Manuelle Playlist-Navigation |
| `PlaylistNavigationCompletedMessage` (Z. 620) | `PlaylistId`, `CurrentEntryId`, `Result`, `Forward`, `Error` | Ergebnis der Playlist-Navigation inkl. `advance` (Fehler-Rückfall) |

## Relevante `AppEffect`-Records
Datei: `VideoPlayer.Mvu/AppEffect.cs`

| Record | Parameter | Rolle |
|--------|-----------|-------|
| `NavigateToPlayerEffect` (Z. 604) | — | Navigation zur Shell-Route `player` |
| `ResolvePlaybackSourceEffect` (Z. 620) | `MediaType`, `MediaId`, `StartPositionSeconds = null` | Quellauflösung; Ergebnis → `PlaybackSourceResolvedMessage`. `StartPositionSeconds` trägt die bereits bekannte Fortsetzungsposition (Playlist-Antwort). |
| `AdvancePlaylistEntryEffect` (Z. 406) | `PlaylistId`, `CurrentEntryId` | Auto-Fortsetzung **und** Fehler-Rückfall im Playlist-Kontext → `POST api/playlists/{id}/play/advance`; Ergebnis → `PlaylistNavigationCompletedMessage` |
| `ReportPlaybackProgressEffect` (Z. 652) | `MediaType`, `MediaId`, `Position`, `Duration`, `PlaylistId` | Fortschrittsmeldung an den Server |
| `LoadUpcomingEpisodesEffect` (Z. 636) | `MediaId`, `ShowId` | Serienkontext/Fortsetzungsliste beim Direktstart laden |
| `TogglePictureInPictureEffect` (Z. 664) | — | PiP-Umschaltung |

## `AppSettings` (Wiedergabe-relevante Felder)
Datei: `VideoPlayer.Storage/AppSettings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `AutoResumeEnabled` | `bool` (Default `true`) | Auto-Fortsetzung am Medienende |
| `AutoResumeRemainingSeconds` | `int` (Default `300`, Clamp 30–3600) | Restlaufzeit-Fenster für die Auto-Fortsetzung |
| `WatchedEndWindowSeconds` | `int` (Default `300`, Clamp 30–3600) | Endfenster der „angesehen"-Schwelle |

Hinweis: Das interne `ResumeSeekTimeout` (10 s) ist **kein** Setting — es steht als `private static readonly`-Feld in `PlayerPage` (Zeile 26). Die Konstante `PlaybackEndedToleranceSeconds` (300 s) ist `private` in `AppUpdate` (Zeile 88).
