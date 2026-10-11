# Bestandsaufnahme: Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-11 19:1x (UTC+2)
- Branch und Commit-ID: `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w-schritt-7-download-reihenfolge`, Commit `6cd811f` („chore: Schritt 6 fertig gemerged, Schritt 7 in Arbeit")
- Uncommittete Änderungen im getesteten Stand: nur neue Lifecycle-Artefakte unter `docs/features/…schritt-7-download-reihenfolge/` (kein Produktivcode)
- Testumgebung und Runtime-/SDK-Versionen: `dotnet` SDK 10.0.401; xUnit v3; E2E = FlaUI UIA3 auf `net10.0-windows` + `MockVideoServer` + Developer-API
- Ermittelte Testsuiten: `VideoWebPlayer.Maui.Tests` (Unit, `net10.0`), `VideoPlayer.E2E.Tests` (`net10.0-windows`, FlaUI + `MockVideoServer`)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline Unit | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --nologo` | Repo-Stamm | 0 | 1278 | 0 | 0 | [baseline-unit-tests.log](test-results/baseline-unit-tests.log) |

### Nachgewiesene bestehende Testfehler

Keine — der Baseline-Lauf der Unit-Testsuite war vollständig grün. Die E2E-Suite wurde in der Bestandsaufnahme nicht ausgeführt (siehe Testlücken); daraus folgt keine Aussage über ihren Ausgangszustand.

### Testlücken und Ausführungsprobleme

- **E2E-Baseline nicht ausgeführt:** Der komplette E2E-Lauf (Debug-Build der MAUI-App + alle FlaUI-Szenarien) dauert erfahrungsgemäß deutlich über 45 Minuten (Konvention aus Vorgängerschritten). Die Anforderung betrifft reine Service-Logik ohne UI-Änderung; es existiert kein E2E-Test für Downloads (keine `*Download*Tests.cs` unter `VideoPlayer.E2E.Tests/Tests/`). Der Funktionsnachweis erfolgt über die Unit-Testsuite mit echter SQLite-Ablage und HTTP-Stub.

## Testklassen (relevant für diese Anforderung)

### `AutoDownloadServiceTests` (`VideoWebPlayer.Maui.Tests/AutoDownloadServiceTests.cs`)

Echte SQLite-Ablage (`CacheDatabase` im Temp-Verzeichnis) + `Stubs/StubHttpMessageHandler`; Cache-Einspeisung der Weiterschauen-Liste über `CachedListEntry` mit serialisiertem `ContinueWatchingEntry[]`.

- `Run_MovieCandidate_EnqueuesDirectly` — Film-Kandidat reiht sich ohne Detail-Request ein
- `Run_EpisodeCandidate_EnqueuesUpcomingEpisodes` — Episode löst `api/items/tvshow/{id}` auf und reiht `PreloadEpisodeCount` Folge-Episoden ein
- `Run_EpisodeCandidate_CrossesIntoNextSeason` — staffelübergreifende Serienreihenfolge inkl. `DownloadSeriesContext`-Persistenz
- `Run_Disabled_DoesNothing`, `Run_WifiOnlyWithoutWifi_DoesNothing`, `Run_NotLoggedIn_DoesNothing` — Schalter/Bedingungen
- `Run_ExistingManifestEntry_Deduplicates` — Dedup
- `Run_Throttled_UnlessForced` — `MinRunInterval`-Throttle
- `Run_EpisodeWithoutSeason_SkipsCandidate` — fehlender Serienkontext → übersprungen

### `DownloadServiceTests` (`VideoWebPlayer.Maui.Tests/DownloadServiceTests.cs`)

Warteschlange, Dedup, Quota, Abbruch/Löschung, Fortschritt — mit echter SQLite-Ablage.

### `AppUpdateTests_Downloads` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Downloads.cs`)

MVU-Pfade `DownloadRequestedMessage` → `EnqueueDownloadEffect` (Serien-Hints), Offline-Abweisung, Retry/Cancel/Delete.

### `OfflineContinueWatchingServiceTests`

Offline-Platzhalter der Folge-Episoden aus Manifest + `ShowDetailsCache` (Serienkontext-Spalten).

### `LocalDatabaseTests` / `DownloadsContractTests`

Cache-Datenbank-Initialisierung; Contract-Pins der Downloads-Seite/MAUI-Verdrahtung.

### `EnumCoverageTests`

Erzwingt Testabdeckung je Enum-Wert — kein neuer Enum geplant.

## Hilfsmethoden

### `AutoDownloadServiceTests` (Testklasse)

- `CacheContinueWatchingAsync(params ContinueWatchingEntry[])` — schreibt die Liste serialisiert in `cached_lists`
- `ShowWithEpisodes(id, name, eps)` — baut `TvShow` mit Staffeln/Episoden für den `api/items/tvshow`-Stub
- `EntriesAsync()` — liest `download_manifest`
- `WaitForAsync(done)` — wartet auf asynchrone Transfer-Endzustände
- `FakeNetworkMonitor` (`IsWifi`/`IsOnline`)

### `Stubs/StubHttpMessageHandler`

- `Enqueue(HttpResponseMessage)`, `Enqueue(statusCode, jsonBody)`, `EnqueueJson(statusCode, obj)`, `Enqueue(Func<…, Task<HttpResponseMessage>>)` — Antwort-Queue bzw. Responder-Delegate
- `Requests` — aufgezeichnete Requests (`PathAndQuery`, Header, Body)

### `MockVideoServer` (E2E)

Playlist-Endpunkte inkl. `GET /api/playlists/{id}/entries` und `/entries/paged` (Server-Reihenfolge, `sortOrder` = Listenindex), `MockPlaylist`/`MockPlaylistEntry` mit `MediaType`, `MediaId`, `ParentMedia*`, `IsAccessible`, `EpisodeNumber` — steht für eventuelle E2E-Erweiterungen bereit.
