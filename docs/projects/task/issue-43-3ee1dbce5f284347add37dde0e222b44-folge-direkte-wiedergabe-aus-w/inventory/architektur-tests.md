# Detailbefund — Architektur, Tests, Build und wiederverwendbare Bausteine

## MVU-Architektur (neue App)

Eigenes schlankes MVU ohne externes Framework, Kern plattformneutral in
`VideoPlayer.Mvu`, App-Rand in `VideoPlayer.Maui`:

- **`MvuHost`** (`VideoPlayer.Mvu/MvuHost.cs`): hält `AppModel` (unveränderliches
  `sealed record`), `DispatchAsync(AppMessage)` serialisiert per Semaphore,
  feuert `PropertyChanged`/`ModelChanged` und führt die Effekte der Transition
  nacheinander über `IAppEffectHandler.HandleAsync` aus.
- **`AppUpdate.Update`** (`VideoPlayer.Mvu/AppUpdate.cs`, ~2500 Zeilen): statische
  Update-Funktion, großes `switch` über alle `AppMessage`-Typen (Z. 92–169),
  liefert `AppTransition(Model, Effects)`; `AppTransition.Unchanged` für
  Zustands-gleiche Nachrichten.
- **`AppMessage`** (`AppMessage.cs`, ~627 Zeilen): `abstract record` +
  `sealed record`s pro Nachricht, ausführliche XML-Doku (Deutsch).
- **`AppEffect`** (`AppEffect.cs`, ~435 Zeilen): `abstract record` + `sealed record`s
  pro Effekt (Navigation, Laden, Speichern, Wiedergabe, Downloads …).
- **`IAppEffectHandler`** (`IAppEffectHandler.cs`): Schnittstelle am App-Rand;
  Implementierung `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`,
  ~1490 Zeilen) — ein `switch` über alle Effekt-Typen (Z. 89–232), ruft Dienste auf
  und dispatcht Ergebnis-Messages zurück in den `MvuHost`.
- **`AppModel`** (`AppModel.cs`): ein globales State-Record; Seiten-Zustände als
  Sub-Records (`MediaDetailsState`, `SourceBrowserState`, `FavoritesPageState`,
  `DownloadsPageState`, `ProfilePageState`, `PlayerState`, `SourcesOverviewState`,
  `HomeHeroState` …), plus `Settings` (`AppSettings`).
- **Seiten-Pattern:** `ContentPage` mit `BindingContext = MvuHost`; `OnAppearing`
  abonniert `ModelChanged` (`mvuHost.ModelChanged -= …; += …`), `OnDisappearing`
  meldet ab; `UpdateUi(model)` liest den Modell-Stand (UI-Anpassungen via
  `MainThread.BeginInvokeOnMainThread`); Benutzeraktionen dispatcht die Seite als
  Messages. Seiten-Kommentare verweisen explizit auf „Muster: SettingsPage".
- **DI:** `MauiProgram.CreateMauiApp` (`VideoPlayer.Maui/MauiProgram.cs`):
  alle Dienste `Singleton`, Seiten `Transient`; `AppShell` Singleton.
  `MvuHost`+`IAppEffectHandler` zentral (Z. 44–55).
- **Naming:** `XxxRequestedMessage` / `XxxCompletedMessage` / `XxxLoadedMessage` für
  Messages; `XxxEffect` für Effekte; `UpdateXxx` für Update-Methoden; Services als
  `XxxService`; State als `XxxPageState`/`XxxState`; Anzeige-Items als `XxxItem`/
  `XxxListItem` (`HomeMediaItem`, `SourceMediaItem`, `FavoriteListItem`, `EpisodeItem`…).
- **Medientypen:** `MediaTypeNames.Normalize` bildet Server-Konvention
  (`movie`, `show`, `season`, `episode`, `collection`) auf CLR-Konvention
  (`movie`, `moviecollection`, `tvshow`, `tvshowseason`, `tvshowepisode`) ab.
