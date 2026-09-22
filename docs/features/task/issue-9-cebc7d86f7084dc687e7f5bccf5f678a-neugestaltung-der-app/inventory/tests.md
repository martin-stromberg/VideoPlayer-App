# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22, 18:53 Uhr +0200 (lokale Systemzeit, CEST)
- Branch und Commit-ID: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`, Commit `945a075aebbbe5fe986e254d58f15b05655cebb8` („fix: Client-Gate-Token zusätzlich aus lokaler Token-Datei auflösen")
- Uncommittete Änderungen im getesteten Stand: nur untracked Dateien unter `docs/features/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/` (`requirement.md`, `todo.md`, `inventory/`). Keine Änderungen an Produktiv- oder Testcode; `git status --porcelain` zeigt ausschließlich `?? docs/features/task/…`.
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK `10.0.401` (einzige installierte SDK); installierte MAUI-Workloads: `android` 36.1.69, `ios`/`maccatalyst` 26.5.10318, `maui-windows` 10.0.20 (jeweils SDK 10.0.400).
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - Einzige ausführbare Testsuite im Repository: `VideoWebPlayer.Maui.Tests` (`net10.0`, xunit.v3 3.2.2, Microsoft.NET.Test.Sdk 18.8.1; referenziert `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`, `VideoPlayer.Storage`, `VideoWebPlayer.Client` — **nicht** `VideoPlayer.Maui`).
  - Testbefehl laut CI (`.github/workflows/pr-checks.yml`): `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --configuration Release` nach Release-Builds von Client und Tests; die CI baut zusätzlich `VideoPlayer.Maui` und `VideoWebPlayer.Maui` (Android + Windows) und prüft `dotnet format whitespace --verify-no-changes`.
  - Hinweis: Die `AGENTS.md`-Regeln nennen `dotnet test VideoWebPlayer.Tests/VideoWebPlayer.Tests.csproj` und `VideoPlayer.sln` — beide existieren in diesem Repository nicht; die Solution heißt `VideoPlayer.App.sln`, die Testsuite `VideoWebPlayer.Maui.Tests`.
  - UI-End-to-End-/Playwright-Tests sind im Repository nicht vorhanden (dokumentierte Einschränkung, siehe `SourcesContractTests`-Klassenkommentar); die MAUI-Schicht wird über quelltextbasierte Contract-Tests geprüft.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release` | Repo-Wurzel `D:\Repositories\softwareschmiede\cebc7d86-f708-4dc6-87e7-f5bccf5f678a` | 0 | 629 | 0 | 0 | [dotnet-test-release.log](test-results/dotnet-test-release.log) |
| 2 (Build) | `dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj --configuration Release --framework net10.0-windows10.0.19041.0` | Repo-Wurzel | 0 | – (Build) | 0 Fehler, 0 Warnungen | – | [dotnet-build-maui-windows-release.log](test-results/dotnet-build-maui-windows-release.log) |

Lauf 1 umfasst Build (Release) + Testausführung; Dauer der Testausführung ca. 11 s. Der Build warnte mit bekannten, nicht anforderungsrelevanten Warnungen (u. a. CS1570 in `KnownServersService.cs`, CS0108/CS8618 im Legacy-`VideoWebPlayer.Client`).

Lauf 2 ist ein ergänzender Build-Nachweis: die MAUI-App `VideoPlayer.Maui` baut lokal für Windows in Release fehler- und warnungsfrei (XAML-SourceGen aktiv). Der Android-/iOS-Build wurde nicht ausgeführt.

### Nachgewiesene bestehende Testfehler

Es wurden keine Testfehler nachgewiesen — alle 629 Tests des Ausgangslaufs sind erfolgreich.

### Testlücken und Ausführungsprobleme

- Die MAUI-App `VideoPlayer.Maui` kann vom Testprojekt nicht instanziiert werden (keine Projektreferenz möglich); ihre Verdrahtung ist nur über Contract-Tests auf Quelltextebene abgedeckt.
- Keine UI-E2E-Tests vorhanden (keine Playwright-/Appium-Suite im Repository).
- Der Android-/iOS-Teil der CI-Builds wurde lokal nicht ausgeführt; der Windows-Release-Build von `VideoPlayer.Maui` ist erfolgreich (Lauf 2). `VideoWebPlayer.Maui` (Altbestand) wurde nicht gebaut.

## Testklassen

### `VideoPlayerMauiContractTests` (`VideoWebPlayer.Maui.Tests/VideoPlayerMauiContractTests.cs`)

