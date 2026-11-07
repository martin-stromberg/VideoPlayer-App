# Bestandsaufnahme: Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-04-06 (Ortszeit, Windows-Host)
- Branch und Commit-ID: `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w-schritt-2-kontextmenue`, `8d9d7b65ad32884009fd150541bd344ec7bd7c05`
- Uncommittete Änderungen im getesteten Stand: nur Lifecycle-Artefakte unter `docs/features/task/…-kontextmenue/` (kein Produktivcode)
- Testumgebung und Runtime-/SDK-Versionen: Windows, `dotnet` SDK 10.0.401, xUnit (Testprojekt `net10.0`, E2E `net10.0-windows` + FlaUI/UIA3)
- Ermittelte Testsuiten und Quellen der Testbefehle: `*.sln`-Vorlage aus `/run-tests` → `dotnet test` je Testprojekt; E2E baut die App selbständig (`AppFixture`, Debug-Build `net10.0-windows10.0.19041.0`).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --nologo -v q` | Repo-Stamm | 0 | 1079 | 0 | 0 | Konsole (Dauer 11 s) |
| E2E | `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --nologo -v q` | Repo-Stamm | siehe Abschnitt Testlücken | – | – | – | Konsole |

### Nachgewiesene bestehende Testfehler

Keine Testfehler im Unit-Testlauf nachgewiesen.

### Testlücken und Ausführungsprobleme

- Der E2E-Testlauf (`VideoPlayer.E2E.Tests`) baut die Windows-App neu (bis ~10 Min) und steuert ein echtes Fenster — Ergebnis wird in `test-results/` ergänzt, sobald der Lauf abgeschlossen ist. Ergebnis nicht eingetroffen zum Zeitpunkt dieser Bestandsaufnahme → Ausgangszustand der E2E-Suite formal unbekannt; spätere Fehlschläge dürfen nicht als „preexisting" gelten, außer der ergänzte Lauf belegt sie.

## Testklassen (relevant für diese Anforderung)

### `AppUpdateTests_ContinueWatching`
- `ContinueWatchingItemSelected_Movie_StartsPlayback` — Direktstart Film.
- `ContinueWatchingItemSelected_Episode_StartsPlaybackWithShowLoad` — Direktstart Episode + `LoadUpcomingEpisodesEffect`.
- `ContinueWatchingItemSelected_ClrEpisodeType_StartsPlaybackWithShowLoad` — CLR-Typvariante.
- `ContinueWatchingItemSelected_EpisodeWithoutShowId_NoShowLoad` — kein Serienkontext.
- `ContinueWatchingItemSelected_NonPlayable_NavigatesToDetails` — `show`/`season`/`collection` → Detailseite.
- `ContinueWatchingItemSelected_NonSelectable_Unchanged` — Guards (Id 0, leerer Typ, Platzhalter).
- `UpcomingEpisodesLoaded_*` — Fortsetzungsliste.

### `VideoPlayerApiClientTests_Endpoints`
- `Endpoint_UsesExpectedMethodAndPath` (MemberData `EndpointCases`) — pinnt u. a. `POST /api/continue-watching/hide` und `POST /api/continue-watching/skip` (bereits vorhanden).

### `FavoritesServiceTests` (Muster für Mutationsdienst-Tests)
- `Toggle_SendsEntry_AndRefreshesCachedFavorites`, `Toggle_WithoutClient_ReturnsNull_NoRequests`, `RemoveFavorite_SendsRemove_AndRefreshesCache`, `RemoveFavorite_WithoutClient_ReturnsFalse_NoRequests` — Stub-Muster: `StubHttpMessageHandler`, `AuthSessionService` mit `HttpClient`-Factory, `CacheDatabase`/`BackupDatabase` im Temp-Layout.

### `HomePageContractTests` (Muster für Contract-Tests)
- `HomePage_ContinueWatchingCarousel_DispatchesOwnMessage`, `MediaCarousel_ItemsAreTappable_PostersViaCache`, `ShellAppEffectHandler_HandlesNewEffects_AndNotifiesListChanges` — Quelltext-Verträge der MAUI-Schicht.

### `ContinueWatchingEventIngressTests` / `NetworkSyncContractTests`
- `SyncContinueWatchingEffect`-Verdrahtung und `CachedListsChangedMessage`-Kette.

### `ContinueWatchingPlaybackTests` (E2E)
- `TapContinueWatchingMovie_StartsPlaybackAtResume_WithoutDetails`, `TapContinueWatchingEpisode_StartsPlaybackAndAdvancesToNextEpisode`, `TapContinueWatchingShowEntry_OpensDetails_NotPlayback` — Kurz-Tap-Flüsse; Long-Press darf diese nicht brechen.

## Hilfsmethoden

### `E2ETestBase`
- `ConnectToPairingRequiredAsync`, `PairAsync`, `LoginAsync`, `StartAppAsync` — Setup bis zur befüllten Startseite.

### `DevApiClient`
- `GetModelAsync`, `GetTreeAsync`, `InvokeAsync(automationId, itemIndex)`, `SetTextAsync`, `WaitForModelAsync` — Test-Treiber der Developer-API.

### `UiDriver`
- `FindByAutomationId`, `Exists`, `EnterText`, `ClickButton`, `OpenFlyoutItem`, `FindByName`, `DumpTree` — FlaUI-Hilfen.

### `StubHttpMessageHandler` (`VideoWebPlayer.Maui.Tests/Stubs`)
- `EnqueueJson`/`Enqueue`/`Requests` — hermetische HTTP-Antworten für Dienst-Tests.

### `RepositoryRoot`
- `Find()` — Repo-Wurzel für Quelltext-Verträge.
