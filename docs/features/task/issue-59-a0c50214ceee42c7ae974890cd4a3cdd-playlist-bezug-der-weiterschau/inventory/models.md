# Datenmodell

Betroffene Datenmodellklassen der Anforderung „Playlist-Bezug in der Fortschrittsmeldung (Weiterschauen)". Pfade relativ zum Repository-Root (kein `src/`-Ordner).

## `PlaybackProgressReport`
Datei: `VideoPlayer.ApiClient/Models/ContinueWatching.cs` (Z. 56–69)

Request-Body von `POST api/continue-watching/progress`. Enthält **keine** Playlist-Felder — der Playlist-Bezug geht hier verloren.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` | `string` | Medientyp (z. B. `movie`, `episode`); wird mit dem CLR-normalisierten Namen aus `PlayerState.MediaType` belegt (keine Rückübersetzung in die Server-Konvention auf diesem Pfad) |
| `MediaId` | `long` | ID des Mediums |
| `PositionSeconds` | `long` | Aktuelle Wiedergabeposition in Sekunden |
| `DurationSeconds` | `long` | Gesamtdauer in Sekunden (Server verlangt die Dauer bei jeder Meldung) |

Serialisierung: `System.Text.Json` mit `PropertyNameCaseInsensitive = true`; keine Naming-Policy → PascalCase-Feldnamen im JSON (bestätigt durch `ProgressReportServiceTests.ReportProgress_Online_SendsDirectlyWithoutQueueing`, der `MediaType`/`MediaId`/`PositionSeconds`/`DurationSeconds` im Body prüft).

## `ContinueWatchingEntry`
Datei: `VideoPlayer.ApiClient/Models/ContinueWatching.cs` (Z. 6–43)

Eintrag der Weiterschauen-Liste (`GET api/continue-watching`). Trägt den Playlist-Bezug bereits **eingehend** — der umgekehrte Pfad ist vorhanden.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` | `string` | Medientyp des Eintrags (Server-Konvention: `movie`, `episode`, `show`, …) |
| `Entry` | `MediaEntry?` | Zugehöriger Medieneintrag (polymorph) |
| `PositionSeconds` | `long` | Gemerkte Wiedergabeposition |
| `DurationSeconds` | `long?` | Gesamtdauer, falls bekannt |
| `Title` | `string` | Anzeigetitel |
| `PosterPictureId` | `long?` | Posterbild-Id |
| `PlaylistId` | `long?` | Playlist, aus deren Wiedergabekontext der Eintrag entstand; `null` ohne Bezug |
| `PlaylistName` | `string?` | Name der Playlist; `null` ohne `PlaylistId` |
| `PlaylistEntryId` | `long?` | Playlist-Eintrag, dem das Medium aktuell entspricht; Grundlage von `POST api/playlists/{id}/play?entryId=…` |

## `PlayerState`
Datei: `VideoPlayer.Mvu/PlayerState.cs` (Z. 9–122)

