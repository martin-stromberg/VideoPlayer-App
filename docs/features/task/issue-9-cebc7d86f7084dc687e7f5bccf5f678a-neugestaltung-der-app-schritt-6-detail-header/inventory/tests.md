# Bestandsaufnahme — Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22T23:54+02:00
- Branch und Commit-ID: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-6-detail-header` @ `c2ed35e0743056e157bd78dbc33981e888868380`
- Uncommittete Änderungen im getesteten Stand: nur neue Artefakte unter `docs/features/task/…-schritt-6-detail-header/` sowie die Fremddatei `docs/features/task/issue-9-customer-feedback.md` (untracked) — kein Produktivcode geändert
- Testumgebung und Runtime-/SDK-Versionen: .NET SDK 10.0.401, Windows, xUnit
- Ermittelte Testsuiten und Quellen der Testbefehle: `VideoWebPlayer.Maui.Tests` (einzige Testsuite der `VideoPlayer.App.sln`; Befehl gemäß AGENTS.md/DoD)
- Vorab-Build: `dotnet build VideoPlayer.App.sln -c Debug` — 0 Fehler (281 vorhandene Warnungen, überwiegend Legacy-Altbestand)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --logger "console;verbosity=normal"` | Repo-Root | 0 | 697 | 0 | 0 | [baseline-testrun.log](test-results/baseline-testrun.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 697 Tests bestanden im Ausgangslauf.

### Testlücken und Ausführungsprobleme

Keine bekannt. Die MAUI-Oberfläche (`VideoPlayer.Maui`) ist im Testprojekt
nicht instanziierbar (Zielframeworks) — Seiten werden über
Quell-Vertragstests abgesichert (Muster `HomePageContractTests`,
`SourcesContractTests`).

## Testklassen (für diese Anforderung relevant)

### `AppUpdateTests_MediaDetails` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_MediaDetails.cs`)
- `MediaDetailsRequested_StartsLoading_WithHints` — Ladeeffekt + Hints
- `MediaDetailsRequested_InvalidIdentity_ShowsError`
- `MediaDetailsRequested_SameTargetAlreadyLoaded_NoSecondRun`
- `MediaDetailsLoaded_SetsDetails` / `…_StaleResponse_IsDiscarded` / `…_Error_SetsErrorState`
- `SeasonSelected_SwitchesSelectedSeason` / `…_SameSeason_NoChange`
- `FavoriteToggle*` — Toggle-Effekt, Serverzustand, Fehler, Fremd-Medium

### `AppUpdateTests_HomeHero` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_HomeHero.cs`)
- Muster für den Bild-Ladezyklus: `LoadHeroBackgroundEffect`-Emission,
  `HeroBackgroundLoadedMessage` mit/ohne Bytes, veraltete Antwort,
  Sitzungs-Reset.

### `MediaDetailsServiceTests` (`VideoWebPlayer.Maui.Tests/MediaDetailsServiceTests.cs`)
- Online-Mapping aller Medientypen über `StubHttpMessageHandler` +
  `AuthSessionService` + echte SQLite-`CacheDatabase` (`SqliteDatabase`-Collection).
- Offline-Snapshot-Pfade (Manifest, `ShowDetailsCache`, Platzhalter).
- 404-Fallback Kollektion → Film.

### `VideoPlayerApiClientTests_Endpoints` (`VideoWebPlayer.Maui.Tests/VideoPlayerApiClientTests_Endpoints.cs`)
- `Endpoint_UsesExpectedMethodAndPath` (Theory, `EndpointCases`) — Methode,
  Pfad, API-Key je Endpunkt.
- `GetHeroBackgroundAsync_RequestsHeroEndpoint_AndReturnsBytes`,
  `GetHeroBackgroundAsync_SendsBearerToken` — Muster für Bild-Endpunkte.

### `HomePageContractTests` (`VideoWebPlayer.Maui.Tests/HomePageContractTests.cs`)
- Quell-Vertragstests für `HomePage.xaml`/`.xaml.cs`,
  `ShellAppEffectHandler`, `VideoPlayerApiClient`, `Colors.xaml`,
  `SettingsPage` — Muster für die anzulegenden `MediaDetailsPageContractTests`.

## Hilfsmethoden

### `StubHttpMessageHandler` (`VideoWebPlayer.Maui.Tests/Stubs/StubHttpMessageHandler.cs`)
- `Enqueue(HttpStatusCode, json)` / `EnqueueJson` / `EnqueueException` / `Enqueue(responder)` / `EnqueueAsyncResponder` — Antwort-Queue.
- `Requests` — aufgezeichnete Requests (Methode, Pfad, Authorization, ApiKey, Body).

### `ApiClientTestFactory` (gleiche Datei)
- `CreateClient(handler)` / `CreateClientWithDelays(handler, delays)` — Client mit `test-api-key` gegen `http://testserver/`.

### `RepositoryRoot` (`VideoWebPlayer.Maui.Tests/Stubs/RepositoryRoot.cs`)
- `Find()` — Repo-Wurzel für Quell-Vertragstests.
