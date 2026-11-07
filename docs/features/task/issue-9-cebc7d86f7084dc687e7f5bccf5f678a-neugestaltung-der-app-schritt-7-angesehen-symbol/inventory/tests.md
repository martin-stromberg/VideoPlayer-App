# Inventur: Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-23, ~00:33-00:45 +02:00
- Branch und Commit-ID: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-7-angesehen-symbol` @ `9d2f9de`
- Uncommittete Änderungen im getesteten Stand: untracked `docs/features/task/issue-9-customer-feedback.md` (Fremddatei) sowie die neuen Planungsartefakte dieses Schritts unter `docs/features/task/…-schritt-7-angesehen-symbol/` — keinerlei Produktivcode-Änderung
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401, Ziel `net10.0`
- Ermittelte Testsuiten und Quellen der Testbefehle: einzige Testsuite `VideoWebPlayer.Maui.Tests` (xUnit); Befehl gemäß DoD aus `AGENTS.md`/Projektkonvention

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --nologo` | Repo-Root | 0 | 728 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — der Ausgangslauf ist vollständig grün (728/728).

### Testlücken und Ausführungsprobleme

Keine. Hinweis ohne Befundcharakter: Der Build des Testlaufs meldet
CS8618-Warnungen in `VideoWebPlayer.Client`-DTOs (Legacy-Bestand, unverändert
durch diesen Schritt). Playwright-/Server-Tests gehören nicht zu diesem Repo.

## Testklassen

### `MediaEntrySerializationTests`

- `MediaEntry_DeserializesByTypeDiscriminator` — `$type`-Diskriminierung aller sechs Typen
- `MediaEntry_SerializesWithTypeDiscriminator` — `$type` beim Serialisieren
- Eigene `JsonSerializerOptions` mit `PropertyNameCaseInsensitive` — spiegelt die Client-Pipeline

### `MediaDetailsServiceTests`

- `Load_Movie_MapsBasicDetails`, `Load_MovieCollection_MapsContainedMovies`, `Load_TvShow_…` u. a. — Online-Mapping über `StubHttpMessageHandler.EnqueueJson` (serialisiert die ApiClient-Modelle direkt)
- Offline-Pfade (`Load_…_Offline…`) über `CacheDatabase` + `ShowDetailsCache`/`CachedFavoritesEntry`, injizierbare `fileExists`-Prüfung
- Hilfsmethode `Show(long id)` baut eine `TvShow` mit zwei Staffeln/Episoden

### `AppUpdateTests_Player`

- `PlayingState`/`WithPlayer`/`WithDetails`-Hilfen: Player-State für `tvshowepisode` 31 in geladenen Seriendetails (Staffeln 21/22, Episoden 31-34)
- `PlaybackProgress_MarksWatchedWithinWindow` — Watched-Schwelle (`WatchedEndWindowSeconds`, Default 300)
- `PlaybackEnded_MarksWatchedAndAdvances`, `PlaybackEnded_WithoutNext_MarksWatchedOnly` — Ende → `IsWatched`, Abschlussmeldung, Auto-Fortsetzung

### `AppUpdateTests_MediaDetails`

- `MediaDetailsRequested_*`, `MediaDetailsLoaded_*`, `SeasonSelected_*`, `FavoriteToggle*` — Detail-Ladung, Staffelwahl, Favoriten
- `ShowDetails`-Hilfe mit `SeasonItem`/`EpisodeItem`-Fixtures

### `MediaDetailsPageContractTests` (Schritt 6 angelegt)

- Quell-Vertragstests gegen `MediaDetailsPage.xaml(.cs)`, `MediaDetailsView`, `MediaDetailsState`, `AppMessage`/`AppEffect`, `ShellAppEffectHandler`, `VideoPlayerApiClient` — die MAUI-Schicht ist im Testprojekt nicht instanziierbar

### `DesignTokensContractTests`

- `Colors_DefinesAllWebTokens`, `Colors_DefinesOverlayAndShadowTokens`, `Colors_ContainsNoTemplatePalette`, `PagesAndControls_ContainNoHardcodedColors`, `CodeBehind_ContainsNoHardcodedColors` — pinnen die Token-Werte und verbieten Farbliterale außerhalb `Colors.xaml`

## Hilfsmethoden

### `StubHttpMessageHandler` (`VideoWebPlayer.Maui.Tests/Stubs`)

- `EnqueueJson(HttpStatusCode, object)` — stellt serialisierte Antworten bereit; `Requests` protokolliert Anfragen (Pfad/Methode/Header)

### `RepositoryRoot` (`Stubs`)

- `Find()` — lokalisiert den Repo-Root für Quell-Vertragstests

### Weitere

- `InMemorySettingsStore`, `InMemorySecretStore`, `AppSecretsService`, `AuthSessionService` (echte Dienste mit HTTP-Stub), `CacheDatabase` auf temp. `AppStorageLayout`, `RecordingDelays`
