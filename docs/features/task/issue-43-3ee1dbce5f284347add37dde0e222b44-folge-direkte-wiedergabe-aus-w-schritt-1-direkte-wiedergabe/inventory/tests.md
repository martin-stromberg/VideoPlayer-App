# Tests — Direkte Wiedergabe aus der Weiterschauen-Liste

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-11, ~14:30–14:35 (lokale Zeit, Windows)
- Branch und Commit-ID: `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w-schritt-1-direkte-wiedergabe`, `ccb8c820f54b0e53270bef08c607f4c4db1f4343`
- Uncommittete Änderungen im getesteten Stand: nur Lifecycle-Artefakte unter `docs/features/…` (kein Produktivcode)
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401, .NET 10.0.12; Debug-Build der Solution
- Ermittelte Testsuiten und Quellen der Testbefehle: `VideoPlayer.App.sln` mit `VideoWebPlayer.Maui.Tests` (xUnit v3, net10.0) und `VideoPlayer.E2E.Tests` (xUnit v3 + FlaUI, net10.0-windows; baut die Windows-App selbst via `AppFixture`)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Build | `dotnet build VideoPlayer.App.sln --nologo -v m` | Repo-Wurzel | 0 | – | – | – | 0 Fehler, 297 Warnungen (Altbestand) |
| Unit/Contract | `dotnet test VideoWebPlayer.Maui.Tests --no-build --logger "console;verbosity=normal"` | Repo-Wurzel | 0 | 1056 | 0 | 0 | [baseline-unit-tests.log](test-results/baseline-unit-tests.log) |
| E2E | `dotnet test VideoPlayer.E2E.Tests --no-build --logger "console;verbosity=normal"` | Repo-Wurzel | 0 | 19 | 0 | 0 | [baseline-e2e-tests.log](test-results/baseline-e2e-tests.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 1075 Tests des Ausgangslaufs bestanden.

### Testlücken und Ausführungsprobleme

Keine. Beide Suiten liefen vollständig durch.

## Testklassen (bestehend, für die Anforderung relevant)

### `AppUpdateTests_Player` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Player.cs`)
- `PlayRequested_ValidMovie_SetsPlayerStateAndEffects` — Player-State + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`
- `PlayRequested_Episode_BuildsUpcomingEpisodes` — `NextEpisodeQueue` aus `model.MediaDetails`
- `PlayRequested_LastEpisode_HasNoUpcoming` — leere Fortsetzungsliste
- `PlayRequested_InvalidIdentity_Unchanged` — Guard nicht abspielbarer Typen
- `PlaybackSourceResolved_*`, `PlaybackProgress_*`, `PlaybackEnded_*` — Auflösung, Auto-Fortsetzung, Fehler-Rückfall
- Hilfen: `Connected()`, `ShowDetails()` (2 Staffeln à 2 Episoden, Ids 31–34, ShowId 11), `PlayingState()`, `WithPlayer()`

### `AppUpdateTests_HomeLists` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_HomeLists.cs`)
- `MediaItemSelected_NavigatesToDetails` — `NavigateToMediaDetailsEffect` mit Serien-Hints
- `MediaItemSelected_WithoutIdentifiableMedia_NoNavigation` (Theory) — Guard
- `MediaItemSelected_Placeholder_NoNavigation` — `IsSelectable`-Guard

### `HomePageContractTests` (`VideoWebPlayer.Maui.Tests/HomePageContractTests.cs`)
- `HomePage_ShowsThreeHorizontalMediaLists` — drei `MediaCarousel` + Bindings
- `HomePage_ContinueWatching_UsesLandscapeCards` — Weiterschauen-Karten 256×144
- `HomePage_SelectionDispatchesMediaItemSelected` — **pinnt** `MediaItemSelectedMessage` + `DispatchAsync` im Code-Behind (muss nach Umstellung auf getrennte Handler weiter bestehen bzw. angepasst werden)
- `MediaCarousel_ItemsAreTappable_PostersViaCache` — `TapGestureRecognizer`, `CachedPosterImage`, `{Binding Progress}`
- `ShellAppEffectHandler_HandlesNewEffects_AndNotifiesListChanges` — Effekt-Verdrahtung

### `PlayerContractTests` (`VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`)
- `MediaDetailsPage_HasPlayButton` — „Abspielen"-Button der Detailseite

### `MediaDetailsServiceTests` / `AutoDownloadServiceTests` / `ShowDetailsCache`-nahe Tests
- `AutoDownloadServiceTests.Run_EpisodeCandidate_EnqueuesUpcomingEpisodes` / `_CrossesIntoNextSeason` — `GetTvShowAsync` + staffelübergreifende Ordnung; Test-Setup mit `StubHttpMessageHandler`, `AuthSessionService`, `CacheDatabase`, `ShowWithEpisodes`-Hilfe
- `OfflineContinueWatchingServiceTests` — Offline-Weiterschauen mit `ShowId`/Platzhaltern

### `EnumCoverageTests` (`VideoWebPlayer.Maui.Tests/EnumCoverageTests.cs`)
- Erzwingt Testabdeckung je Enum-Wert (betroffene Enums der Anforderung: keine — `MediaTypeNames` ist String-Konvention, kein Enum)

### `DevApiContractTests` / `DevApiRouterTests*` / `DevApiModelSnapshotTests` (`VideoWebPlayer.Maui.Tests`)
- Verträge der Developer-API (4 Endpunkte, `#if`, Loopback, Modell-Snapshot-Mapping in `DevApiRouterTests_MapModel.cs` — neue Snapshot-Felder müssen dort ggf. ergänzt werden)

### `PlaybackTests` (`VideoPlayer.E2E.Tests/Tests/PlaybackTests.cs`)
- `PlayFromRecent_StartsPlaybackAtResumePosition` — „Neu im Programm"-Tap → Detailseite → „Abspielen" → Fortsetzungsposition
- `PlayWithFailingStream_ShowsErrorInsteadOfCrash` — Stream-404 → `PlayerError` im Modell

## Hilfsmethoden

### `StubHttpMessageHandler` (`VideoWebPlayer.Maui.Tests/Stubs/StubHttpMessageHandler.cs`)
- `EnqueueJson(status, obj)` / `Enqueue(...)` — HTTP-Stub für `AuthSessionService`-basierte Clients; `Requests` protokolliert Anfragen

### `InMemorySettingsStore` / `InMemorySecretStore` / `SqliteDatabase`-Collection (`VideoWebPlayer.Maui.Tests/Stubs/`)
- In-Memory-Ablagen bzw. echte SQLite-Testdatenbank (`[Collection("SqliteDatabase")]`, Muster `AutoDownloadServiceTests`)

### `MockVideoServer` (`VideoPlayer.E2E.Tests/Support/MockVideoServer.cs`)
- Opt-in-Wiedergabefluss (`PlaybackMovieId` u. a.); `IsListEndpoint`-Liste; `PlaybackMediaEntryJson` (`$type`-Diskriminator)

### `DevApiClient` / `UiDriver` (`VideoPlayer.E2E.Tests/Support/`)
- `WaitForModelAsync` (Modell-Prädikat mit Timeout), `GetTreeAsync` + `FindByAutomationId`/`Items`, `InvokeAsync` (itemIndex), `FindByAutomationId`/`ClickButton` (FlaUI)
