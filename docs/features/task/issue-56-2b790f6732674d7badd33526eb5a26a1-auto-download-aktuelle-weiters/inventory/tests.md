# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-17, ~12:20–12:22 MESZ (UTC+02:00); entspricht ~10:20–10:22 UTC.
- Branch und Commit-ID: `task/issue-56-2b790f6732674d7badd33526eb5a26a1-auto-download-aktuelle-weiters`, Commit `14ca0b3afd8b8d6ad1e8b3bf3545eb88aea2555c` („Download optimieren (#53)").
- Uncommittete Änderungen im getesteten Stand: nur untracked Dateien — das Feature-Verzeichnis `docs/features/task/issue-56-…/` (damals `requirement.md`, `todo.md`; die Inventar-Dateien und Testnachweise entstanden erst während/nach dem Lauf) sowie die leere, untracked Datei `nul` (0 Byte, Windows-Artefakt im Repo-Wurzel). Keine Änderungen an Produktivcode, Tests oder Testkonfiguration.
- Testumgebung und Runtime-/SDK-Versionen: Windows 11, .NET SDK 10.0.402, Workload-Version 10.0.401 (installiert: `maui-windows`, `android`, `ios`, `maccatalyst`); Testprojekt `net10.0` mit xunit.v3 3.2.2, Microsoft.NET.Test.Sdk 18.8.1, coverlet.collector 10.0.1.
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `VideoWebPlayer.Maui.Tests` — einzige CI-Testsuite (`.github/workflows/pr-checks.yml`: Release-Build des Testprojekts, dann `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --configuration Release`; auch README „Bauen und Testen").
  - `VideoPlayer.E2E.Tests` — Windows-E2E-Suite (`net10.0-windows`, FlaUI/UIA3 gegen die echte Debug-App + lokalen Mock-Server; `docs/help/e2e-tests.md`). Nicht Teil des CI-Gates; benötigt eine interaktive Desktop-Session (siehe Testlücken).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Build (Voraussetzung) | `dotnet build VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release` | Repo-Wurzel `D:\Repositories\softwareschmiede\2b790f67-3267-4d7b-add3-3526eb5a26a1` | 0 | — | — | — | [dotnet-build-tests-release-2026-10-17.log](test-results/dotnet-build-tests-release-2026-10-17.log) |
| Ausgangslauf | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --configuration Release --logger "console;verbosity=normal"` | Repo-Wurzel `D:\Repositories\softwareschmiede\2b790f67-3267-4d7b-add3-3526eb5a26a1` | 0 | 1326 | 0 | 0 | [dotnet-test-release-2026-10-17.log](test-results/dotnet-test-release-2026-10-17.log) |

Build-Hinweise des Laufs (keine Fehler, bestehender Stand): 10 Warnungen im Altbestand `VideoWebPlayer.Client` (`CS0108` ausgeblendete Basismember in `DtoSource.cs`, `CS8618` nicht-nullbare Eigenschaften ohne Initialisierung in `MediaEntryDto.cs`/`FavoriteEntry.cs`/`DtoSource.cs`/`DtoRecentEntry.cs`/`ContinueWatchingDto.cs`/`AuthorizationToken.cs`); Gesamtzeit des Testlaufs 34,96 s.

### Nachgewiesene bestehende Testfehler

Keine — alle 1326 Tests des Ausgangslaufs waren erfolgreich (0 Fehlschläge, 0 übersprungen).

### Testlücken und Ausführungsprobleme

- **`VideoPlayer.E2E.Tests` nicht ausgeführt.** Die Suite baut die MAUI-App (`VideoPlayer.Maui`, `net10.0-windows10.0.19041.0`, Debug) und steuert sie per FlaUI/UI-Automation über sichtbare Fenster; Voraussetzung ist eine interaktive Windows-Desktop-Session, die dokumentierte Laufzeit liegt bei ~20–25 min. Umfang heute: 62 `[Fact]`-Tests in 16 Dateien unter `VideoPlayer.E2E.Tests/Tests/` (die Doku `docs/help/e2e-tests.md` nennt noch 19 bzw. 25 Tests aus der Anfangszeit). Sie ist nicht Teil des CI-Gates (`pr-checks.yml` führt sie nicht aus). Für die Bestandsaufnahme ist ihr Ausgangszustand damit **unbekannt** — daraus folgt keine Aussage über ihren Stand.
- Die MAUI-Projekte `VideoPlayer.Maui`/`VideoWebPlayer.Maui` sind vom Testprojekt nicht referenzierbar (MAUI-Zielframeworks); ihre Verdrahtung (u. a. `ShellAppEffectHandler`-Zweige, DI-Registrierung in `MauiProgram`) wird nur über Quelltext-Contract-Tests geprüft (z. B. `DownloadsContractTests.ShellAppEffectHandler_WiresDownloadEffects`, `MauiProgram_RegistersDownloadServices`) — Laufzeitverhalten dieser Projekte ist ungetestet.
- Kein Test prüft heute, dass die aktuelle Episode eines Weiterschauen-Eintrags selbst eingeplant wird — die bestehenden `AutoDownloadServiceTests` erwarten das heutige Verhalten „nur Folge-Episoden" (siehe Testklassen).

## Testklassen

### `AutoDownloadServiceTests` (`VideoWebPlayer.Maui.Tests/AutoDownloadServiceTests.cs`)

`[Collection("SqliteDatabase")]`, `IDisposable`; echte SQLite-Ablage in einem temp. `AppStorageLayout`, `StubHttpMessageHandler` als Server, `FakeNetworkMonitor` (privat, `IsWifi`/`IsOnline` steuerbar), produktive `AuthSessionService`-/`DownloadService`-/`HomeListService`-/`AutoDownloadService`-Instanzen. Standard-`settings`: `AutoDownloadEnabled = true`, `PreloadEpisodeCount = 2`, `WifiOnlyDownloads = true` (Z. 31–36).

- `Run_MovieCandidate_EnqueuesDirectly` — Film-Kandidat wird ohne Server-Roundtrip (nur Stream-Request) eingereiht und fertig geladen.
- `Run_EpisodeCandidate_EnqueuesUpcomingEpisodes` — Episode löst die Serie auf und reiht die nächsten `PreloadEpisodeCount` **Folge**-Episoden ein (aktuell: 2 Einträge = Folge-Episoden 33/34; die aktuelle Episode 32 wird nicht eingeplant).
- `Run_EpisodeCandidate_CrossesIntoNextSeason` — nach der letzten Episode einer Staffel geht es staffelübergreifend weiter (`PreloadEpisodeCount = 1` → S2E1 mit Serienkontext im Manifest).
- `Run_Disabled_DoesNothing` — deaktivierter Schalter → kein Auftrag, kein Request.
- `Run_WifiOnlyWithoutWifi_DoesNothing` — WLAN-only ohne WLAN → nichts.
- `Run_NotLoggedIn_DoesNothing` — ohne Serververbindung → nichts.
- `Run_ExistingManifestEntry_Deduplicates` — bereits vorhandener wartender Auftrag wird nicht doppelt angelegt.
- `Run_Throttled_UnlessForced` — Intervall-Throttling: Zweitlauf innerhalb `MinRunInterval` übersprungen, `force: true` läuft erneut.
- `Run_EpisodeWithoutSeason_SkipsCandidate` — Episoden-Eintrag ohne Staffel-/Serienbezug (`ShowId` leer, kein Playlist-Bezug) wird komplett übersprungen (kein Auftrag, kein Request).
- `Run_PlaylistEpisode_EnqueuesPlaylistOrder` — Playlist-Bezug: Folge-Einträge in Playlist- statt Serienreihenfolge (Film zwischen den Episoden), Manifest trägt `PlaylistId`/`PlaylistEntryId`/`PlaylistSortOrder`; kein Serien-Detail-Request.
- `Run_PlaylistEpisode_SkipsNonDownloadableEntries` — nicht ladbare (`TVShow`) und nicht zugängliche (`IsAccessible = false`) Einträge werden übersprungen, ohne `remaining` zu verbrauchen.
- `Run_PlaylistEpisode_PersistsSeriesHints` — Serien-Hints der Playlist-Einträge (`ParentMedia*`, `EpisodeNumber`) landen im Manifest (`SeasonNumber` bleibt `null`; Titel „Probe – Folge B").
- `Run_PlaylistMovie_EnqueuesSelfAndNextEntries` — playlist-gebundener Film lädt sich selbst plus Folge-Einträge (3 Einträge bei `PreloadEpisodeCount = 2`: Film + 2 Folge-Filme — der Film selbst zählt nicht ins Budget).
- `Run_PlaylistEntryNotFound_FallsBackToSeries` — aktueller Eintrag nicht mehr in der Playlist → Rückfall auf den Serien-Pfad (Einträge ohne `PlaylistId`, Serien-Detail-Request sichtbar).
- `Run_PlaylistEntryMissing_FallsBackToSeries` — `PlaylistId` ohne `PlaylistEntryId` → Serien-Pfad, kein Playlist-Request.
- `RunAsync_LogsOutcome` — Abschluss- („… abgeschlossen") und Mindestabstand- („Mindestabstand") Protokolleinträge auf Information-Ebene über `RecordingLoggerProvider`.

### `AppUpdateTests_Downloads` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Downloads.cs`)

MVU-`Update`-Tests der Download-/Vorlauf-Verdrahtung (Auswahl der für die Anforderung relevanten):

- `SyncCompleted_AutoDownloadsEnabled_RunsAutoDownloads` / `SyncCompleted_AutoDownloadsDisabled_NoAutoRun` — `RunAutoDownloadsEffect` nach Voll-Sync nur bei aktiviertem Schalter.
- `CachedListsChanged_AutoDownloadsEnabled_RunsAutoDownloads` / `CachedListsChanged_NotConnected_NoAutoRun` — Vorlauf-Effekt nach Listenänderung nur bei `Connected` + Schalter.
- `SaveSettings_EnablingAutoDownloads_ForcesAutoRun` / `SaveSettings_AlreadyEnabled_NoForcedRun` — `Force`-Flag beim Einschalten.
- `PlaybackProgress_WatchedEpisode_EmitsAutoDelete` / `…_WatchedMovie_NoAutoDelete` / `…_AutoDeleteDisabled_NoEffect` / `PlaybackEnded_WatchedEpisode_EmitsAutoDelete` — `DeleteWatchedDownloadEffect`-Bedingungen (nur `tvshowepisode` + Schalter).
- Übrige Tests der Klasse: Downloads-Seiten-Ladung, `DownloadRequested*`/`EnqueueDownloadEffect`, Abbruch/Wiederholen/Löschen, `ResumeDownloadsEffect`-Trigger bei Login/Netzwerk-Rückkehr.

### Weitere fachlich angrenzende Testklassen

- `DownloadServiceTests` (`DownloadServiceTests.cs`) — `EnqueueAsync`-Semantik: Dedup-Hinweise (`Enqueue_CompletedExisting_ReturnsAlreadyPresent`, `Enqueue_QueuedExisting_ReturnsAlreadyQueued`, `Enqueue_FailedEntry_Retries`), Kontext-Persistenz (`Enqueue_WithSeriesContext_PersistsContextInManifest`), Quota (`Enqueue_QuotaExhausted_FailsWithQuotaError`, `…_DuringTransfer_FailsAndRemovesPartial`), WLAN (`Enqueue_WifiOnlyWithoutWifi_StaysQueued`, `Resume_WifiOnly_WifiAvailable_StartsDownload`), Protokollierung (`EnqueueAsync_TransferOutcome_Logged`, `…_DedupNotice_LogsBelowInformation`, `…_QuotaExceeded_LogsSingleWarning`), Fehlerpfade (`Enqueue_ServerError_FailsWithHttpError`, `…_TransportError_FailsWithNetworkError`, `Enqueue_InvalidMediaType_Rejected`, `Enqueue_WithoutLogin_FailsNoConnection`) u. a.
- `DownloadServiceReconcileTests` (`DownloadServiceReconcileTests.cs`) — Ablageort↔Manifest-Abgleich (`Reconcile_*`, `DeleteAll_RemovesFilesAndManifest`, `PurgeOrphans_RemovesOnlyOrphaned`).
- `OfflineContinueWatchingServiceTests` (`OfflineContinueWatchingServiceTests.cs`) — Offline-Weiterschauen inkl. Platzhalter fehlender Folge-Episoden aus dem `ShowDetailsCache`-/`DownloadSeriesContext`-Stand (`ContinueWatching_EpisodeWithMissingNext_ShowsPlaceholder`, `…_NextEpisodeDownloaded_NoPlaceholder` u. a.).
- `DownloadsContractTests` (`DownloadsContractTests.cs`) — Quelltext-Contracts, u. a. `MauiProgram_RegistersDownloadServices` (`AddSingleton<DownloadService>`/`AddSingleton<AutoDownloadService>`) und `ShellAppEffectHandler_WiresDownloadEffects` (Effekt-Zweige inkl. `RunAutoDownloadsEffect`/`DeleteWatchedDownloadEffect`, Ereignis-Abonnements).
- `HomeListServiceTests`, `ServerSyncServiceTests`, `PlaylistServiceTests`, `VideoPlayerApiClientTests_Playlists` — Cache-/Sync-/API-Abdeckung der Weiterschauen- und Playlist-Daten.

## Hilfsmethoden

### `AutoDownloadServiceTests` (interne Helfer, `AutoDownloadServiceTests.cs`)

- `LoginAsync` — simuliert die Anmeldung (Token-Antwort einreihen, `authSession.LoginAsync`, Request-Log leeren).
- `CacheContinueWatchingAsync(params ContinueWatchingEntry[])` — serialisiert die Einträge in den `continue_watching`-Schlüssel von `cached_lists` (Cache-Quelle des Vorlaufs).
- `ShowWithEpisodes(long id, string name, params (int Season, int Episode, long Id, string Ep)[])` — baut die `TvShow`-Antwort (Staffeln gruppiert, ohne Show-Rückverweis auf den Staffeln).
- `EntriesAsync` — liest alle `DownloadManifestEntry`-Zeilen der Test-SQLite-DB.
- `WaitForAsync(Func<Task<bool>>, int timeoutMs = 15000)` — pollbedingtes Warten auf den asynchronen Transfer-Endzustand.
- `PlaylistPageJson(bool hasNextPage, params object[] entries)` — JSON-Antwort einer `entries/paged`-Seite.
- `PlaylistEntryJson(id, mediaType, mediaId, title, parentMediaType, parentMediaId, parentMediaTitle, isAccessible, sortOrder, episodeNumber)` — JSON-Playlist-Eintrag in Server-Feldnamen.
- `FakeNetworkMonitor` (private Klasse) — `INetworkStatusMonitor`-Fake mit steuerbaren `IsOnline`/`IsWifi`.

### Stubs (`VideoWebPlayer.Maui.Tests/Stubs/`)

- `StubHttpMessageHandler` (`StubHttpMessageHandler.cs`) — Server-Simulation: Antwort-Queue (`Enqueue`, `Enqueue(statusCode, jsonBody)`, `EnqueueJson`, `EnqueueException`, `Enqueue(responder)`, `EnqueueAsyncResponder`), aufgezeichnete Requests als `RecordedRequest` (`Method`, `PathAndQuery`, `Authorization`, `ApiKey`, `Body`) in `Requests`; wirft `InvalidOperationException` bei unerwarteten Requests.
- `RecordingDelays` (ebd.) — zeichnet `Delay`-Aufrufe auf, ohne zu warten (Delay-Haken des `AuthSessionService`).
- `ApiClientTestFactory` (ebd.) — `CreateClient`/`CreateClientWithDelays` für `VideoPlayerApiClient` über dem Stub-Handler.
- `RecordingLoggerProvider` (`RecordingLoggerProvider.cs`) — `ILoggerProvider`/`ILoggerFactory`-Ersatz; Einträge als `RecordedLogEntry` (`Level`, `Category`, `Message`, `Exception`) in `Entries`.
- `InMemorySettingsStore` (`InMemorySettingsStore.cs`) — `ISettingsStore`-Fake (Dictionary), `Keys` für Isolationsprüfungen.
- `InMemorySecretStore` (`InMemorySecretStore.cs`) — `ISecretStore`-Fake (Dictionary), `Keys`.
- `SqliteDatabaseCollection` (`SqliteDatabaseCollection.cs`) — `[CollectionDefinition("SqliteDatabase")]`: serialisiert alle Testklassen mit echter SQLite-Ablage (`ResetPool` ist nicht parallel-sicher).
- `RepositoryRoot` (`RepositoryRoot.cs`) — findet die Repo-Wurzel (enthält `VideoPlayer.App.sln`) für die Quelltext-Contract-Tests.