- **Konvention „kein Wurf im UI":** Fehler werden geloggt (`Debug.WriteLine`) und/oder
  als `Error`-Feld im State abgelegt; Kommentare begründen Fallbacks ausführlich.

## Menü/Navigation

- `Shell` mit `FlyoutBehavior="Flyout"` (`AppShell.xaml`); fünf statische
  `FlyoutItem`s; dynamische Quellengruppe via Code in `AppShell.xaml.cs`
  (`Items.Insert` ab Index 1, Templates `SourceSectionTemplate`/`SourceErrorTemplate`/
  `SourceInfoTemplate`).
- Geschobene Routen via `Routing.RegisterRoute` in `AppShell.xaml.cs` Z. 59–77:
  `serversetup`, `login`, `mediadetails` (mit Query-Parametern), `sourceoverview`,
  `player`.
- `AppRoutes.TryGetPage` bildet Shell-Orte auf `AppPage` (Sync in `NavigatedMessage`).
- Neue Menüseite = `AppPage`-Wert + `AppRoutes`-Eintrag + `FlyoutItem` +
  Page + DI + State/Messages/Effects; Routen sind per Contract-Test gepinnt
  (`AppRoutesTests`, `VideoPlayerMauiContractTests` — dort `ExpectedMenuEntries`).

## Testinfrastruktur

- **`VideoWebPlayer.Maui.Tests`** (xUnit v3, `xunit.runner.visualstudio`,
  coverlet): **keine** MAUI-Targets referenzierbar — testet die plattformneutralen
  Projekte direkt (`VideoPlayer.Mvu`, `VideoPlayer.Storage`, `VideoPlayer.ApiClient`,
  `VideoWebPlayer.Client`, `msTools.DeveloperApi`) und die MAUI-Oberfläche über
  **Contract-Tests** (Quelltext-/XAML-Analyse mit `RepositoryRoot.Find()`).
  ~80 Testdateien; Struktur: `AppUpdateTests_*` (Update-Funktion pro Bereich),
  `*ServiceTests` (Dienste mit In-Memory-/Stub-Ablagen: `Stubs/InMemorySettingsStore`,
  `InMemorySecretStore`, `StubHttpMessageHandler`, `SqliteDatabaseCollection`),
  `*ContractTests` (UI-/Struktur-Pins: AutomationIds, Routen, Styles, `#if`-Blöcke),
  `EnumCoverageTests` (jeder Enum-Wert muss in Tests vorkommen — Git-Hook
  `enum-coverage-check`).
- **`VideoPlayer.E2E.Tests`** (xUnit v3 + FlaUI.UIA3): Windows-E2E gegen die echte
  `VideoPlayer.Maui.exe` (Debug-Build via `AppFixture`, isoliertes Profil via
  `VIDEOPLAYER_PROFILE`-Umgebungsvariable, siehe `AppProfile.FromEnvironment`),
  lokaler `MockVideoServer` (Roh-HTTP: health, pairing/exchange+ bootstrap,
  auth login/refresh/logout, continue-watching/progress, items/movie(+stream),
  favorites/continue-watching/Sources/items/recent-Listen), Steuerung über die
  Developer-API (`DevApiClient` → `/tree`, `/invoke`, `/set-text`, `/model`)
  plus FlaUI/UIA3. Tests: Bootstrap, Pairing, Login, ServerSetup, ServerSwitch,
  Playback (`PlayFromRecent_StartsPlaybackAtResumePosition` — Weiterschauen-Tap →
  Details → Abspielen → Resume-Seek). Collection `E2E` (sequentiell).
- **Developer-API** (`msTools.DeveloperApi(.Maui)`): lokaler Loopback-HTTP-Server
  nur unter `#if DEBUG || DEV_API` (`MauiProgram.cs` Z. 144–149), Endpunkte
  `/model` (Modell-Snapshot ohne Secrets), `/tree`, `/invoke`, `/set-text` —
  dokumentiert in `docs/help/developer-api.md`.
