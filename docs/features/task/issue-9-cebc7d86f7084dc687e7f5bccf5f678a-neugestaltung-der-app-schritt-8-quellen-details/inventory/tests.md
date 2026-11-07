# Tests — Bestand und Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22 (UTC+2)
- Branch und Commit-ID:
  `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-8-quellen-details`
  @ `28f2e1d0f0503c11194353604533e16f8bb02643`
- Uncommittete Änderungen im getesteten Stand: `.gitignore` modifiziert
  und untracked `.agents/` — beides fremde Änderungen ohne Einfluss auf
  den Teststand, werden nicht committet.
- Testumgebung: Windows, .NET SDK 10.0.401, xUnit v3.
- Ermittelte Testsuiten: `VideoWebPlayer.Maui.Tests` (einzige Suite;
  referenziert `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`,
  `VideoPlayer.Storage`, `VideoWebPlayer.Client`).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --logger "console;verbosity=normal"` | Repo-Wurzel | 0 | 350 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 350 Tests bestanden.

### Testlücken und Ausführungsprobleme

- Kein UI-Automatisierungs-/E2E-Framework im Repository; das
  Testprojekt kann `VideoPlayer.Maui` (MAUI-Targets) nicht
  referenzieren. Benutzerfluss-Nachweise erfolgen daher — wie in den
  vorherigen Schritten — über MVU-Transitions-Tests (Message → Model +
  Effekte), Service-Tests mit `StubHttpMessageHandler`/realer SQLite-
  Datei und Contract-Tests der XAML-/DI-Verdrahtung (Quelltext-Prüfung).
  Diese Substitution ist seit Schritt 1 dokumentiert und wird auch für
  Schritt 8 angewandt; echte UI-E2E bleibt eine bekannte Lücke des
  Gesamtprojekts.
- `VideoPlayer.Maui` ist nur per Windows-Build kompilierbar
  (`net10.0-windows10.0.19041.0`); iOS/MacCatalyst benötigen macOS.

## Relevante Testklassen (Bestand)

### `AppUpdateTests_*` (MVU-Übergänge, plattformneutral)

- `AppUpdateTests_HomeLists` — Cache-First-Laden, `CachedListsChangedMessage`
  → `LoadHomeListsEffect`, `MediaItemSelectedMessage` →
  `NavigateToMediaDetailsEffect` (Validierung Id > 0, MediaType belegt).
- `AppUpdateTests_Navigation`, `AppUpdateTests_NetworkSync` —
  Zustands-Guards, Offline/Online, Ereignis → Teil-Sync.
- Muster: `AppUpdate.Update(model, message)` → `transition.Model` /
  `transition.Effects` prüfen.

### Service-Tests

- `HomeListServiceTests` (`[Collection("SqliteDatabase")]`):
  reale `CacheDatabase` in Temp-Verzeichnis (`AppStorageLayout` +
  `EnsureDirectoriesExist` + `InitializeAsync`), Cache-Schreibung per
  `InsertOrReplaceAsync`/`InsertAsync` im Format von `ServerSyncService`.
- `ServerSyncServiceTests` — `cached_favorites`-Konvention
  (`{clrtype}:{id}`) ist hier belegt.
- `AuthSessionServiceTests` — `StubHttpMessageHandler` +
  `ApiClientTestFactory` liefern antwortende Clients.

### Contract-Tests

- `HomePageContractTests`, `VideoPlayerMauiContractTests` —
  `RepositoryRoot.Find()` + `File.ReadAllText` auf XAML/Code-Behind/
  `MauiProgram`/`AppShell` — Vorlage für die Schritt-8-Contract-Tests.

### API-Client-Tests (bereits vorhanden, bleiben unverändert)

`VideoPlayerApiClientTests_Endpoints` belegt die Schritt-8-Endpunkte:
`/api/Sources`, `/api/SourceGenres/5`, `/api/items` mit Paging/
Suche/Genre, `/api/items/movie|moviecollection|tvshow|tvshowepisode/{id}`,
`/api/items/genres`, `/api/favorites`(+`toggle`/`remove`),
`/api/pictures/7`, `/api/sourceicons/8`.