Contract-Tests der `AppShell`-XAML (gelesen über `RepositoryRoot` + `XDocument`):

- `AppShell_IsFlyoutNavigation` — `FlyoutBehavior="Flyout"`.
- `AppShell_ContainsAllMenuEntries` — **pinnt exakt sechs `FlyoutItem`s** in der Reihenfolge Startseite/Quellen/Favoriten/Downloads/Profil/Einstellungen samt `Route`-Attributen aus `AppRoutes.ToRoute`. Schlägt fehl, wenn die XAML-Struktur geändert wird (z. B. Entfernen des „Quellen"-Eintrags oder zusätzliche statische `FlyoutItem`s); zur Laufzeit hinzugefügte Einträge sieht der Test nicht.
- `AppShell_MenuEntries_BindToExistingPages` — jedes `ContentTemplate` verweist auf eine vorhandene `Pages/*.xaml` (außer `LoadingPage`).
- `NewApp_ProjectTargetsRequiredPlatforms` / `NewApp_DoesNotReferenceLegacyOrExternalMvuFrameworks` / `NewApp_HealthCheckWiredToApiClient` — Projekt-/Verdrahtungs-Verträge.

### `AppRoutesTests` (`VideoWebPlayer.Maui.Tests/AppRoutesTests.cs`)

- `MenuPages_ContainsAllSixMenuEntriesInOrder` — pinnt die sechs Menüseiten inkl. `AppPage.Sources`.
- `ToRoute_RoundTripsAllMenuPages` / `ToRoute_Loading_HasNoRoute` — Routenabbildung.
- `TryGetPage_ResolvesShellLocations` (Theory, u. a. `//sources/SourcesPage` → `AppPage.Sources`) / `TryGetPage_UnknownLocation_ReturnsFalse` — Auflösung von Shell-Orten; `sourceoverview` ist bewusst kein Treffer.

### `SourcesContractTests` (`VideoWebPlayer.Maui.Tests/SourcesContractTests.cs`)

Quelltext-Verträge des Quellen-Browsing:

- `SourcesPage_ShowsSources_WithIcons_AndOfflineHint` — `SourcesPage.xaml` bindet `Model.SourcesOverview.Sources`, `CachedSourceIconImage`, `IconPictureId`, Offline-Banner, Retry-Button.
- `SourcesPage_DispatchesOpenAndSelection` — `SourcesPageOpenedMessage` + `SourceSelectedMessage` im Code-Behind.
- `SourceOverviewPage_HasGenreBar_Search_AndIncrementalGrid` / `SourceOverviewPage_DispatchesBrowserMessages` — Struktur und Messages der `SourceOverviewPage`.
- `MediaDetailsPage_HasSeasonPicker_Episodes_Collection_AndFavorite` / `MediaDetailsPage_DispatchesDetailsMessages` — Detailseiten-Verträge.
- `Shell_RegistersSourceOverviewRoute_WithQueryParameters` — Route `sourceoverview` in `AppShell.xaml.cs`, `QueryProperty` `id`/`name` in der Seite.
- `EffectHandler_HandlesStep8Effects_AndRefreshesFavoriteCaches` — `ShellAppEffectHandler` behandelt `LoadSourcesEffect`, `NavigateToSourceOverviewEffect`, `LoadSourceGenresEffect`, `LoadSourceItemsEffect`, `LoadMediaDetailsEffect`, `ToggleFavoriteEffect`, dispatcht `CachedListsChangedMessage`.
- `MauiProgram_RegistersStep8ServicesAndPages` — DI-Registrierung von `SourceBrowserService`, `MediaDetailsService`, `FavoritesService`, `SourceOverviewPage`.

### `AppUpdateTests_Sources` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Sources.cs`)

21 Tests des Quellen-Übergangspfads (Hilfsmethoden `Connected()`, `Source(id, name)`, `Item(id)`):

- `SourcesPageOpened_Connected_StartsLoading` / `_Offline_NoRequestAndStateReset` / `_NotConnected_NoRequest` / `_AlreadyLoading_NoSecondRun` — Öffnen der Quellen-Seite inkl. Offline-Reset.
- `SourcesLoaded_SetsList_AndEmptyIsDistinguishable` / `SourcesLoaded_Error_SetsErrorState` — Übernahme der geladenen Liste.
- `SourceSelected_NavigatesAndStartsInitialLoad` — pinnt den Auswahl-Pfad: `NavigateToSourceOverviewEffect` + `LoadSourceGenresEffect` + `LoadSourceItemsEffect` (Seite 0) — der Pfad, den Menüeinträge wiederverwenden.
- `SourceBrowserOpened_SameSource_KeepsState` / `_Restore_LoadsGenresAndFirstPage` — Identitätsmeldung der geöffneten Seite.
- `SourceGenresLoaded_WrongSource_IsDiscarded` — Antworten fremder Quellen werden verworfen.
- `GenreSelected_ReloadsFromFirstPage_WithGenreFilter` / `SearchSubmitted_ReloadsFromFirstPage_WithSearchText` / `SearchSubmitted_SameText_NoReload` — Filter/Suche.
- `NextPageRequested_LoadsAppendedPage` / `NextPageRequested_NoMoreOrBusy_IsIgnored` / `ItemsLoaded_Append_ConcatenatesAndAdvancesPage` / `ItemsLoaded_StaleAppend_IsDiscarded` / `ItemsLoaded_AppendError_KeepsExistingItems` — Paging.
- `Retry_OnEmptyError_ReloadsFromScratch` / `Retry_WithItems_RetriesPendingNextPage` — Wiederholung.
- `MediaItemSelected_NavigatesToDetails` — Detail-Navigation aus dem Raster.

### `AppUpdateTests_Offline` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Offline.cs`)

- `SourcesPageOpened_Offline_NoLoadEffect` — offline kein `LoadSourcesEffect`.
- `Reconnection_FromOffline_RunsFullSyncAndResumesDownloads` — Wiederverbindungs-Kette (`CheckServerHealth` → `EnterConnected` mit `RunFullSyncEffect`, `ConnectServerEventsEffect`, `ResumeDownloadsEffect`).
- `HomeListsLoaded_DiscardedCaches_*` (3 Tests), `MediaItemSelected_OfflinePlayable_Navigates`, `DownloadRequested_Offline_*`, `ProfilePageOpened_Offline_NoLoadEffect`, `StorageRecovered_SetsNotice`, `AutoLoginCompleted_*` — Offline-/Wiederherstellungs-Verhalten.

### `AppUpdateTests_NetworkSync` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_NetworkSync.cs`)

- `AutoLoginSucceeded_OnMenuPage_ConnectsEventsAndSyncsWithoutNavigation` / `LoginSucceeded_OnLoginPage_NavigatesHomeAndSyncs` — pinnt die `EnterConnected`-Effektmenge (`ConnectServerEventsEffect`, `RunFullSyncEffect`, `ResumeDownloadsEffect`, ggf. `NavigateToPageEffect`).
- `ServerEventReceived_MapsToPartialSync` (Theory) — `NewVideosScanned` → `SyncMetadataEffect` u. a.
- `SynchronizationCompleted_ClearsSyncFlag` — pinnt exakt einen `LoadHomeListsEffect` als einzigen Effekt nach dem Voll-Sync.
- `NetworkLost_FromConnected_EntersOfflineAndDisconnectsEvents` / `NetworkRestored_FromOffline_RunsHealthCheck` / `ServerEventsClosed_*` / `SessionExpired_DisconnectsEvents` / `StartupRequested_IncludesMonitoringEffect`.

### `AppUpdateTests_Connection` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Connection.cs`)

Verbindungs-/Anmelde-Workflow: `OfflineDataCheckCompleted_*`, `AutoLoginCompleted_*` (u. a. `Succeeded_ConnectsAndNavigatesHome`), `LoginSubmitted_*`, `LoginCompleted_*` — Kontext der `EnterConnected`-Übergänge.

### `AppUpdateTests_FavoritesProfile` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_FavoritesProfile.cs`)

- `LogoutRequested_ResetsSessionAndNavigatesToLogin` / `LogoutRequested_Offline_AlsoEndsSession` / `LogoutRequested_WithoutSession_NoOp` — pinnt das Zurücksetzen des Sitzungszustands (inkl. `SourcesOverview`, `SourceBrowser`).

### `SourceBrowserServiceTests` (`VideoWebPlayer.Maui.Tests/SourceBrowserServiceTests.cs`)

- `LoadSources_MapsServerList_WithIcons` — `GET api/Sources`-Mapping inkl. `IconPictureId`.
- `LoadSources_WithoutClient_Throws` — `InvalidOperationException` ohne Sitzung.
- `LoadGenres_PrependsAllEntry_AndMapsServerGenres` / `LoadItems_*` (3 Tests) / `MapSummaryType_MapsLegacyTypes` (Theory) — Genres, Paging, Typ-Mapping.

### `ServerSyncServiceTests` (`VideoWebPlayer.Maui.Tests/ServerSyncServiceTests.cs`)

- `SynchronizeAsync_SendsPendingThenRefreshesCaches` / `SynchronizeAsync_WithoutClient_IsNoOp` / `SynchronizeAsync_PartialFailure_ReportsFalseButKeepsGoing` — Voll-Sync (schreibt u. a. `cached_lists["sources"]`).
- `FlushPendingProgress_*` (2 Tests), `SyncContinueWatching_RefreshesOnlyThatCache` — Teil-Synchronisationen gegen echtes SQLite (`SqliteDatabaseCollection`).

### `HomeListServiceTests` (`VideoWebPlayer.Maui.Tests/HomeListServiceTests.cs`)

- `LoadHomeLists_*` (u. a. `EmptyCache_ReturnsEmptyLists`, `CorruptJson_ReturnsEmptyList`, `CorruptListEntry_IsDiscardedAndReported`, `PersistsAcrossReads`), `LoadFavoriteItems_*`, `LoadHomeLists_MapsRecent` — Referenzmuster für Cache-Leser-Tests gegen echtes SQLite; der `"sources"`-Schlüssel wird derzeit **nicht** gelesen/getestet.

### `MvuHostTests` (`VideoWebPlayer.Maui.Tests/MvuHostTests.cs`)

- `DispatchAsync_UpdatesModelAndExecutesEffects` / `DispatchAsync_RaisesModelChangedAndPropertyChanged` / `DispatchAsync_NoOpMessage_StillExecutesNoEffects` — Host-Verhalten inkl. `ModelChanged`-Ereignis (Grundlage für ein Shell-Abonnement).

### Weitere Contract-Tests mit Shell-Bezug

- `ConnectionWorkflowContractTests.AppShell_RegistersLoginRoute_WithoutFlyoutItem` — `login`/`LoginPage` dürfen nicht in `AppShell.xaml` stehen.
- `ServerSetupContractTests.AppShell_RegistersServerSetupRoute_WithoutFlyoutItem` — analog `serversetup`/`ServerSetupPage`.
- `FavoritesProfileContractTests.AppShell_KeepsFavoritesAndProfileFlyoutItems` — `FavoritesPage`/`ProfilePage`/`favorites`/`profile` bleiben im XAML enthalten.
- `HomePageContractTests`, `PlayerContractTests`, `DownloadsContractTests`, `SettingsStorageContractTests`, `OfflineRecoveryContractTests`, `NetworkSyncContractTests`, `ClientGateTests` — übrige Bereichsverträge.

## Hilfsmethoden

### `RepositoryRoot` (`VideoWebPlayer.Maui.Tests/Stubs/RepositoryRoot.cs`)

- `Find` — findet die Repo-Wurzel anhand von `VideoPlayer.App.sln` (Basis aller Quelltext-Contract-Tests).

### `StubHttpMessageHandler` (`VideoWebPlayer.Maui.Tests/Stubs/StubHttpMessageHandler.cs`)

- HTTP-Stub für `VideoPlayerApiClient`-Tests (programmierbare Antworten je Route; Grundlage von `SourceBrowserServiceTests`, `AuthSessionServiceTests`, `ServerSyncServiceTests`).

### `InMemorySecretStore` / `InMemorySettingsStore` (`VideoWebPlayer.Maui.Tests/Stubs/`)

- In-Memory-Implementierungen von `ISecretStore`/`ISettingsStore` für Dienst-Tests.

### `SqliteDatabaseCollection` (`VideoWebPlayer.Maui.Tests/Stubs/SqliteDatabaseCollection.cs`)

- xunit-Collection für Tests gegen echte SQLite-Dateien (serielle Ausführung der DB-Tests).

### Test-Helfer in den Testklassen

- `AppUpdateTests_Sources`: `Connected()` (verbundenes Model), `Source(id, name)`, `Item(id)` — direkt für neue Menü-Quellen-Tests wiederverwendbar.
- `AppUpdateTests_NetworkSync`/`_Offline`/`_Connection`: `ConnectedModel()`/`OfflineModel()`/`Server`-Konstanten.
- `ServerSyncServiceTests`/`HomeListServiceTests`: SQLite-Testdatenbank-Setup mit `AppStorageLayout` in Temp-Verzeichnissen.