- **Git-Hooks** (`.githooks/`): `enum-coverage-check.py`, `csproj-xmldoc-check.py`
  (CS1591 ist `WarningsAsErrors`), `no-notimplemented-check.py`, `pre-commit`,
  `pre-push` u. a.

## Build-/Verifikationsbefehle

- Solution: `VideoPlayer.App.sln` (Repo-Root).
- Laut `README.md` („Bauen und Testen"):
  - `dotnet restore VideoPlayer.App.sln`
  - `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`
  - Windows-App: `dotnet build .\VideoPlayer.Maui\VideoPlayer.Maui.csproj -f net10.0-windows10.0.19041.0`
- CI (`.github/workflows/pr-checks.yml`): Restore → Build Client + Tests →
  Build `VideoWebPlayer.Maui` + `VideoPlayer.Maui` (je `net10.0-windows10.0.19041.0`,
  Release) → `dotnet test VideoWebPlayer.Maui.Tests` → `dotnet format whitespace
  --verify-no-changes`. iOS-Build in staging-/main-Workflows
  (`staging-merge.yml`, `main-merge.yml`); iOS-Geräte-Deployment über
  `scripts/iOS-Deployment.ps1` (Pair-to-Mac).
- Zielplattformen `VideoPlayer.Maui.csproj`: net10.0-windows10.0.19041.0,
  net10.0-ios, net10.0-maccatalyst, net10.0-android — einzeln abschaltbar über
  `Include{Android,Ios,MacCatalyst}Target` (Kommentar Z. 4–10).
- `.NET 10 SDK` + MAUI-Workload erforderlich (README).

## Wiederverwendbare Bausteine

- **Dienste (plattformneutral, `VideoPlayer.Mvu`):** `PlaybackSourceService`
  (Quellauflösung + Resume-Position), `HomeListService` (Cache→Anzeigeitems),
  `OfflineContinueWatchingService`, `MediaDetailsService`, `SourceBrowserService`,
  `FavoritesService`, `DownloadService`, `AutoDownloadService`,
  `ServerSyncService` (Cache-Schlüssel `sources`/`recent`/`continue_watching` +
  `cached_favorites`), `ProgressReportService` (Outbox `pending_progress`),
  `ServerEventsService`/`SignalRServerEventsConnection` (Hub `hubs/mediaupdate`;
  Server-Events `ContinueWatchingUpdated`/`FavoritesChanged`/`NewVideosScanned` →
  `ServerEventKind`; `StatusChanged` wird nur geloggt —
  `ServerEventsService.cs` Z. 148–157), `AuthSessionService` (Token/Refresh/Logout),
  `DeviceTokenService`, `ShowDetailsCache`, `ProfileInfoService`.
- **Ablage (`VideoPlayer.Storage`):** `CacheDatabase` (`videoplayer-cache.db3` —
  `cached_lists`, `cached_favorites`, `download_manifest`, …), `BackupDatabase`
  (`videoplayer.db3` — `known_servers`, `pending_progress`, …), `AppSettingsService`,
  `AppSecretsService` (SecureStorage), `ISettingsStore`/`ISecretStore`,
  `AppStorageLayout` (Backup/NoBackup/Temp-Verzeichnisse), `AppProfile`
  (Profil-Isolation via `VIDEOPLAYER_PROFILE`).
- **UI-Bausteine (`VideoPlayer.Maui`):** `Controls/MediaCarousel` (horizontale Liste),
  `Controls/CachedPosterImage` (Bildauflösung `api/pictures/{id}` mit lokaler Ablage),
  zentrale Design-Tokens `Vp*` (`Resources/Styles/Colors.xaml`, `Styles.xaml`:
  `Card`, `InputFrame`, `IconButton`, `SecondaryButton`, `SectionTitle`, `Kicker`,
  `Banner`, `ErrorText`, `Caption`, `PageTitle` …), Material-Icons-Font.
- **Dokumentation:** `docs/help/*.md` deckt die Funktionsbereiche ab (Index in
  `docs/help/index.md`); API-Client-Doku unter `docs/help/api-client/`.
