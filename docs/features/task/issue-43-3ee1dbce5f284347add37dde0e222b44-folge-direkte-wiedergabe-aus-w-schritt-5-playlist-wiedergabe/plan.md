# Umsetzungsplan: Playlist-Wiedergabe und Folgevideo-Auflösung (inkl. Weiterschauen mit Playlist-Bezug)

## Übersicht

Playlist-Einträge werden abspielbar: aus der `PlaylistDetailsPage` (Kopf-Button „Abspielen" = erster abspielbarer Eintrag; Eintrag antippen = Wiedergabe ab diesem Eintrag) sowie aus der Weiterschauen-Liste bei playlist-gebundenen Einträgen. Der `PlayerState` erhält einen Playlist-Kontext (`PlaylistId`/`PlaylistEntryId` u. a.); Folgevideo-Ermittlung für manuelle Vor-/Zurück-Navigation, Auto-Fortsetzung und Fehler-Rückfall delegiert an die serverseitigen Play-Endpunkte (`POST api/playlists/{id}/play`, `.../play/next`, `.../play/previous`, `.../play/advance`). Weiterschauen-Einträge ohne Playlist-Bezug behalten die Serienkontext-Auflösung.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Eintragsauswahl `PlaylistDetailsPage` | Tap auf abspielbaren Eintrag (`movie`/`tvshowepisode`, `IsAccessible`) startet Wiedergabe ab diesem Eintrag; nicht abspielbare Typen öffnen weiterhin die Medien-Detailseite | `PlaylistEntrySelectedMessage`-Doku benennt die Detail-Navigation explizit als Übergang („Wiedergabe gehört zum Folgeschritt"); Tap = „gewählter Eintrag" ist der direkteste Erfüllungspfad des AK, E2E-tauglich via `InvokeAsync(collection, itemIndex)` |
| Start-„Abspielen" im Kopfbereich | Neuer `PlaylistDetailsPlayButton` → `PlaylistPlaybackStartRequestedMessage(PlaylistId, EntryId: null)` | AK „erster abspielbarer Eintrag" entspricht `entryId = null` serverseitig |
| Quellauflösung bei Playlist-Wiedergabe | Bestehender `ResolvePlaybackSourceEffect`/`PlaybackSourceService` wird genutzt; der Effekt erhält einen optionalen `StartPositionSeconds`-Override aus der Server-Antwort | `DtoPlaylistNavigationResult` liefert keine `streamUrl`, die `items/…/stream`-Auflösung existiert bereits inkl. lokaler-Datei-Bevorzugung; die vom Server gelieferte `StartPositionSeconds` ist exakter als ein erneuter Weiterschauen-Lookup (gleiche MediaId in mehreren Playlist-Kontexten) und bleibt auch bei 0 bindend |
| Playlist-Navigation (next/previous/advance) | Drei getrennte `AppEffect`-Records (`LoadNextPlaylistEntryEffect`, `LoadPreviousPlaylistEntryEffect`, `AdvancePlaylistEntryEffect`) — 1:1-Abbild der Endpunkte | Vermeidet einen neuen Enum (kein `EnumCoverageTests`-Eintrag nötig), einheitliche Ergebnis-Nachricht `PlaylistNavigationCompletedMessage` mit `Kind` (eigener kleiner Enum wäre Alternative — verworfen zugunsten booleschem `WasForward`/`WasAdvance`-Konzept: siehe unten `IsAdvance`-Flag) |
| Manuelle Navigation vs. Auto-Fortsetzung | `play/next`/`play/previous` für Buttons, `play/advance` für Auto-Fortsetzung und Fehler-Rückfall | Entspricht der serverseitigen Semantik-Trennung der Endpunkte |
| Playlist-Kontext im `PlayerState` | Flache Felder `PlaylistId`, `PlaylistName`, `PlaylistEntryId`, `PlaylistPosition`, `PlaylistTotalCount` + `IsPlaylistPlayback`-Ableitung | Konsistent mit den flachen Feldern des bestehenden Records; `IsPlaylistPlayback` steuert die Folgevideo-Quelle (Playlist statt `UpcomingEpisodes`) |
| Vor/Zurück-Buttons im Player | Zwei neue Buttons in `PlayerPage` (`PlayerPreviousButton`/`PlayerNextButton`), sichtbar nur bei `IsPlaylistPlayback`; Aktivierung heuristisch über `PlaylistPosition`/`PlaylistTotalCount` | Es existiert keine Navigationssteuerung im Player (auch nicht für Episoden); die Anforderung verlangt sie nur für den Playlist-Kontext. Serverseitige 204-Antworten ohne Folgeeintrag bleiben ohne sichtbares Feedback (Positionsheuristik verhindert den Großteil der Leerklicks; ein verbleibender „Klick ohne Effekt" bei abspielbaren Einträgen hinter nicht abspielbaren Endpositionen ist toleriert) |
| Weiterschauen mit Playlist-Bezug | `UpdateContinueWatchingItemSelected` wählt bei `PlaylistId > 0 && PlaylistEntryId > 0` den Playlist-Startpfad (`StartPlaylistPlaybackEffect` mit `PlaylistEntryId`) statt `StartPlayback` + `LoadUpcomingEpisodesEffect` | `PlaylistEntryId` ist `null`, wenn das Medium nicht mehr Teil der Playlist ist (Server-Doku) → sauberer Rückfall auf den Serienpfad |
| Fehler beim Playlist-Start | `PlaylistPlaybackStartedMessage` mit `Error` → `PlayerState` mit `Error` + `NavigateToPlayerEffect` (Fehlerbanner der PlayerPage) | Keine Fehleranzeige-Infrastruktur auf Detailseite/Home; das Player-Fehlerpanel existiert |
| Playlist-Felder im Weiterschauen-Cache | Keine Schema-Änderung: `ContinueWatchingEntry` wird als JSON in `cached_lists` persistiert — neue Felder gehen automatisch mit | Deserialisierung `PropertyNameCaseInsensitive`; kein Migrationsschritt nötig |

## Programmabläufe

### Playlist-Wiedergabestart aus der Detailansicht

1. `PlaylistDetailsPage`: Klick auf `PlaylistDetailsPlayButton` → `PlaylistPlaybackStartRequestedMessage(PlaylistId, null)`; Tap auf Eintrag → `PlaylistEntrySelectedMessage(item)` (unverändert dispatcht).
2. `AppUpdate`:
   - `UpdatePlaylistPlaybackStartRequested` → `StartPlaylistPlaybackEffect(PlaylistId, EntryId)` (kein Model-Wechsel).
   - `UpdatePlaylistEntrySelected`: `MediaTypeNames.Normalize(item.MediaType)` ∈ {`movie`, `tvshowepisode`} && `item.IsAccessible` → `StartPlaylistPlaybackEffect(PlaylistDetails.PlaylistId, item.EntryId)`; sonst bisherige `NavigateToMediaDetailsEffect`.
3. `ShellAppEffectHandler.StartPlaylistPlaybackAsync`: `PlaylistService.StartPlaybackAsync(playlistId, entryId)` → bei Erfolg `PlaylistPlaybackStartedMessage(start: info)`; bei Fehlschlag (ApiException/Transport/„kein abspielbarer Eintrag") `PlaylistPlaybackStartedMessage(error: PlaybackUnavailableError)`.
4. `AppUpdate.UpdatePlaylistPlaybackStarted`:
   - Erfolg → `Player = new PlayerState { MediaType = normalize(start.MediaType), MediaId = start.MediaId, Title = start.Title, PlaylistId, PlaylistName, PlaylistEntryId = start.CurrentEntryId, PlaylistPosition = start.CurrentPosition, PlaylistTotalCount = start.TotalCount, IsResolving = true, Attempted = true, UpcomingEpisodes = [] }` + `[NavigateToPlayerEffect, ResolvePlaybackSourceEffect(mediaType, mediaId, start.StartPositionSeconds)]`.
   - Fehler → `Player = new PlayerState { Title = start?.PlaylistName, Error = error }` + `NavigateToPlayerEffect`.

Beteiligte: `PlaylistDetailsPage`, `AppUpdate`, `ShellAppEffectHandler`, `PlaylistService`, `VideoPlayerApiClient`.

### Playlist-Wiedergabestart aus der Weiterschauen-Liste

1. `ContinueWatchingItemSelectedMessage` → `UpdateContinueWatchingItemSelected`: vor dem `StartPlayback`-Pfad Prüfung `item.PlaylistId is > 0 && item.PlaylistEntryId is > 0` → `StartPlaylistPlaybackEffect(item.PlaylistId.Value, item.PlaylistEntryId.Value)` (abspielbare Typen wie bisher vorgefiltert: bei `movie`/`tvshowepisode`; nicht abspielbare Typen behalten Detail-Navigation; Playlist-Guard läuft nach der Typprüfung).
2. Weiter wie Ablauf „Start aus Detailansicht" ab Schritt 3 — `PlayerState.UpcomingEpisodes` bleibt leer, kein `LoadUpcomingEpisodesEffect` im Playlist-Pfad.

Beteiligte: `HomeListService` (Mapping), `AppUpdate`, `PlaylistService`.

### Manuelle Vor-/Rückwärts-Navigation im Player

1. `PlayerPage`: `PlayerNextButton`/`PlayerPreviousButton` (nur bei `IsPlaylistPlayback` sichtbar) → `PlaylistNextRequestedMessage`/`PlaylistPreviousRequestedMessage`.
2. `AppUpdate`: Guard `player.IsPlaylistPlayback && !player.IsResolving` (verhindert Doppelversand während eines ausstehenden Navigations-/Auflösungslaufs) → `Player = player with { IsResolving = true, Error = null }` + `LoadNextPlaylistEntryEffect(PlaylistId, PlaylistEntryId)` bzw. `LoadPreviousPlaylistEntryEffect(...)`. Aktivierungsheuristik der Buttons: `PlayerState.CanNavigateNext => IsPlaylistPlayback && !PlaylistNextExhausted && PlaylistPosition < PlaylistTotalCount`, `CanNavigatePrevious => IsPlaylistPlayback && PlaylistPosition > 1` (`IsEnabled`-Bindung).
3. `ShellAppEffectHandler`: `PlaylistService.GetNextEntryAsync`/`GetPreviousEntryAsync` → `PlaylistNavigationCompletedMessage(kind, result)`; Server-204 → `result = null`; Fehlschlag → `error`.
4. `AppUpdate.UpdatePlaylistNavigationCompleted`:
   - `result != null` → `Player` wechselt auf das neue Medium (`MediaType`, `MediaId`, `Title = result.Title`, `PlaylistEntryId = result.EntryId`, `PlaylistPosition = result.Position`, `PlaylistNextExhausted = false`, Wiedergabe-Reset wie `AdvanceToNextEpisode`, `IsResolving = true`, `Error` bleibt als Hinweis wenn gesetzt) + `ResolvePlaybackSourceEffect(result.MediaType, result.MediaId, result.StartPositionSeconds)`.
   - `result == null` (204) → `Player = player with { IsResolving = false, PlaylistNextExhausted = true }` (kein Folgeeintrag in Vorwärtsrichtung — weitere Auto-/Next-Versuche werden unterdrückt; bei `IsAdvance` bleibt der Endzustand stehen).
   - `error` → `Player = player with { IsResolving = false, Error = PlaybackUnavailableError }`.
   - Identitätswache: nur anwenden, wenn `player.PlaylistId == message.PlaylistId` und `player.PlaylistEntryId == message.CurrentEntryId` (abgelöste Antworten verwerfen).

Beteiligte: `PlayerPage`, `AppUpdate`, `ShellAppEffectHandler`, `PlaylistService`, `PlaybackSourceService`.

### Auto-Fortsetzung und Fehler-Rückfall im Playlist-Kontext

1. `UpdatePlaybackProgress` (Restlaufzeit ≤ `AutoResumeRemainingSeconds`, `AutoResumeEnabled`, `IsPlaying`) und `UpdatePlaybackEnded` (`AutoResumeEnabled`): statt `player.HasNextEpisode` prüft der Playlist-Zweig `player.IsPlaylistPlayback && !player.IsResolving && !player.PlaylistNextExhausted` → `AdvancePlaylistEntryEffect(PlaylistId, PlaylistEntryId)` (mit `IsResolving = true` am Player — verhindert mehrfache Advance-Versendung durch Folge-Ticks im Fortsetzungsfenster; `PlaylistNextExhausted` stoppt erneute Versuche nach einer 204-Antwort — Fehler/Ended-Fortschrittsmeldungen wie bisher zusätzlich).
2. `UpdatePlaybackSourceResolved` (Quelle `null`) und `UpdatePlaybackFailed` (Fehler-Rückfall): bei `IsPlaylistPlayback` → `AdvancePlaylistEntryEffect` statt `AdvanceToNextEpisode`; der bisherige Fehlertext bleibt bis zur Auflösung als Hinweis (`Error = error` beim Advance-Übergang analog `AdvanceToNextEpisode`).
3. `ShellAppEffectHandler` → `PlaylistService.AdvanceAsync` → `PlaylistNavigationCompletedMessage(IsAdvance: true, result/error)` → wie manuelle Navigation; bei `result == null` (204) bleibt der Endzustand stehen (`IsResolving = false`), bei `error` → `Player.Error`.

Beteiligte: `AppUpdate`, `ShellAppEffectHandler`, `PlaylistService`.

### Weiterschauen-Mapping (Playlist-Bezug)

1. `ContinueWatchingEntry` (ApiClient-DTO) erhält `PlaylistId` (`long?`), `PlaylistName` (`string?`), `PlaylistEntryId` (`long?`) — Server-Feldnamen `playlistId`/`playlistName`/`playlistEntryId` (case-insensitive Deserialisierung).
2. `HomeListService.MapContinueWatching` mappt die drei Felder auf `HomeMediaItem.PlaylistId`/`.PlaylistName`/`.PlaylistEntryId`.
3. `UpdateContinueWatchingItemSelected` nutzt den Bezug wie oben beschrieben.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `PlaylistPlaybackStart` | Datenmodellklasse (`VideoPlayer.ApiClient/Models/`) | Pendant zu `DtoPlaylistPlaybackStart` (alle Felder inkl. `CurrentEntry` als `PlaylistEntry`, `StreamUrl`, `StartPositionSeconds`) |
| `PlaylistNavigationResult` | Datenmodellklasse (`VideoPlayer.ApiClient/Models/`) | Pendant zu `DtoPlaylistNavigationResult` (`Entry`, `Position`, `StartPositionSeconds`) |
| `PlaylistPlaybackStartInfo` | Record (`VideoPlayer.Mvu`, in `PlaylistService.cs`) | Anzeige-/State-Daten des aufgelösten Starteintrags (`PlaylistId`, `PlaylistName`, `TotalCount`, `CurrentPosition`, `CurrentEntryId`, `MediaType` (normalisiert), `MediaId`, `Title`, `StartPositionSeconds`) |
| `PlaylistNavigationItem` | Record (`VideoPlayer.Mvu`, in `PlaylistService.cs`) | Anzeige-/State-Daten eines Navigationsergebnisses (`EntryId`, `Position`, `MediaType` (normalisiert), `MediaId`, `Title`, `StartPositionSeconds`) |
| `PlaylistPlaybackStartRequestedMessage` | `AppMessage` | UI-Auslöser Playlist-Wiedergabe (Kopf-Button; `PlaylistId`, `EntryId?`) |
| `PlaylistPlaybackStartedMessage` | `AppMessage` | Ergebnis des Start-Effekts (`PlaylistId`, `Start` (`PlaylistPlaybackStartInfo?`), `Error` (`string?`)) |
| `PlaylistNextRequestedMessage` / `PlaylistPreviousRequestedMessage` | `AppMessage` | Player-Buttons Vor/Zurück (parameterlos — Kontext aus `PlayerState`) |
| `PlaylistNavigationCompletedMessage` | `AppMessage` | Ergebnis eines Navigations-Effekts (`PlaylistId`, `CurrentEntryId`, `Result` (`PlaylistNavigationItem?`), `IsAdvance`, `Error` (`string?`)) |
| `StartPlaylistPlaybackEffect` | `AppEffect` | `POST api/playlists/{id}/play` ausführen (`PlaylistId`, `EntryId?`) |
| `LoadNextPlaylistEntryEffect` / `LoadPreviousPlaylistEntryEffect` / `AdvancePlaylistEntryEffect` | `AppEffect` | `POST …/play/next`/`previous`/`advance` (`PlaylistId`, `CurrentEntryId`) |
| `PlaylistPlaybackTests` | E2E-Testklasse | Szenarien der Playlist-Wiedergabe |
| `AppUpdateTests_PlaylistPlayback` | Unit-Testklasse | Update-Verträge der Playlist-Wiedergabe |

## Änderungen an bestehenden Klassen

### `ContinueWatchingEntry` (Datenmodellklasse, `VideoPlayer.ApiClient/Models/ContinueWatching.cs`)

- **Neue Eigenschaften:** `PlaylistId` (`long?`), `PlaylistName` (`string?`), `PlaylistEntryId` (`long?`) — Playlist-Bezug des Weiterschauen-Eintrags (Server-Felder `playlistId`/`playlistName`/`playlistEntryId`).

### `VideoPlayerApiClient` (Klasse)

- **Neue Methoden:**
  - `StartPlaylistPlaybackAsync(long playlistId, long? entryId, CancellationToken)` → `Task<PlaylistPlaybackStart>` — `POST api/playlists/{id}/play` (+`?entryId=` bei gesetztem Wert).
  - `GetNextPlaylistEntryAsync(long playlistId, long currentEntryId, CancellationToken)` → `Task<PlaylistNavigationResult?>` — `POST api/playlists/{id}/play/next?currentEntryId=`; 204 → `null`.
  - `GetPreviousPlaylistEntryAsync(...)` → analog `play/previous`.
  - `AdvancePlaylistAsync(...)` → analog `play/advance`.
  - Privater Helfer `PostJsonAsync<T>(string endpoint, CancellationToken)`-Variante ohne Request-Body (POST ohne Content); 204-Behandlung → `null` für die Navigationsmethoden (via `ApiException`/Statusabfrage des Responses — Umsetzung: dedizierte `PostAsync`-Hilfsmethode, die `HttpResponseMessage` liefert; Navigationsmethoden prüfen `StatusCode == NoContent`).

### `HomeMediaItem` (Record, `VideoPlayer.Mvu`)

- **Neue Eigenschaften:** `PlaylistId` (`long?`), `PlaylistName` (`string?`), `PlaylistEntryId` (`long?`) — Playlist-Bezug des Weiterschauen-Eintrags; `null` ohne Bezug.

### `PlayerState` (Record, `VideoPlayer.Mvu`)

- **Neue Eigenschaften:** `PlaylistId` (`long?`), `PlaylistName` (`string?`), `PlaylistEntryId` (`long`), `PlaylistPosition` (`int`), `PlaylistTotalCount` (`int`), `PlaylistNextExhausted` (`bool` — Server meldete keinen Folgeeintrag; unterdrückt weitere Vorwärts-Navigations-/Advance-Versuche bis zum nächsten Medienwechsel).
- **Neue Ableitungen:** `IsPlaylistPlayback => PlaylistId is > 0 && PlaylistEntryId > 0`; `CanNavigateNext => IsPlaylistPlayback && !PlaylistNextExhausted && PlaylistPosition < PlaylistTotalCount`; `CanNavigatePrevious => IsPlaylistPlayback && PlaylistPosition > 1`; `PlaylistLabel` (Anzeige „„Name" · Eintrag n/m" bzw. „„Name"" ohne bekannte Position).

### `PlaylistService` (Klasse, `VideoPlayer.Mvu`)

- **Neue Methoden:**
  - `StartPlaybackAsync(long playlistId, long? entryId, CancellationToken)` → `Task<PlaylistPlaybackStartInfo>` — ruft `StartPlaylistPlaybackAsync`, mappt auf `PlaylistPlaybackStartInfo` (`MediaType` via `MediaTypeNames.Normalize`, `Title` aus `CurrentEntry.MediaTitle` mit `MediaTitle`-Fallback).
  - `GetNextEntryAsync(long playlistId, long currentEntryId, CancellationToken)` → `Task<PlaylistNavigationItem?>` — `GetNextPlaylistEntryAsync` → Mapping; `null` bei 204.
  - `GetPreviousEntryAsync(...)` → analog `play/previous`.
  - `AdvanceAsync(...)` → analog `play/advance`.

### `PlaybackSourceService` (Klasse, `VideoPlayer.Mvu`)

- **Geänderte Methode:** `ResolveAsync` — neuer optionaler Parameter `long? resumePositionSecondsOverride` (letzter Parameter); gesetzt → Fortsetzungsposition = `TimeSpan.FromSeconds(value)` (auch bei 0 — kein Weiterschauen-Lookup mehr); `null` → bisheriges Verhalten.

### `ResolvePlaybackSourceEffect` (Record, `VideoPlayer.Mvu`)

- **Neuer Parameter:** `long? StartPositionSeconds = null` — durchgereicht aus der Playlist-Antwort an `PlaybackSourceService`.

### `HomeListService` (Klasse, `VideoPlayer.Mvu`)

- **Geänderte Methode:** `MapContinueWatching` — mappt `PlaylistId`/`PlaylistName`/`PlaylistEntryId` auf `HomeMediaItem`.

### `AppUpdate` (Klasse, `VideoPlayer.Mvu`)

- **Neue Einträge im `Update`-Switch:** `PlaylistPlaybackStartRequestedMessage`, `PlaylistPlaybackStartedMessage`, `PlaylistNextRequestedMessage`, `PlaylistPreviousRequestedMessage`, `PlaylistNavigationCompletedMessage`.
- **Geänderte Methoden:**
  - `UpdateContinueWatchingItemSelected` — Playlist-Guard (`PlaylistId > 0 && PlaylistEntryId > 0`) → `StartPlaylistPlaybackEffect` (vor `StartPlayback`/`LoadUpcomingEpisodesEffect`).
  - `UpdatePlaylistEntrySelected` — abspielbarer + zugänglicher Eintrag → `StartPlaylistPlaybackEffect` statt Detail-Navigation (nicht abspielbare Typen unverändert).
  - `UpdatePlaybackProgress`, `UpdatePlaybackEnded` — Auto-Fortsetzung bei `IsPlaylistPlayback` via `AdvancePlaylistEntryEffect` (UpcomingEpisodes-Pfad bleibt für den Serienkontext).
  - `UpdatePlaybackSourceResolved`, `UpdatePlaybackFailed` — Fehler-Rückfall bei `IsPlaylistPlayback` via `AdvancePlaylistEntryEffect`.
- **Neue Methoden:** `UpdatePlaylistPlaybackStartRequested`, `UpdatePlaylistPlaybackStarted`, `UpdatePlaylistNextRequested`, `UpdatePlaylistPreviousRequested`, `UpdatePlaylistNavigationCompleted`.

### `ShellAppEffectHandler` (Klasse, `VideoPlayer.Maui`)

- **Neue `case`-Zweige + Methoden:** `StartPlaylistPlaybackAsync(StartPlaylistPlaybackEffect)`, `NavigatePlaylistEntryAsync(LoadNextPlaylistEntryEffect/LoadPreviousPlaylistEntryEffect/AdvancePlaylistEntryEffect)` (gemeinsamer Kern, `IsAdvance`-Flag in der Ergebnisnachricht je Effekttyp).
- **Geänderte Methode:** `ResolvePlaybackSourceAsync` — reicht `resolve.StartPositionSeconds` an `PlaybackSourceService.ResolveAsync` weiter.

### `PlaylistDetailsPage` (`VideoPlayer.Maui/Pages`)

- **XAML:** Neuer Button „Abspielen" (`AutomationId="PlaylistDetailsPlayButton"`) im Kopfbereich neben `SummaryText`, sichtbar wenn Einträge vorhanden (`HasItems`); Clicked → `PlaylistPlaybackStartRequestedMessage(PlaylistId, null)`.

### `PlayerPage` (`VideoPlayer.Maui/Pages`)

- **XAML:** Zwei Buttons `PlayerPreviousButton` („Zurück")/`PlayerNextButton` („Weiter") im Overlay (eigene Zeile unter dem Kopfband, kein `InputTransparent`-Konflikt — Muster `PipButton`); Sichtbarkeit via `UpdateUi` bei `player.IsPlaylistPlayback`, `IsEnabled` an `CanNavigatePrevious`/`CanNavigateNext`. Optionale Positionszeile `PlaylistLabel` unter `TitleLabel` (`IsVisible` bei `IsPlaylistPlayback`).
- **Code-Behind:** Clicked → `PlaylistPreviousRequestedMessage`/`PlaylistNextRequestedMessage`; `UpdateUi` setzt Sichtbarkeit/Aktivierung + Label.

### `DevApiModelSnapshot` (`VideoPlayer.Mvu/DevApi`)

- **Neue Eigenschaften:** `PlayerPlaylistId` (`long`), `PlayerPlaylistEntryId` (`long`), `PlayerPlaylistPosition` (`int`) — für E2E-Nachweise des Playlist-Kontexts.

### `DevApiModels` (`VideoPlayer.E2E.Tests/Support`)

- `DevApiModel` um die drei neuen Snapshot-Felder ergänzen.

### `MockVideoServer` (`VideoPlayer.E2E.Tests/Support`)

- Playlist-Routing für `POST /api/playlists/{id}/play` (+`next`/`previous`/`advance`) in `HandlePlaylistRequest` (Auth-Prüfung wie GET-Zweig; `entryId`/`currentEntryId` aus Query).
- Play-Auflösung: `entryId` gegeben → dieser Eintrag; `null` → erster Eintrag mit `IsAccessible` && abspielbarem Typ (`Movie`/`TVShowEpisode`). `next`/`previous`/`advance` → nächster/voriger Eintrag mit denselben Kriterien in `Entries`-Reihenfolge; keiner → 204. Antworten im `DtoPlaylistPlaybackStart`-/`DtoPlaylistNavigationResult`-JSON-Schema (`startPositionSeconds` aus `MockPlaylistEntry.ResumePositionSeconds`, sonst 0).
- `MockPlaylistEntry` + `ResumePositionSeconds` (`long?`).
- Stream-Endpunkte (`HandleMovieItem`, `HandleEpisodeItem`): liefern `PlaybackVideoPath` zusätzlich für Media-Ids, die als `MediaId` in `Playlists[].Entries` (passender Typ) vorkommen.
- `ListOverride`/continue-watching: Opt-in-Felder für Playlist-Bezug am Episoden-/Movie-Eintrag (`PlaybackEpisodePlaylistId`/`PlaybackPlaylistName`/`PlaybackEpisodePlaylistEntryId` bzw. generisch `ContinueWatchingPlaylistId`/`ContinueWatchingPlaylistName`/`ContinueWatchingPlaylistEntryId`) → JSON-Felder `playlistId`/`playlistName`/`playlistEntryId`.

## Datenbankmigrationen

Keine. (Der `cached_lists`-JSON-Cache nimmt die neuen `ContinueWatchingEntry`-Felder schemafrei auf.)

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `StartPlaylistPlaybackEffect.EntryId` | `null` oder `> 0` | Ungültige Werte → Guard im Update/Effekt (kein Aufruf) |
| `PlaylistNavigationCompletedMessage` | Identitätswache `PlaylistId` + `CurrentEntryId` gegen `PlayerState` | Abgelöste Antwort → `AppTransition.Unchanged` |
| Eintragsauswahl | Nur `movie`/`tvshowepisode` + `IsAccessible` | Sonst Detail-Navigation (Bestand) bzw. keine Aktion |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Detail-Navigation von Playlist-Einträgen:** ändert für abspielbare Typen zu Direktwiedergabe — bestehender Playlists-E2E-Test (`SelectingPlaylist_ShowsEntriesInServerOrder`) selektiert nicht, unbetroffen; ggf. betroffene Unit-Tests zu `PlaylistEntrySelectedMessage` anpassen.
- **Fehler-Rückfall im Serienkontext:** unverändert — Playlist-Zweig greift nur bei `IsPlaylistPlayback`.
- **Weiterschauen-Cache älterer Stände:** fehlende Playlist-Felder deserialisieren als `null` → Serienpfad (rückwärtskompatibel).
- **`ResolvePlaybackSourceEffect`-Erweiterung:** optionaler Parameter — bisherige Aufrufer unverändert (`StartPlayback`, `AdvanceToNextEpisode` ohne Override).
- **`PlaybackSourceService.ResolveAsync`-Signatur:** interner optionaler Parameter — Tests, die `ResolveAsync` aufrufen, bleiben kompatibel.
- **Auto-Fortsetzung bei Playlist mit lokalem Download:** `PlaybackSourceService` bevorzugt weiterhin lokale Dateien auch im Playlist-Kontext — gewünscht (identisches Medium).
- **204 ohne Folgeeintrag bei manueller Navigation:** kein sichtbares Feedback — bewusst toleriert (siehe Designentscheidungen).

## Umsetzungsreihenfolge

1. **ApiClient: DTOs + Play-Methoden**
   - Voraussetzungen: Keine.
   - `ContinueWatchingEntry` um drei Playlist-Felder; neue Dateien `PlaylistPlaybackStart.cs`, `PlaylistNavigationResult.cs` (Models); `VideoPlayerApiClient` + 4 Methoden + POST-ohne-Body-Helfer (inkl. 204-Behandlung).

2. **MVU-Modelle: `HomeMediaItem`, `PlayerState`, Info-Records, `PlaylistService`**
   - Voraussetzungen: Schritt 1.
   - `HomeMediaItem` +3 Felder; `PlayerState` +5 Felder + Ableitungen; `PlaylistPlaybackStartInfo`/`PlaylistNavigationItem`; `PlaylistService` +4 Methoden; `HomeListService.MapContinueWatching` erweitern; `PlaybackSourceService.ResolveAsync` + Override-Parameter.

3. **MVU-Nachrichten/-Effekte + `AppUpdate`**
   - Voraussetzungen: Schritt 2.
   - Neue `AppMessage`/`AppEffect`-Records (deutsche XML-Doku); `AppUpdate`-Switch + Update-Methoden (`UpdatePlaylistPlaybackStartRequested`, `UpdatePlaylistPlaybackStarted`, `UpdatePlaylistNextRequested`, `UpdatePlaylistPreviousRequested`, `UpdatePlaylistNavigationCompleted`, Änderungen an `UpdateContinueWatchingItemSelected`, `UpdatePlaylistEntrySelected`, `UpdatePlaybackProgress`, `UpdatePlaybackEnded`, `UpdatePlaybackSourceResolved`, `UpdatePlaybackFailed`); `ResolvePlaybackSourceEffect` + `StartPositionSeconds`.

4. **Effekt-Handler**
   - Voraussetzungen: Schritt 3.
   - `ShellAppEffectHandler`: neue `case`-Zweige + `StartPlaylistPlaybackAsync`/`NavigatePlaylistEntryAsync`; `ResolvePlaybackSourceAsync` reicht Override weiter. Keine DI-Änderungen (`PlaylistService`/`PlaybackSourceService` bereits Singletons).

5. **UI: `PlaylistDetailsPage` + `PlayerPage`**
   - Voraussetzungen: Schritt 3.
   - „Abspielen"-Button im Playlist-Kopfbereich; Vor/Zurück-Buttons + `PlaylistLabel` im Player-Overlay; Code-Behind-Dispatches und `UpdateUi`-Sichtbarkeiten.

6. **DevApi-Snapshot**
   - Voraussetzungen: Schritt 2.
   - `DevApiModelSnapshot` + `DevApiModels` um `PlayerPlaylistId`/`PlayerPlaylistEntryId`/`PlayerPlaylistPosition`.

7. **MockVideoServer-Erweiterungen**
   - Voraussetzungen: Keine (parallel zu 1–6 möglich).
   - POST-Play-Endpunkte, `MockPlaylistEntry.ResumePositionSeconds`, Stream-Bedienung für Playlist-Medien-Ids, Playlist-Felder im Weiterschauen-Opt-in.

8. **Unit-/Vertragstests**
   - Voraussetzungen: Schritte 1–4.
   - `AppUpdateTests_PlaylistPlayback` (alle neuen Update-Pfade inkl. Guards, Identitätswache, Fehler-Rückfall, 204), Service-Mapping-Tests (`PlaylistService.StartPlaybackAsync`/`GetNextEntryAsync` — Muster der vorhandenen Service-Tests mit Test-`AuthSessionService`/HttpClient-Stub), Mapping-Test `HomeListService` (Playlist-Bezug), `PlaybackSourceService`-Override-Test.

9. **E2E-Tests**
   - Voraussetzungen: Schritte 1–7.
   - `PlaylistPlaybackTests` mit den Pflichtszenarien (siehe unten).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `PlaylistPlaybackStartRequested_DispatchesStartEffect` | `AppUpdateTests_PlaylistPlayback` | Kopf-Button-Nachricht → `StartPlaylistPlaybackEffect(PlaylistId, null)` |
| `PlaylistPlaybackStarted_SetsPlayerAndResolves` | `AppUpdateTests_PlaylistPlayback` | Erfolg → PlayerState (Playlist-Kontext, MediaId/Typ, IsResolving) + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` mit `StartPositionSeconds` |
| `PlaylistPlaybackStarted_Error_ShowsPlayerError` | `AppUpdateTests_PlaylistPlayback` | Fehler → `Player.Error` + `NavigateToPlayerEffect` |
| `PlaylistEntrySelected_Playable_StartsPlaylistPlayback` | `AppUpdateTests_PlaylistPlayback` | Tap auf `movie`/`tvshowepisode` → `StartPlaylistPlaybackEffect(PlaylistId, EntryId)` |
| `PlaylistEntrySelected_NonPlayable_OrInaccessible_OpensDetails` | `AppUpdateTests_PlaylistPlayback` | `tvshow`/`moviecollection`/nicht zugänglich → `NavigateToMediaDetailsEffect` (bzw. unverändert) |
| `ContinueWatchingSelected_WithPlaylist_StartsPlaylistPlayback` | `AppUpdateTests_PlaylistPlayback` | `PlaylistId`+`PlaylistEntryId` → `StartPlaylistPlaybackEffect`, kein `LoadUpcomingEpisodesEffect`/`ResolvePlaybackSourceEffect` direkt |
| `ContinueWatchingSelected_WithoutPlaylist_KeepsSeriesPath` | `AppUpdateTests_PlaylistPlayback` | Ohne Bezug → `StartPlayback` + `LoadUpcomingEpisodesEffect` (Regressionsnachweis) |
| `NextRequested_InPlaylist_LoadsNextEntry` / `PreviousRequested_InPlaylist_LoadsPreviousEntry` | `AppUpdateTests_PlaylistPlayback` | Guard + Effekte `LoadNextPlaylistEntryEffect`/`LoadPreviousPlaylistEntryEffect` |
| `NextRequested_WithoutPlaylist_NoEffect` | `AppUpdateTests_PlaylistPlayback` | Guard ohne Playlist-Kontext |
| `NavigationCompleted_SwitchesMediaAndResolves` | `AppUpdateTests_PlaylistPlayback` | `PlaylistNavigationItem` → neuer Player-Stand + `ResolvePlaybackSourceEffect` mit `StartPositionSeconds` |
| `NavigationCompleted_NoEntry_KeepsPlayback` | `AppUpdateTests_PlaylistPlayback` | `result == null` → `IsResolving = false`, sonst unverändert |
| `NavigationCompleted_StaleIdentity_Ignored` | `AppUpdateTests_PlaylistPlayback` | `PlaylistId`/`CurrentEntryId` passt nicht → `Unchanged` |
| `PlaybackEnded_InPlaylist_AdvancesPlaylist` | `AppUpdateTests_PlaylistPlayback` | `AutoResumeEnabled` + `IsPlaylistPlayback` → `AdvancePlaylistEntryEffect` statt Serienliste |
| `PlaybackEnded_InPlaylist_AutoResumeOff_NoAdvance` | `AppUpdateTests_PlaylistPlayback` | Schalter aus → kein Effekt |
| `PlaybackFailed_InPlaylist_AdvancesPlaylist` | `AppUpdateTests_PlaylistPlayback` | Fehler-Rückfall → `AdvancePlaylistEntryEffect` |
| `SourceResolveFailed_InPlaylist_AdvancesPlaylist` | `AppUpdateTests_PlaylistPlayback` | `PlaybackSourceResolvedMessage` ohne Quelle → `AdvancePlaylistEntryEffect` |
| `StartPlaybackAsync_MapsDto` | `PlaylistServiceTests_Playback` (neu) | `PlaylistPlaybackStart` → `PlaylistPlaybackStartInfo` (Typ-Normalisierung, Titel, Positionen) |
| `GetNextEntryAsync_NoContent_ReturnsNull` | `PlaylistServiceTests_Playback` | 204 → `null` |
| `MapContinueWatching_MapsPlaylistFields` | `HomeListService`-Test (Muster suchen/erweitern) | Playlist-Felder landen in `HomeMediaItem` |
| `ResolveAsync_Override_SkipsContinueWatchingLookup` | bestehende `PlaybackSourceService`-Tests erweitern | `resumePositionSecondsOverride` wird verwendet (auch 0) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Tests zu `PlaylistEntrySelectedMessage` (in `AppUpdateTests_Playlists`) | Verhalten abspielbarer Einträge ändert zu `StartPlaylistPlaybackEffect` — bestehende Erwartungen (Detail-Navigation für `movie`/`tvshowepisode`) sind zu prüfen/anzupassen; nicht abspielbare Fälle bleiben |
| `PlaybackSourceService`-Tests | Signatur-Erweiterung (optionaler Parameter — voraussichtlich keine Anpassung nötig, Compile-Check genügt) |
| `DevApiModelSnapshot`-Verträge | neue Felder — Snapshot-Mapping mitprüfen |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Detailseite → „Abspielen" startet Wiedergabe ab erstem abspielbarem Eintrag (erster Eintrag der Liste nicht abspielbar → Server überspringt) | `PlaylistPlaybackTests` | AK 1 | Vollständiger Benutzerfluss über Mock-`play`-Endpunkt + Player |
| Pflicht | Tap auf Eintrag startet Wiedergabe ab diesem Eintrag (`entryId` im play-Request nachweisbar) | `PlaylistPlaybackTests` | AK 1 | Benutzeraktion → Server-Aufruf → Player |
| Pflicht | „Weiter"/„Zurück" im Player navigieren über `play/next`/`play/previous` zum richtigen Eintrag | `PlaylistPlaybackTests` | AK 2 (manuelle Navigation) | Player-UI-Aktion + Medienwechsel |
| Pflicht | Auto-Fortsetzung am Titelende nutzt `play/advance` → Folge-Eintrag (kurzes Testvideo im Fortsetzungsfenster) | `PlaylistPlaybackTests` | AK 2 (Auto-Fortsetzung) | Zeit-/Ende-getriebener Fluss nur E2E prüfbar |
| Pflicht | Weiterschauen-Eintrag mit Playlist-Bezug → Wiedergabe via `play` + Folgevideo via `play/advance` (kein Serienkontext-Abruf `api/items/tvshow/{id}`) | `PlaylistPlaybackTests` | AK 3 | Kernanforderung „Playlist-Reihenfolge statt Serienreihenfolge" — Nachweis über angefragte Pfade |
| Pflicht | Fehler-Rückfall: Stream des aktuellen Eintrags 404 → automatischer Wechsel via `play/advance` | `PlaylistPlaybackTests` | AK 2 (Fehler-Rückfall) | Fehlerpfad über UI/Player |
| – | CW ohne Playlist-Bezug | bestehender `ContinueWatchingPlaybackTests` | AK 4 | bereits abgedeckt (kein neuer Test nötig) |

Welche bestehenden E2E-Tests müssen angepasst werden?

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PlaylistsTests.SelectingPlaylist_ShowsEntriesInServerOrder` | selektiert keinen Eintrag (nur Detail-Öffnung via Übersicht) — voraussichtlich keine Anpassung; bei etwaiger Eintragsauswahl in anderen Tests prüfen |

## Offene Punkte

Keine.
