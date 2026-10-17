# Tests

Bestehende Tests und Hilfsmethoden für den Bereich „Fortschrittsmeldung / Playlist-Wiedergabe" sowie der projektweite Test-Ausgangszustand.

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-10-17 18:12–18:14 +02:00 (CEST; 16:12–16:14 UTC)
- **Branch und Commit-ID:** `task/issue-59-a0c50214ceee42c7ae974890cd4a3cdd-playlist-bezug-der-weiterschau` @ `277219c34d138c8339ce5931119e978a1e65be7b` („fix: Download-Manifest-Pfade nach Container-Umzug über Dateinamen auflösen (#58)")
- **Uncommittete Änderungen im getesteten Stand:** nur untracked `docs/features/task/issue-59-a0c50214ceee42c7ae974890cd4a3cdd-playlist-bezug-der-weiterschau/` (übersetzte `requirement.md` sowie diese Bestandsaufnahme inkl. `test-results/`); **keine** Änderungen an Produktivcode, Tests oder Konfiguration
- **Testumgebung und Runtime-/SDK-Versionen:** Windows, .NET SDK 10.0.402, Test-Laufzeit .NET 10.0.13 (64-bit), xUnit.net v3 (xunit.v3 3.2.2, VSTest Adapter 3.1.5); installierte MAUI-Workloads: maui-windows 10.0.20/10.0.100, android, ios, maccatalyst
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `VideoWebPlayer.Maui.Tests` (Target `net10.0`, xunit.v3) — Unit-/Contract-Tests. Quelle des Befehls: CI-Workflow `.github/workflows/pr-checks.yml` (`dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`, Release) und README „Bauen und Testen".
  - `VideoPlayer.E2E.Tests` (Target `net10.0-windows`, xunit.v3 + FlaUI.UIA3) — Windows-End-to-End-Tests gegen die echte App + lokalen `MockVideoServer`. Quelle des Befehls: `docs/help/e2e-tests.md` (`dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj`); **nicht** Teil des CI-PR-Gates.
  - Keine weiteren Testprojekte im Repository; kein eigenes `src/`-Verzeichnis — Projekte liegen im Root.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 (Ausgangslauf Unit/Contract) | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release --logger "console;verbosity=normal" --logger "trx;LogFileName=VideoWebPlayer.Maui.Tests-baseline.trx" --results-directory docs\features\task\issue-59-a0c50214ceee42c7ae974890cd4a3cdd-playlist-bezug-der-weiterschau\inventory\test-results` | Repository-Root | 0 | 1334 | 0 | 0 | [Konsolen-Log](test-results/dotnet-test-VideoWebPlayer.Maui.Tests.log), [TRX-Report](test-results/VideoWebPlayer.Maui.Tests-baseline.trx) |
| 2 (E2E: nur Build + Discovery, keine Ausführung) | `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --configuration Release --list-tests` | Repository-Root | 0 | — (nicht ausgeführt; 62 Tests aufgelistet) | — | — | [Log](test-results/dotnet-test-E2E-list-tests.log) |

Anmerkung Lauf 1: Testzeit ~44,3 s. Der Lauf enthält die Release-Kompilierung der referenzierten Projekte; es traten ausschließlich bereits vorhandene Build-Warnungen in `VideoWebPlayer.Client` auf (CS0108 „Id/Name blendet geerbten Member aus" in `DtoSource.cs`; mehrere CS8618 Non-Nullable-Warnungen in Altbestand-DTOs, u. a. `ContinueWatchingDto.cs`). Keine Testfehler, keine übersprungenen Tests (Ausgabe „Gesamtzahl Tests: 1334 / Bestanden: 1334").

### Nachgewiesene bestehende Testfehler

**Keine.** Im dokumentierten Ausgangslauf (Lauf 1) sind alle 1334 Tests der Suite `VideoWebPlayer.Maui.Tests` erfolgreich. Zur nicht ausgeführten E2E-Suite kann keine Aussage über Einzeltests getroffen werden (siehe Testlücken).

### Testlücken und Ausführungsprobleme

- **`VideoPlayer.E2E.Tests` — nicht ausgeführt (62 Tests, Discovery-Nachweis siehe Lauf 2).** Grund: Die Suite benötigt eine interaktive Windows-Desktopsitzung mit sichtbaren Fenstern (FlaUI/UI-Automation); die Test-Fixture baut `VideoPlayer.Maui` selbst im Debug (`net10.0-windows10.0.19041.0`) und startet pro Test einen echten App-Prozess inkl. Wiedergabe — dokumentierte Laufzeit ca. 20–25 min (Hinweis in `docs/help/e2e-tests.md`, bezieht sich auf eine kleinere Suite; aktuell 62 Tests). Die Suite ist nicht Teil des CI-PR-Gates (`pr-checks.yml` führt nur `VideoWebPlayer.Maui.Tests` aus). Build des Testprojekts (`net10.0-windows`, Release) und Test-Discovery sind erfolgreich — ein Build-/Setup-Problem liegt also nicht vor; die Ausführung wurde für die Bestandsaufnahme nicht angestoßen (Infrastruktur-/Laufzeitaufwand, keine Fenstersession-Garantie im Laufkontext). Betroffener Umfang: alle 62 E2E-Tests, darunter `PlaylistPlaybackTests` (6 Tests), `ContinueWatchingPlaybackTests` (3), `ContinueWatchingContextMenuTests` (3), `PlaybackTests` (2).
- **Keine `src/`-Projekte / keine weiteren Suiten:** `VideoPlayer.Maui` (MAUI-App, Multi-Target) und `VideoWebPlayer.Maui` (Altbestand) werden in der CI nur gebaut (`net10.0-windows10.0.19041.0`), nicht getestet; ein Testprojekt für den Altbestand existiert nicht.

## Testklassen

### `AppUpdateTests_Player`
Datei: `VideoWebPlayer.Maui.Tests/AppUpdateTests_Player.cs` — MVU-Übergänge der Wiedergabeseite.

- `PlaybackProgress_ReportsChangedPosition` — `PlaybackProgressMessage` erzeugt `ReportPlaybackProgressEffect` mit `MediaType`/`MediaId`/`Position`/`Duration` (prüft die vier Felder; Playlist-Felder existieren nicht)
- `PlaybackProgress_UnchangedPosition_NoReport` — unveränderte Position → keine Meldung (Throttling)
- `PlaybackProgress_MarksWatchedWithinWindow` — „angesehen"-Fenster
- `PlaybackProgress_AutoResumeAdvancesWhenEnabled` / `…_AutoResumeDisabled_DoesNotAdvance` / `…_AboveResumeWindow_DoesNotAdvance` / `…_PausedWithinWindow_DoesNotAdvance` / `…_NoNextEpisode_DoesNotAdvance` — Auto-Fortsetzung Serienpfad
- `PlaybackEnded_MarksWatchedAndAdvances` / `…_WithoutNext_MarksWatchedOnly` / `…_MidPlayback_IsIgnored` / `…_UnknownDuration_IsIgnored` / `…_WithinTolerance_Completes` / `…_JustOutsideTolerance_IsIgnored` / `…_AfterAdvance_IgnoresStaleEvent` — Ende-Ereignis inkl. Abschluss-`ReportPlaybackProgressEffect` (Endposition = Dauer)
- `PlaybackEnded_MarksEpisodeWatchedInDetails` / `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` / `PlaybackEnded_AlreadyWatchedEpisode_KeepsServerTimestamp` / `PlaybackEnded_Movie_DoesNotTouchEpisodeList` / `PlaybackEnded_UnknownEpisode_LeavesDetailsUnchanged` — „angesehen"-Übertrag in die Detailansicht
- `PlaybackEnded_AutoResume_StillMarksFinishedEpisode` — Abschlussmeldung + Auto-Fortsetzung gemeinsam
- `PlaybackFailed_ShowsErrorAndFallsBack` — Fehler-Rückfall Serienpfad
- `PlayerClosed_ReportsFinalAndResets` — `PlayerClosedMessage` erzeugt `ReportPlaybackProgressEffect` mit letzter Position + PiP-Rücksprung + `PlayerState.Empty`
- `PlayerClosed_WithoutPosition_ResetsOnly` — ohne Position keine Meldung
- `PlayRequested_*`, `PlaybackSourceResolved_*`, `PictureInPicture_*`, `Logout_ResetsPlayer` — übrige Player-Pfade

### `AppUpdateTests_PlaylistPlayback`
Datei: `VideoWebPlayer.Maui.Tests/AppUpdateTests_PlaylistPlayback.cs` — Playlist-Wiedergabekontext (Issue #43, Schritt 5).

- `PlaylistPlaybackStartRequested_DispatchesStartEffect` / `…_InvalidId_IsIgnored` — `StartPlaylistPlaybackEffect`
- `PlaylistPlaybackStarted_SetsPlayerAndResolves` — belegt `PlayerState` mit `PlaylistId`/`PlaylistName`/`PlaylistEntryId`/`PlaylistPosition`/`PlaylistTotalCount` (`IsPlaylistPlayback = true`)
- `PlaylistPlaybackStarted_Error_ShowsPlayerError` — Fehlerpfad
- `PlaylistEntrySelected_Playable_StartsPlaylistPlayback` / `…_NonPlayableOrInaccessible_OpensDetails` — Eintragsauswahl
- `ContinueWatchingSelected_WithPlaylist_StartsPlaylistPlayback` / `…_WithoutPlaylist_KeepsSeriesPath` / `…_PlaylistWithoutEntry_KeepsSeriesPath` — Auswahl-Guard playlist-gebundener Weiterschauen-Einträge (`PlaylistId` + `PlaylistEntryId` > 0)
- `NextRequested_InPlaylist_LoadsNextEntry` / `PreviousRequested_InPlaylist_LoadsPreviousEntry` / `NavigationRequested_WithoutPlaylistOrWhileResolving_NoEffect` — manuelle Navigation
- `NavigationCompleted_SwitchesMediaAndResolves` / `…_NoEntry_ExhaustsForward` / `…_NoEntryBackward_KeepsForward` / `…_Error_ShowsError` / `…_StaleIdentity_IsDiscarded` — Navigationsergebnis inkl. `PlaylistEntryId`-Wechsel und Stale-Guard
- `PlaybackEnded_InPlaylist_AdvancesPlaylist` — Ende → `AdvancePlaylistEntryEffect`; **Kommentar belegt: die Abschluss-`ReportPlaybackProgressEffect` läuft neben dem Advance mit** (prüft derzeit nur den Advance-Effekt, nicht die Playlist-Felder der Meldung — die Felder existieren noch nicht)
- `PlaybackEnded_InPlaylist_AutoResumeOff_NoAdvance` / `…_MidPlayback_IsIgnored`
- `PlaybackProgress_NearEnd_InPlaylist_AdvancesPlaylist` / `…_WhileResolving_NoSecondAdvance` — Takt-Auslösung des Advance
- `PlaybackFailed_InPlaylist_AdvancesPlaylist` / `SourceResolveFailed_InPlaylist_AdvancesPlaylist` / `PlaybackFailed_InPlaylist_Exhausted_KeepsErrorState` — Fehler-Rückfälle
- `PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance` — Serienpfad unverändert
- Hilfsmethoden: `Connected()`, `StartInfo(…)`, `NavItem(…)`, `PlayingInPlaylist(…)` — Testdaten-Fixture für den Playlist-Kontext (`PlaylistId = 7`, `PlaylistEntryId = 5`)

### `VideoPlayerApiClientTests_Endpoints`
Datei: `VideoWebPlayer.Maui.Tests/VideoPlayerApiClientTests_Endpoints.cs` — Endpoint-URL-/Methoden-Contract-Tests.

- `Endpoint_UsesExpectedMethodAndPath` (Theory, `MemberData EndpointCases`) — Fall `Progress`: `ReportPlaybackProgressAsync("movie", 7, 120, 3600)` → `POST /api/continue-watching/progress` (prüft Methode/Pfad/Gate-Header, **nicht** den Request-Body)
- `GetContinueWatchingAsync_DeserializesEntries` — Deserialisierung der Weiterschauen-Einträge

### `ProgressReportServiceTests`
Datei: `VideoWebPlayer.Maui.Tests/ProgressReportServiceTests.cs` (`[Collection("SqliteDatabase")]`)

- `ReportProgress_Online_SendsDirectlyWithoutQueueing` — Direktversand; prüft den Request-Body (`MediaType`, `MediaId`, `PositionSeconds`, `DurationSeconds` — PascalCase) und leere Queue
- `ReportProgress_Offline_EnqueuesWithoutRequest` — Offline → `pending_progress`-Eintrag (VideoType/VideoId/PositionTicks/DurationTicks), kein Request
- `ReportProgress_WithoutClient_Enqueues` / `…_TransportError_Enqueues` — Einreihen ohne Client bzw. nach Retries
- `ReportProgress_PermanentClientError_Discards` — 404 → verworfen
- `ReportProgressAsync_QueueAndDiscard_Logged` — deduplizierte Protokolleinträge
- `ReportProgress_EnqueueFailure_LogsWarningOncePerMedia` — Schreibfehler der Queue
- Hilfsklassen/-methoden: `FakeNetworkMonitor` (`INetworkStatusMonitor`), `LoginAsync()`, `CreateService()` — reale `BackupDatabase` + `PendingProgressSyncService` auf Temp-Verzeichnis

### `ServerSyncServiceTests`
Datei: `VideoWebPlayer.Maui.Tests/ServerSyncServiceTests.cs`

- `SynchronizeAsync_SendsPendingThenRefreshesCaches` — Flush vor Cache-Refresh; zwei `POST /api/continue-watching/progress`-Requests
- `SynchronizeAsync_WritesResultCounters` / `…_WithoutClient_IsNoOp` / `…_PartialFailure_ReportsFalseButKeepsGoing`
- `FlushPendingProgress_LogsSentCount_OnlyWhenSent` / `…_TransportError_KeepsRemainingEntries` / `…_PermanentError_DiscardsEntryAndContinues` — Nachsende-Semantik der Warteschlange
- `SyncContinueWatching_RefreshesOnlyThatCache`

### `PendingProgressSyncServiceTests`
Datei: `VideoWebPlayer.Maui.Tests/PendingProgressSyncServiceTests.cs`

- `Enqueue_StoresPositionAndDuration` — persistierte Felder des Eintrags
- `Enqueue_SameMedia_KeepsOnlyLatestPosition` — Deduplikation je Medium
- `GetPending_OrdersChronologically` / `RemoveForMedia_RemovesOnlyThatMedia` / `Enqueue_Remove_LoggedAggregated` / `Pending_SurvivesDatabaseReopen` (Persistenz über DB-Neuöffnung)

### Contract-/Neben-Tests
- `PlayerContractTests` (`VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`): `EffectHandler_HandlesPlayerEffects` — quelltextbasierter Contract-Test: `ShellAppEffectHandler` verdrahtet `ReportPlaybackProgressEffect` und nutzt `ProgressReportService` (kein zweites Warteschlangensystem)
- `NetworkSyncContractTests`: `ProgressReportService`-Registrierung im DI (`MauiProgram`)
- `OfflineContinueWatchingServiceTests`: u. a. `ContinueWatching_Progress_FromPendingOutbox` — die Offline-Weiterschauen-Liste liest `pending_progress` als Fortschrittsquelle
- `ContinueWatchingEventIngressTests`, `ContinueWatchingMutationServiceTests`, `ContinueWatchingContextMenuContractTests` — Weiterschauen-Nebenpfade (nicht direkt betroffen)

### E2E-Tests (nicht ausgeführt, siehe Testlücken)
Datei: `VideoPlayer.E2E.Tests/Tests/`

- `PlaylistPlaybackTests` (6 Tests): `PlayButton_StartsPlaybackAtFirstPlayableEntry`, `SelectEntry_StartsPlaybackAtThatEntry`, `NextAndPreviousButtons_NavigatePlaylist`, `PlaybackEnd_AdvancesToNextPlaylistEntry`, `StreamFailure_AdvancesToNextPlaylistEntry`, `ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries`
- `ContinueWatchingPlaybackTests` (3): Titel-Detailansicht + Wiedergabe ab Fortsetzungsposition, Folge-Episode, nicht abspielbarer Eintrag
- `PlaybackTests` (2): Wiedergabestart aus „Neu im Programm", Stream-Fehlschlag
- `ContinueWatchingContextMenuTests` (3): Ausblenden/Überspringen

## Hilfsmethoden / Test-Infrastruktur

### `Stubs` (VideoWebPlayer.Maui.Tests/Stubs/)
- `StubHttpMessageHandler` — sequenzielle/gefilterte Antworten und aufgezeichnete Requests (`Requests` mit `PathAndQuery`, `Body`, `Authorization`, `ApiKey`); `Enqueue`, `EnqueueJson`, `EnqueueException`, `EnqueueAsyncResponder`
- `ApiClientTestFactory` — `CreateClient(handler)` für `VideoPlayerApiClient` (Test-Gate-Key `test-api-key`)
- `InMemorySettingsStore` / `InMemorySecretStore` — In-Memory-Ablagen für `AppSecretsService`
- `RecordingDelays` — sofortige Retry-Verzögerungen
- `RecordingLoggerProvider` / `RecordedLogEntry` — Log-Assertions (Level/Category/Message)
- `SqliteDatabaseCollection` / `ClientGateCollection` — xUnit-Collections (SQLite-Seriellisierung, statischer Gate-Schlüssel)
- `RepositoryRoot` — Quelltext-Zugriff der Contract-Tests

### `VideoPlayer.E2E.Tests/Support/`
- `MockVideoServer` — lokaler TCP-Mock der Server-API; `POST /api/continue-watching/progress` ist eine reine 204-Senke ohne Body-Auswertung; Opt-in-Felder `ContinueWatchingPlaylistId`/`PlaylistName`/`PlaylistEntryId` liefern playlist-gebundene Weiterschauen-Einträge
- `E2ETestBase` / `AppFixture` / `AppInstance` / `DevApiClient` / `UiDriver` — Debug-Build, App-Start mit isoliertem Profil, `/model`-Polling, UI-Automation