Zustand der Wiedergabeseite im MVU-Model (`AppModel.Player`, `AppModel.cs` Z. 209). Trägt den Playlist-Wiedergabekontext bereits — die Meldungspfad-Erzeugung nutzt ihn bislang nicht.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` | `string?` | Medientyp (normalisiert, CLR-Konvention) |
| `MediaId` | `long` | Id des abzuspielenden Mediums |
| `Title` | `string?` | Anzeigetitel |
| `Source` | `PlaybackSource?` | Aufgelöste Wiedergabequelle |
| `IsResolving` | `bool` | Quellauflösung/Playlist-Navigation läuft |
| `Attempted` | `bool` | Wiedergabeversuch gestartet |
| `Position` | `TimeSpan` | Aktuelle Position (letzter Takt) |
| `Duration` | `TimeSpan` | Gesamtdauer |
| `IsPlaying` | `bool` | Wiedergabe läuft |
| `IsBuffering` | `bool` | Player puffert |
| `IsWatched` | `bool` | „angesehen"-Schwelle erreicht |
| `IsPictureInPicture` | `bool` | PiP-/Mini-Player-Modus |
| `UpcomingEpisodes` | `IReadOnlyList<PlayerEpisodeRef>` | Serien-Fortsetzungsliste (leer im Playlist-Kontext) |
| `HasNextEpisode` | `bool` | `UpcomingEpisodes.Count > 0` |
| `PlaylistId` | `long?` | Id der Playlist des Wiedergabekontexts; `null` ohne Playlist-Wiedergabe |
| `PlaylistName` | `string?` | Name der wiedergegebenen Playlist |
| `PlaylistEntryId` | `long` | Id des aktuell gespielten Playlist-Eintrags; `0` ohne Kontext |
| `PlaylistPosition` | `int` | 1-basierte Position in der Playlist-Reihenfolge |
| `PlaylistTotalCount` | `int` | Gesamtzahl der Einträge laut Server |
| `PlaylistNextExhausted` | `bool` | Server meldete keinen Folgeeintrag (204) |
| `IsPlaylistPlayback` | `bool` | `PlaylistId is > 0 && PlaylistEntryId > 0` (Z. 90) — der Kontext-Guard der Anforderung |
| `CanNavigateNext` / `CanNavigatePrevious` | `bool` | Heuristik für die Navigations-Schaltflächen |
| `PlaylistLabel` | `string` | Kontextzeile („„Name" · Eintrag n/m") |
| `Error` | `string?` | Fehlertext der letzten Störung |
| `HasError` | `bool` | `Error is not null` |
| `LastReportedPositionSeconds` | `double` | Zuletzt gemeldete Position — Throttling der Meldungen (Startwert `-1`) |
| `IsActive` | `bool` | `MediaId > 0 && MediaType is not null` |

Belegung des Playlist-Kontexts: `UpdatePlaylistPlaybackStarted` (aus `PlaylistPlaybackStartedMessage`, `AppUpdate.cs` Z. 2449–2483) und `UpdatePlaylistNavigationCompleted` (`PlaylistNavigationCompletedMessage`, Z. 2540–2599, `PlaylistEntryId = next.EntryId`). Kontextlose Starts über `StartPlayback` (Z. 3628–3644) lassen die Felder unbelegt.

## `ReportPlaybackProgressEffect`
Datei: `VideoPlayer.Mvu/AppEffect.cs` (Z. 651–655)

MVU-Effekt (positional record) der Fortschrittsmeldung. Enthält **keine** Playlist-Felder.

| Parameter | Typ | Beschreibung / Zweck |
|-----------|-----|----------------------|
| `MediaType` | `string` | Medientyp (normalisiert, CLR-Konvention) |
| `MediaId` | `long` | Id des Mediums |
| `Position` | `TimeSpan` | Zu meldende Wiedergabeposition |
| `Duration` | `TimeSpan` | Gesamtdauer |

Erzeugt an drei Stellen in `AppUpdate.cs` (siehe [Logik](logic.md)): Fortschrittstakt (Z. 3816–3817), Wiedergabeende (Z. 3919–3920), Schließen (Z. 4082–4083). Ausgeführt von `ShellAppEffectHandler.ReportPlaybackProgressAsync`.

## `PlaybackProgressMessage`
Datei: `VideoPlayer.Mvu/AppMessage.cs` (Z. 1003–1007)

Positions-Takt der Wiedergabeseite (`PlayerPage` 5-s-Timer bzw. `StateChanged`).

| Parameter | Typ | Beschreibung / Zweck |
|-----------|-----|----------------------|
| `Position` | `TimeSpan` | Aktuelle Wiedergabeposition |
| `Duration` | `TimeSpan` | Gesamtdauer |
| `IsPlaying` | `bool` | Wiedergabe läuft |
| `IsBuffering` | `bool` | Player puffert/öffnet |

Trägt **keine** Playlist-Felder — der Kontext liegt ausschließlich im `PlayerState` (entspricht der Anforderung: keine UI-Änderung nötig).

## Playlist-Meldungen im MVU-Zyklus
Datei: `VideoPlayer.Mvu/AppMessage.cs`

| Record | Felder | Zweck |
|--------|--------|-------|
| `PlaylistPlaybackStartRequestedMessage` (Z. 570) | `PlaylistId`, `EntryId` (`long?`) | Wiedergabe-Anforderung („Abspielen") |
| `PlaylistPlaybackStartedMessage` (Z. 584) | `PlaylistId`, `Start` (`PlaylistPlaybackStartInfo?`), `Error` | Ergebnis von `POST api/playlists/{id}/play` — belegt den `PlayerState`-Playlist-Kontext |
| `PlaylistNextRequestedMessage` (Z. 595) / `PlaylistPreviousRequestedMessage` (Z. 603) | — | Manuelle Navigation im Player |
| `PlaylistNavigationCompletedMessage` (Z. 620) | `PlaylistId`, `CurrentEntryId`, `Result` (`PlaylistNavigationItem?`), `Forward`, `Error` | Ergebnis von `play/next`, `play/previous`, `play/advance`; `CurrentEntryId` bindet die Antwort an den auslösenden Stand |
| `PlaybackEndedMessage` (Z. 1010) / `PlaybackFailedMessage` (Z. 1013) / `PlayerClosedMessage` (Z. 1030) | — | Ende, Fehlschlag, Seitenende |

## `PendingProgressSyncEntry`
Datei: `VideoPlayer.Storage/Entities/PendingProgressSyncEntry.cs`

Persistenter Warteschlangen-Eintrag (Tabelle `pending_progress`, backup-pflichtig, `[Table]`). Enthält **keine** Playlist-Spalten — der Offline-/Nachsendepfad verliert den Kontext ebenfalls.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` | Primärschlüssel (`[PrimaryKey, AutoIncrement]`) |
| `VideoId` | `int` | Id des Videos (`[Indexed]`) |
| `VideoType` | `string` | Medientyp |
| `PositionTicks` | `long` | Wiedergabeposition in Ticks |
| `DurationTicks` | `long` | Gesamtdauer in Ticks (per `EnsureColumnAsync`-Migration nachgezogen, `BackupDatabase.InitializeAsync` Z. 45) |
| `UpdatedAtUtc` | `DateTime` | Zeitpunkt der letzten Aktualisierung (UTC) |

