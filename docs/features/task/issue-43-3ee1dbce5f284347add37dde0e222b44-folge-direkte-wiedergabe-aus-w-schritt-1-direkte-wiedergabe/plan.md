# Umsetzungsplan: Direkte Wiedergabe aus der Weiterschauen-Liste

## Übersicht

Ein Tap auf einen abspielbaren Eintrag der Weiterschauen-Liste (`movie`/`episode`) startet direkt die Wiedergabe über den bestehenden `PlayRequested`-Pfad — ohne Detailansicht. Die Herkunftsliste wird über einen eigenen `ItemTapped`-Handler des Weiterschauen-Karussells und eine neue MVU-Nachricht unterscheidbar; alle anderen Listen bleiben unverändert beim Detail-Navigationspfad. Für den Erhalt der Folgevideo-Funktion wird der Serienkontext per neuem Datenabruf-Effekt (`api/items/tvshow/{showId}`, Offline-Fallback `ShowDetailsCache`) ohne Detailseite geladen und über eine Ergebnisnachricht in `PlayerState.UpcomingEpisodes` nachgetragen.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Herkunftslisten-Unterscheidung | Eigener `ItemTapped`-Handler `OnContinueWatchingItemTapped` am Weiterschauen-Karussell in `HomePage.xaml`, der `ContinueWatchingItemSelectedMessage` dispatcht — `MediaCarousel` bleibt unverändert, kein neues Control-Attribut | Die Unterscheidung ist nur am Dispatch-Punkt nötig; alle übrigen Auswahlpfade bleiben wortwörtlich unverändert (Abgrenzung „andere Listen" strukturell, nicht per Flag). Ein neuer Message-Typ vermeidet ein Herkunfts-Flag an `MediaItemSelectedMessage`, das alle anderen Dispatch-Stellen still mitschleppen müssten. |
| Wiedergabestart | `UpdateContinueWatchingItemSelected` verzweigt nach `MediaTypeNames.Normalize`: `movie`/`tvshowepisode` → gemeinsamer Übergang mit `UpdatePlayRequested` (neuer privater Helfer `StartPlayback`), sonst `NavigateToMediaDetailsEffect` | Ein einziger Wiedergabe-Startpfad — Fortsetzungsposition, Guard, Effekt-Reihenfolge und `UpcomingEpisodes`-Vorbefüllung aus evtl. vorhandenem Detailkontext bleiben identisch. |
| Serienkontext ohne Detailseite | Neuer Service `UpcomingEpisodesService` (VideoPlayer.Mvu) + `LoadUpcomingEpisodesEffect` + `UpcomingEpisodesLoadedMessage`; async Nachladen in `PlayerState.UpcomingEpisodes` mit Identitäts-Guard | `Update` ist rein und kann nichts laden. Die Ergebnisnachricht mit `MediaId`-Guard (Muster `UpdatePlaybackSourceResolved`) macht den Race mit Quellauflösung/Fortsetzung deterministisch; der Service bündelt Online (`GetTvShowAsync` + `ShowDetailsCache.WriteAsync`) und Offline (`ShowDetailsCache.ReadAsync`) in einer testbaren Klasse. |
| E2E-Messbarkeit | `DevApiModelSnapshot` um `PlayerMediaId` und `PlayerUpcomingEpisodeCount` erweitern; `MockVideoServer` um Episoden-/Show-Fixture und Request-Protokoll (`RequestedPaths`) ergänzen | Folgevideo-Beleg und „Detailseite nicht geöffnet" sind nur über Modell-Snapshot bzw. Server-Zugriffsprotokoll objektiv prüfbar — additive Erweiterung, keine Vertragsbrüche. |

## Programmabläufe

### Weiterschauen-Direktstart (abspielbarer Eintrag)

1. `MediaCarousel.OnItemTapped` feuert `ItemTapped` → `HomePage.OnContinueWatchingItemTapped` dispatcht `ContinueWatchingItemSelectedMessage(item)`.
2. `AppUpdate.UpdateContinueWatchingItemSelected`: Guard (`Id > 0`, `MediaType` belegt, `IsSelectable` — identisch zu `UpdateMediaItemSelected`), `MediaTypeNames.Normalize` → `movie`/`tvshowepisode`.
3. `StartPlayback`: `PlayerState` belegen (`MediaType`, `MediaId`, `Title`, `IsResolving`, `Attempted`, `UpcomingEpisodes = NextEpisodeQueue(model, mediaId)` — evtl. vorhandener Detailseiten-Kontext wird wie bei `PlayRequested` genutzt); Effekte `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`.
4. Bei `tvshowepisode` und `item.ShowId > 0`: zusätzlich `LoadUpcomingEpisodesEffect(MediaId, ShowId)`.
5. `ShellAppEffectHandler.LoadUpcomingEpisodesAsync`: `UpcomingEpisodesService.LoadAsync` → dispatcht `UpcomingEpisodesLoadedMessage(MediaId, Episodes)` (Fehler isoliert, leere Liste).
6. `AppUpdate.UpdateUpcomingEpisodesLoaded`: Guard `player.IsActive && player.MediaId == message.MediaId && player.MediaType == "tvshowepisode"` → `UpcomingEpisodes` übernehmen.
7. `PlaybackSourceService.ResolveAsync` liefert Quelle + `ResumePosition` (unverändert); `PlayerPage` seekt verzögert (unverändert). `AdvanceToNextEpisode`/Auto-Fortsetzung arbeiten mit der nachgeladenen Liste unverändert.

Beteiligte Klassen/Komponenten: `MediaCarousel`, `HomePage`, `AppUpdate`, `ShellAppEffectHandler`, `UpcomingEpisodesService`, `PlaybackSourceService`, `PlayerPage`.

### Weiterschauen-Fallback (nicht abspielbarer Eintrag / Platzhalter)

1. Wie oben bis Schritt 2; `Normalize` liefert `tvshow`/`tvshowseason`/`moviecollection` bzw. `IsSelectable == false`.
2. Übergang wie `UpdateMediaItemSelected`: `NavigateToMediaDetailsEffect(item.MediaType, item.Id, item.Title, item.ShowId, item.SeasonId, item.EpisodeId)` — unveränderte Detail-Navigation.

### `UpcomingEpisodesService.LoadAsync`

1. `authSession.Client is not null` → `GetTvShowAsync(showId)`; bei Erfolg `ShowDetailsCache.WriteAsync` (Offline-Serienkontext auffrischen).
2. Ohne Client oder nach Client-Fehlschlag → `ShowDetailsCache.ReadAsync(showId)` (Offline-/Fehler-Fallback; `null`-tolerant).
3. Episoden staffelübergreifend ordnen (`Seasons.OrderBy(Number).SelectMany(Episodes.OrderBy(Number))`, Muster `AutoDownloadService`/`NextEpisodeQueue`), Index der gestarteten `mediaId` suchen, alle Folge-Episoden als `PlayerEpisodeRef`-Liste liefern. Episode nicht enthalten oder kein Kontext → leere Liste.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `ContinueWatchingItemSelectedMessage` | `AppMessage`-Record | Auswahl eines Weiterschauen-Eintrags (`HomeMediaItem Item`) |
| `UpcomingEpisodesLoadedMessage` | `AppMessage`-Record | Ergebnis der Serienkontext-Ladung (`long MediaId`, `IReadOnlyList<PlayerEpisodeRef> Episodes`) |
| `LoadUpcomingEpisodesEffect` | `AppEffect`-Record | Folge-Episoden per Datenabruf laden (`long MediaId`, `long ShowId`) |
| `UpcomingEpisodesService` | Klasse (VideoPlayer.Mvu) | Serienkontext-Auflösung ohne Detailseite: online `GetTvShowAsync` + `ShowDetailsCache.WriteAsync`, offline `ShowDetailsCache.ReadAsync`; liefert geordnete Folge-Episoden |
| `ContinueWatchingPlaybackTests` | E2E-Testklasse | Benutzerfluss-Nachweis der Direktwiedergabe |

## Änderungen an bestehenden Klassen

### `AppMessage` / `AppEffect` (`VideoPlayer.Mvu`)

- **Neue Records:** `ContinueWatchingItemSelectedMessage(HomeMediaItem Item)`, `UpcomingEpisodesLoadedMessage(long MediaId, IReadOnlyList<PlayerEpisodeRef> Episodes)` in `AppMessage.cs`; `LoadUpcomingEpisodesEffect(long MediaId, long ShowId)` in `AppEffect.cs` — jeweils mit deutscher XML-Doku (CS1591 als Fehler).

### `AppUpdate` (`VideoPlayer.Mvu`)

- **Neue Methoden:**
  - `UpdateContinueWatchingItemSelected(model, message)` — Guard wie `UpdateMediaItemSelected`; `movie`/`tvshowepisode` → `StartPlayback` + ggf. `LoadUpcomingEpisodesEffect`; sonst `NavigateToMediaDetailsEffect` mit Serien-Hints.
  - `UpdateUpcomingEpisodesLoaded(model, message)` — Identitäts-Guard (`IsActive`, `MediaId` gleich, `MediaType == "tvshowepisode"`) → `player with { UpcomingEpisodes = message.Episodes }`.
- **Geänderte Methoden:** `Update` (Switch) — zwei neue Cases für die neuen Nachrichten; `UpdatePlayRequested` — Rumpf nutzt den neuen gemeinsamen Helfer `StartPlayback(model, mediaType, mediaId, title)` (Player-State + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`), Verhalten unverändert.

### `HomePage` (`VideoPlayer.Maui/Pages/HomePage.xaml(.cs)`)

- **XAML:** Weiterschauen-`MediaCarousel` erhält `AutomationId="ContinueWatchingCarousel"` und `ItemTapped="OnContinueWatchingItemTapped"`; Favoriten/„Neu im Programm" bleiben bei `OnMediaItemTapped`.
- **Code-Behind:** neuer Handler `OnContinueWatchingItemTapped(object?, HomeMediaItem)` → `mvuHost.DispatchAsync(new ContinueWatchingItemSelectedMessage(item))`; `OnMediaItemTapped` unverändert.

### `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`)

- **Neue Case:** `LoadUpcomingEpisodesEffect` → `LoadUpcomingEpisodesAsync(effect, cancellationToken)`.
- **Neue Methode:** `LoadUpcomingEpisodesAsync` — `UpcomingEpisodesService.LoadAsync` aufrufen (Fehler isoliert → leere Liste), `UpcomingEpisodesLoadedMessage` via `DispatchMessageAsync` zurückspielen.

### `MauiProgram` (`VideoPlayer.Maui/MauiProgram.cs`)

- `builder.Services.AddSingleton<UpcomingEpisodesService>();` — bei den übrigen Mvu-Services.

### `DevApiModelSnapshot` (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`)

- **Neue Eigenschaften:** `PlayerMediaId` (`long`), `PlayerUpcomingEpisodeCount` (`int`) — in `FromModel` aus `model.Player` befüllen (`#if DEBUG || DEV_API` bleibt).

### `DevApiModel` (`VideoPlayer.E2E.Tests/Support/DevApiModels.cs`)

- **Neue Eigenschaften:** `playerMediaId`, `playerUpcomingEpisodeCount` (JsonPropertyName, Spiegel des Snapshots).

### `MockVideoServer` (`VideoPlayer.E2E.Tests/Support/MockVideoServer.cs`)

- **Neue Opt-in-Fixture:** `PlaybackShowId`, `PlaybackShowTitle`, `PlaybackEpisodeId`, `PlaybackEpisodeTitle`, `PlaybackNextEpisodeId`, `PlaybackNextEpisodeTitle`, `PlaybackEpisodeResumePositionSeconds`, `PlaybackShowEntryId` (Weiterschauen-Eintrag `show`).
- **Neue Endpunkte:** `GET api/items/tvshow/{id}` (Serie mit eingebetteten Staffeln/Episoden: laufende + Folge-Episode), `GET api/items/tvshowepisode/{id}` (Basisdetails), `GET api/items/tvshowepisode/{id}/stream` (Range-fähige Videodatei wie Movie-Stream).
- **`ListOverride` erweitern:** `api/continue-watching` liefert zusätzlich den Episoden-Eintrag (`mediaType: "episode"`, `entry` mit `$type: "episode"` inkl. eingebettetem `season`/`show`-Graph → `ShowId`-Hint) bzw. den Show-Eintrag (`mediaType: "show"`), je nach Opt-in.
- **Neue Diagnose:** `RequestedPaths` (`List<string>`) — protokollierte Anfragepfade für den Nachweis „Detailseite nicht geladen" vs. „Serienkontext geladen".

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `ContinueWatchingItemSelectedMessage.Item` | `Id > 0`, `MediaType` belegt, `IsSelectable` | Übergang unverändert (`AppTransition.Unchanged`) — analog `UpdateMediaItemSelected` |
| `LoadUpcomingEpisodesEffect.ShowId` | `> 0` | Effekt wird gar nicht erst ausgelöst (Guard im Update) |
| `UpcomingEpisodesLoadedMessage` | Player aktiv, `MediaId`/`MediaType` stimmen | Antwort verwerfen — analog `UpdatePlaybackSourceResolved` |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Andere Listen:** Favoriten, „Neu im Programm", Favoriten-Seite, Quellen-Übersicht, Episodenliste und Kollektionen dispatcht weiterhin `MediaItemSelectedMessage`/`SourceMediaItemSelectedMessage` — unverändert, strukturell durch getrennten Handler garantiert.
- **Race Quellauflösung ↔ Serienkontext:** Trifft `UpcomingEpisodesLoadedMessage` nach einem Fehler-Rückfall/`AdvanceToNextEpisode` ein, verwirft der `MediaId`-Guard die Antwort — konsistent zum bestehenden Verwerfungs-Muster.
- **Offline-Direktstart:** Offline-Weiterschauen-Einträge (Manifest, `IsSelectable`) starten ebenfalls direkt — `PlaybackSourceService` liefert im Offline-Zustand nur lokale Dateien (`allowServerStream`); der Serienkontext kommt aus `ShowDetailsCache`. Platzhalter bleiben unantippbar.
- **Contract-Test `HomePage_SelectionDispatchesMediaItemSelected`:** bleibt gültig (`MediaItemSelectedMessage` + `DispatchAsync` bleiben im Code-Behind), wird um den neuen Dispatch-Pfad ergänzt.
- **`EnumCoverageTests`:** keine neuen Enums — `MediaTypeNames` ist String-Konvention.
- **`DevApiModelSnapshot`-Erweiterung:** rein additiv (camelCase-Felder); kein bestehender Vertragstest pinnt die Feldliste.

## Umsetzungsreihenfolge

1. **MVU-Nachrichten und -Effekt anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: `ContinueWatchingItemSelectedMessage`, `UpcomingEpisodesLoadedMessage` in `AppMessage.cs`; `LoadUpcomingEpisodesEffect` in `AppEffect.cs` — mit deutscher XML-Doku.

2. **`UpcomingEpisodesService` anlegen**
   - Voraussetzungen: `VideoPlayerApiClient.GetTvShowAsync`, `ShowDetailsCache` (vorhanden); Schritt 1 (`PlayerEpisodeRef` bereits vorhanden).
   - Beschreibung: Neue Klasse mit `AuthSessionService` + `CacheDatabase` im Konstruktor; `LoadAsync(long mediaId, long showId, CancellationToken)` nach dem oben beschriebenen Programmablauf; DI-Registrierung in `MauiProgram.cs`.

3. **`AppUpdate` erweitern**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `StartPlayback`-Helfer aus `UpdatePlayRequested` extrahieren; `UpdateContinueWatchingItemSelected` und `UpdateUpcomingEpisodesLoaded` implementieren; zwei neue Switch-Cases.

4. **`ShellAppEffectHandler` erweitern**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: `LoadUpcomingEpisodesEffect`-Case + `LoadUpcomingEpisodesAsync` (Fehler isoliert, `DispatchMessageAsync`).

5. **`HomePage` verdrahten**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `AutomationId="ContinueWatchingCarousel"` + `ItemTapped="OnContinueWatchingItemTapped"` im XAML; neuer Handler im Code-Behind.

6. **`DevApiModelSnapshot` und E2E-`DevApiModel` erweitern**
   - Voraussetzungen: Keine.
   - Beschreibung: `PlayerMediaId` + `PlayerUpcomingEpisodeCount` in beiden Klassen ergänzen.

7. **`MockVideoServer` erweitern**
   - Voraussetzungen: Keine.
   - Beschreibung: Episoden-/Show-Opt-in-Fixture, `api/items/tvshow/{id}`, `api/items/tvshowepisode/{id}[/stream]`, `ListOverride`-Einträge, `RequestedPaths`-Protokoll.

8. **Unit- und Contract-Tests schreiben**
   - Voraussetzungen: Schritte 1–5.
   - Beschreibung: Neue Testklasse `AppUpdateTests_ContinueWatching` (Übergänge); Erweiterung `HomePageContractTests` (getrennte Handler/AutomationId) und `DevApiModelSnapshot`-bzw. Service-Tests (`UpcomingEpisodesServiceTests` mit `StubHttpMessageHandler` + `SqliteDatabase`-Collection nach Muster `AutoDownloadServiceTests`).

9. **E2E-Tests schreiben**
   - Voraussetzungen: Schritte 5–7.
   - Beschreibung: Neue Testklasse `ContinueWatchingPlaybackTests` mit den Szenarien aus dem E2E-Abschnitt.

10. **Vollständiger Build + alle Testläufe**
    - Voraussetzungen: Schritte 1–9.
    - Beschreibung: `dotnet build VideoPlayer.App.sln`, `dotnet test` beider Suiten.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `ContinueWatchingItemSelected_Movie_StartsPlayback` | `AppUpdateTests_ContinueWatching` | `movie`-Eintrag → `PlayerState` + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`, kein `NavigateToMediaDetailsEffect` |
| `ContinueWatchingItemSelected_Episode_StartsPlaybackWithShowLoad` | `AppUpdateTests_ContinueWatching` | `episode`-Eintrag mit `ShowId` → Wiedergabe-Übergang + zusätzlich `LoadUpcomingEpisodesEffect(MediaId, ShowId)` |
| `ContinueWatchingItemSelected_EpisodeWithoutShowId_NoShowLoad` | `AppUpdateTests_ContinueWatching` | Episode ohne `ShowId` → Wiedergabe ohne `LoadUpcomingEpisodesEffect` |
| `ContinueWatchingItemSelected_NonPlayable_NavigatesToDetails` (Theory: `show`, `season`, `collection`) | `AppUpdateTests_ContinueWatching` | Fallback `NavigateToMediaDetailsEffect` inkl. Serien-Hints, kein `NavigateToPlayerEffect` |
| `ContinueWatchingItemSelected_PlaceholderOrInvalid_Unchanged` | `AppUpdateTests_ContinueWatching` | `IsPlaceholder`/`Id <= 0`/leerer `MediaType` → `AppTransition.Unchanged` |
| `UpcomingEpisodesLoaded_MatchingPlayer_SetsQueue` | `AppUpdateTests_ContinueWatching` | Nachricht mit passender Identität → `Player.UpcomingEpisodes` befüllt |
| `UpcomingEpisodesLoaded_StaleOrOtherMedia_Discarded` | `AppUpdateTests_ContinueWatching` | Guard: inaktiver Player, andere `MediaId`, `movie` → unverändert |
| `LoadAsync_Online_ReturnsUpcomingEpisodes` | `UpcomingEpisodesServiceTests` | `GetTvShowAsync`-Antwort → geordnete Folge-Episoden ab `mediaId` (staffelübergreifend) + `ShowDetailsCache`-Eintrag geschrieben |
| `LoadAsync_Offline_ReadsShowCache` | `UpcomingEpisodesServiceTests` | ohne `Client` → Folge-Episoden aus `ShowDetailsCache` |
| `LoadAsync_EpisodeNotFoundOrNoContext_ReturnsEmpty` | `UpcomingEpisodesServiceTests` | unbekannte `mediaId`/fehlender Cache → leere Liste |
| `HomePage_ContinueWatchingCarousel_DispatchesOwnMessage` | `HomePageContractTests` (Erweiterung) | XAML: `AutomationId="ContinueWatchingCarousel"` + `ItemTapped="OnContinueWatchingItemTapped"`; Code-Behind dispatcht `ContinueWatchingItemSelectedMessage`; andere Karussells bleiben bei `OnMediaItemTapped`/`MediaItemSelectedMessage` |
| `ShellAppEffectHandler_HandlesLoadUpcomingEpisodesEffect` | `HomePageContractTests` o. neue Contract-Testklasse (Erweiterung) | Quelltext-Vertrag: Case `LoadUpcomingEpisodesEffect` + `UpcomingEpisodesLoadedMessage` + `UpcomingEpisodesService` |
| `DevApiModelSnapshot_ContainsPlayerUpcomingFields` | `DevApiModelSnapshotTests` (Erweiterung) | Snapshot trägt `playerMediaId`/`playerUpcomingEpisodeCount` |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `HomePageContractTests.HomePage_SelectionDispatchesMediaItemSelected` | Bleibt bestehen (weiter `MediaItemSelectedMessage` im Code-Behind) — ggf. um Assertion erweitern, dass beide Dispatch-Pfade existieren; nicht brechend |
| `AppUpdateTests_Player.PlayRequested_*` | Unverändert — `UpdatePlayRequested` delegiert nur an `StartPlayback`, Signaturen/Effekte bleiben |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Tap auf Film in Weiterschauen → Wiedergabe läuft ab Fortsetzungsposition, ohne Detailseite (`PlayerIsActive`/`IsPlaying`/`PositionSeconds >= Resume`; `RequestedPaths` ohne `api/items/movie/{id}`-Detailaufruf, mit `/stream`) | `ContinueWatchingPlaybackTests` | AK 1 + AK 2 | Gesamter Benutzerfluss UI→MVU→Player |
| Pflicht | Tap auf Episode in Weiterschauen → `PlayerUpcomingEpisodeCount > 0`, danach Wechsel zur Folge-Episode (`PlayerMediaId` = nächste Episode, `IsPlaying`); `api/items/tvshow/{id}` wurde abgerufen, `api/items/tvshowepisode/{id}`-Detailaufruf nicht | `ContinueWatchingPlaybackTests` | AK 5 (Serienkontext ohne Detailseite; Folgevideo/Auto-Fortsetzung) | Nachweis, dass `UpcomingEpisodes` ohne Detailansicht befüllt und verbraucht wird — 20-s-Testvideo + Auto-Resume-Fenster 300 s machen den Fortsetzungswechsel E2E beobachtbar |
| Pflicht | Tap auf `show`-Eintrag in Weiterschauen → Detailseite öffnet (`HeaderSection` im Tree), `PlayerIsActive` bleibt `false` | `ContinueWatchingPlaybackTests` | AK 4 | Fallback-Verhalten über die echte UI |
| Pflicht | Tap auf „Neu im Programm"-Eintrag → Detailseite (bestehendes Verhalten, unverändert) | bereits vorhanden: `PlaybackTests.PlayFromRecent_StartsPlaybackAtResumePosition` (Detailseite → `PlayButton`) | AK 3 | Bestehender E2E-Nachweis bleibt grün; kein neuer Test nötig |

Welche bestehenden E2E-Tests müssen angepasst werden?

Keine — `PlaybackTests` bleiben unverändert gültig und decken AK 3 mit ab.

## Offene Punkte

Keine.