Deduplikation: `EnqueueAsync` löscht pro `VideoType`/`VideoId` den Vorgänger und fügt den neuesten Stand ein — bei Kontextwechsel desselben Mediums gewinnt der zuletzt gemeldete Stand (bestehende Semantik).

## `HomeMediaItem`
Datei: `VideoPlayer.Mvu/HomeMediaItem.cs`

Anzeigeeintrag der Startseiten-Listen — trägt den Playlist-Bezug des Weiterschauen-Eintrags für den Wiedereinstieg.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Id des Mediums |
| `MediaType` | `string` | Medientyp (Server- oder CLR-Konvention je Herkunft) |
| `Title` | `string` | Anzeigetitel |
| `PosterPictureId` | `long?` | Bild-Id für `api/pictures/{id}` |
| `Progress` | `double?` | Fortschritt 0–1 |
| `ShowId` / `SeasonId` / `EpisodeId` | `long?` | Serienkontext-Hints |
| `PlaylistId` | `long?` | Playlist-Bezug des Weiterschauen-Eintrags (Z. 55) |
| `PlaylistName` | `string?` | Name der Playlist |
| `PlaylistEntryId` | `long?` | Playlist-Eintrag des Mediums (Z. 66) |
| `IsPlaceholder` / `IsSelectable` / `CardOpacity` / `HasPoster` / `ShowProgress` | `bool`/`double` | Anzeige-Hilfsflags |

Der Auswahl-Guard `item.PlaylistId is > 0 && item.PlaylistEntryId is > 0` in `UpdateContinueWatchingItemSelected` (`AppUpdate.cs` Z. 1398–1406) startet playlist-gebundene Einträge über `StartPlaylistPlaybackEffect` — der Wiedereinstiegspfad ist vorhanden und bleibt unangetastet.

## `PlaylistPlaybackStartInfo` / `PlaylistNavigationItem`
Datei: `VideoPlayer.Mvu/PlaylistService.cs` (Z. 35–63 bzw. 69–88)

Plattformneutrale Ergebnis-Records des Playlist-Wiedergabestarts bzw. der Navigation (aus den Server-Antworten gemappt).

`PlaylistPlaybackStartInfo`: `PlaylistId`, `PlaylistName`, `TotalCount`, `CurrentPosition`, `CurrentEntryId`, `MediaType`, `MediaId`, `Title`, `StartPositionSeconds` (alle `required`).

`PlaylistNavigationItem`: `EntryId`, `Position`, `MediaType`, `MediaId`, `Title`, `StartPositionSeconds` (alle `required`).

## `PlaylistPlaybackStart` / `PlaylistNavigationResult` / `PlaylistEntry`
Datei: `VideoPlayer.ApiClient/Models/PlaylistPlaybackStart.cs`, `PlaylistNavigationResult.cs`, `PlaylistEntry.cs`

API-DTOs der Playlist-Play-Endpunkte: `PlaylistPlaybackStart` trägt `PlaylistId`, `PlaylistName`, `TotalCount`, `CurrentPosition`, `CurrentEntryId`, `CurrentEntry` (`PlaylistEntry?`), `StreamUrl`, `MediaType`, `MediaId`, `StartPositionSeconds`; `PlaylistNavigationResult` trägt `Entry`, `Position`, `StartPositionSeconds`. `PlaylistEntry.Id` ist die Eintrags-Id (wird zu `PlaylistEntryId`).

## Altbestand: `ContinueWatchingDto` / `ProgressPayload`
Datei: `VideoWebPlayer.Client/Models/ContinueWatchingDto.cs`, `VideoWebPlayer.Client/VideoWebPlayerClient.cs`

`ContinueWatchingDto` (`MediaType`, `Entry`, `PositionSeconds`, `DurationSeconds`, `Title`, `PosterPictureId`) und das interne `ProgressPayload` (`MediaType`, `MediaId`, `PositionSeconds`, `DurationSeconds`, `VideoWebPlayerClient.cs` Z. 558–564) tragen **keine** Playlist-Felder — dieselbe Lücke wie im neuen Pfad (per Anforderung außerhalb des Issue-Fokus).
