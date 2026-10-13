# Tests

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-10-13, ca. 21:15–21:25 MESZ (UTC+02:00)
- **Branch und Commit-ID:** `task/issue-49-8320ea2ee24347a3b7ecbb210062e168-rueckmeldungen-quellen-browsin`, Commit `086558f413fbe6cf7cbe5b4f9f4baf039e8fbe49` („Protokollierung (#48)")
- **Uncommittete Änderungen im getesteten Stand:** nur das untracked Verzeichnis `docs/projects/task/` (die `requirement.md` dieses Auftrags). Keine Änderungen an Produktivcode, Tests oder Testkonfiguration.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows 11 (Microsoft Windows NT 10.0.26200.0), .NET SDK **10.0.401** (`dotnet --version`), installierte Workloads: `android`, `ios`, `maccatalyst`, `maui-windows` (jeweils 10.0.100-Manifest). Interaktive Konsolensitzung (Benutzer `Martin`, Session 1, Status Aktiv). `dotnet test` lokal ausgeführt, keine CI-Umgebung.
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `VideoWebPlayer.Maui.Tests` (`VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`, Target `net10.0`, xUnit v3 3.2.2, Microsoft.NET.Test.Sdk 18.8.1) — Unit- und Contract-Tests des MVU-Kerns, der Dienste, des API-Clients und (textbasiert, ohne UI-Instanziierung) der MAUI-Schicht. CI-Referenzbefehl aus `.github/workflows/pr-checks.yml`: `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --configuration Release` (nach vorausgehenden Build-Schritten). Einziges in der PR-CI ausgeführtes Testprojekt.
  - `VideoPlayer.E2E.Tests` (`VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj`, Target `net10.0-windows`, xUnit v3 + FlaUI.UIA3 5.0.0) — echte UI-Automatisierung gegen die gestartete `VideoPlayer.Maui.exe` (Fixture `AppFixture` baut die App einmalig als Windows-Debug-Build, `AppInstance` startet sie mit isoliertem Profil, `MockVideoServer` + Developer-API). Nicht Teil der PR-CI.
  - Weitere Testprojekte sind in `VideoPlayer.App.sln` nicht vorhanden (Projektliste: VideoWebPlayer.Client, VideoWebPlayer.Maui, VideoWebPlayer.Maui.Tests, VideoPlayer.ApiClient, VideoPlayer.Mvu, VideoPlayer.Maui, VideoPlayer.Storage, VideoPlayer.E2E.Tests, msTools.DeveloperApi, msTools.DeveloperApi.Maui).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| T1 (Ausgangslauf) | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release --logger "trx;LogFileName=VideoWebPlayer.Maui.Tests-baseline.trx" --results-directory docs/projects/task/issue-49-8320ea2ee24347a3b7ecbb210062e168-rueckmeldungen-quellen-browsin/inventory/test-results` | Repo-Root | 0 | 1281 | 0 | 0 | [Log](test-results/dotnet-test-VideoWebPlayer.Maui.Tests.log) · [TRX-Report](test-results/VideoWebPlayer.Maui.Tests-baseline.trx) |

Hinweise zu T1: Kompilierung in Release erfolgreich; vorhandene Compiler-Warnungen (CS0108, CS8618) ausschließlich in `VideoWebPlayer.Client` (DTO-Vererbung/Nullable-Initialisierung), keine Fehler. Testdauer ~35 s.

### Nachgewiesene bestehende Testfehler

Keine — der dokumentierte Ausgangslauf T1 endete mit 0 fehlgeschlagenen Tests (1281/1281 erfolgreich). Damit existiert für keinen Testfall ein „preexisting"-Nachweis.

### Testlücken und Ausführungsprobleme

- **`VideoPlayer.E2E.Tests` — nicht ausgeführt.** Umfang: 13 Testklassen (`BootstrapTests`, `ContinueWatchingContextMenuTests`, `ContinueWatchingPlaybackTests`, `LogProtocolTests`, `LoginTests`, `PairingTests`, `PlaybackTests`, `PlaylistManagementTests`, `PlaylistPlaybackTests`, `PlaylistsTests`, `SeriesDetailsTests`, `ServerSetupTests`, `ServerSwitchTests`), Anzahl der Einzeltests unbekannt. Grund: Die Suite treibt die echte `VideoPlayer.Maui.exe` per FlaUI-UIA3-UI-Automation — sie benötigt eine laufende App-Instanz mit sichtbarem, fokussierbarem Fenster auf dem Benutzerdesktop (die Collection-Fixture baut vorab den kompletten Windows-Debug-Build der MAUI-App, Timeout-Budget 10 min allein für den Build, plus Fensterstarts je Test). Ein unbeaufsichtigter Lauf aus dem Agenten-Prozesskontext würde reale Fenster in der Benutzersitzung öffnen und den Fokus stehlen; zudem ist die Suite nicht Teil der PR-CI und liefert keinen CI-seitig abgesicherten Baseline-Wert. Für diese Bestandsaufnahme wurde auf die Ausführung verzichtet — die Suite gilt als nicht ausgeführt, nicht als bestanden/fehlgeschlagen.
- **Kein Infrastruktur-/Build-Fehler** bei T1; alle lokal ausführbaren Tests der Unit-Suite wurden einmalig und vollständig ausgeführt.
- **Abdeckungslücke der lokalen Suite (fachlich):** Die MAUI-UI-Schicht (`VideoPlayer.Maui`) ist im Testprojekt nicht instanziierbar — UI-Verhalten wird nur über textbasierte Contract-Tests (Quelltext-Prüfung) abgesichert (vgl. Kommentar `SourcesContractTests.cs:5-11`; dessen Aussage, UI-End-to-End-Automatisierung sei im Repository nicht vorhanden, ist veraltet — die FlaUI-Suite `VideoPlayer.E2E.Tests` existiert, wurde aber wie oben dokumentiert nicht ausgeführt). Die anforderungsrelevanten Oberflächendefekte FA-1 (Sichtbarkeit der Genre-Leiste), FA-2 (Überschreiben der Sucheingabe durch `UpdateUi`-Ticks), FA-3 (Scrollsprung durch `ItemsSource`-Referenzwechsel) und FA-5 (ungeprüftes `MediaEnded` → Auto-Vorlauf) sind im Ist-Stand durch keinen Test gepinnt — kein bestehender Test prüft ein `IsVisible`-Verhalten der `GenresCollection`, einen Schutz der Sucheingabe gegen Modell-Ticks, die Erhaltung der Scrollposition oder einen Plausibilitätscheck vor `PlaybackEndedMessage`.

## Testklassen

Auflistung der für die fünf Mängel relevanten Testklassen der lokalen Suite (`VideoWebPlayer.Maui.Tests/`). Die `AppUpdateTests_*`-Dateien sind Partial-Klassen des gemeinsamen `AppUpdateTests`-Scopes (Muster der übrigen Feature-Tests).

### `AppUpdateTests_Sources` (`AppUpdateTests_Sources.cs`)

Quellenliste + Quellen-Browsing (FA-1 bis FA-3):

- `EnterConnected_StartsSourcesLoad`, `SynchronizationCompleted_WhenConnected_ReloadsSources`, `SynchronizationCompleted_WhileSourcesLoading_NoDuplicate`, `CachedListsChanged_WhenConnected_ReloadsSources`, `CachedListsChanged_WhileSourcesLoading_NoDuplicate`, `CachedListsChanged_WhenNotConnected_NoSourcesLoad` — Auslöser/Deduplizierung des Quellen-Ladens
- `SourcesLoaded_WhenNotConnected_IsIgnored` (Theory), `SourcesLoaded_SetsList_AndEmptyIsDistinguishable`, `SourcesLoaded_Error_SetsErrorState`, `SourcesLoaded_Error_KeepsExistingSources`, `SourcesLoaded_Success_ReplacesExistingSources` — Quellenliste im Modell
- `SourcesReloadRequested_WhenConnected_ReloadsAndClearsError`, `SourcesReloadRequested_WhenNotConnected_IsIgnored` (Theory), `SourcesReloadRequested_WhileLoading_NoDuplicate` — Retry-Pfad
- `SourceSelected_NavigatesAndStartsInitialLoad` — Browser-Initialisierung + Navigation + Genre-/Erstseiten-Ladung
- `SourceBrowserOpened_SameSource_KeepsState`, `SourceBrowserOpened_Restore_LoadsGenresAndFirstPage` — Rückkehr-/Wiederherstellungspfad
- `SourceGenresLoaded_WrongSource_IsDiscarded` — FA-1: Genres-Antwort verlassener Quelle wird verworfen
- `GenreSelected_ReloadsFromFirstPage_WithGenreFilter` — FA-1: Genre-Auswahl löst Neu-Laden mit `GenreId` ab Seite 0 aus
- `SearchSubmitted_ReloadsFromFirstPage_WithSearchText`, `SearchSubmitted_SameText_NoReload` — FA-2: Abschicken setzt `SearchText` und lädt neu; identischer Text kein Neuladen
- `NextPageRequested_LoadsAppendedPage`, `NextPageRequested_NoMoreOrBusy_IsIgnored` — FA-3: Nachlade-Trigger und Guards
- `ItemsLoaded_Append_ConcatenatesAndAdvancesPage`, `ItemsLoaded_StaleAppend_IsDiscarded`, `ItemsLoaded_AppendError_KeepsExistingItems` — FA-3: Anhänge-Pfad (`Concat`), Verwerfen veralteter Seiten, Fehlerverhalten
- `Retry_OnEmptyError_ReloadsFromScratch`, `Retry_WithItems_RetriesPendingNextPage` — Retry-Pfade
- `MediaItemSelected_NavigatesToDetails` — Rasterauswahl → Detail-Navigation

### `AppUpdateTests_Player` (`AppUpdateTests_Player.cs`)

Wiedergabe-Flow und Ende-Erkennung (FA-5, FA-4-Startpfad):

- `PlayRequested_ValidMovie_SetsPlayerStateAndEffects`, `PlayRequested_Episode_BuildsUpcomingEpisodes`, `PlayRequested_LastEpisode_HasNoUpcoming`, `PlayRequested_InvalidIdentity_Unchanged` — `StartPlayback`-Pfad inkl. `NextEpisodeQueue`
- `PlaybackSourceResolved_Success_SetsSource`, `..._Error_FallsBackToNextEpisode`, `..._ErrorWithoutNext_StaysOnError`, `..._StaleResponse_Discarded` — Quellauflösung + Fehler-Rückfall
- `PlaybackProgress_ReportsChangedPosition`, `..._UnchangedPosition_NoReport`, `..._MarksWatchedWithinWindow`, `..._AutoResumeAdvancesWhenEnabled`, `..._AutoResumeDisabled_DoesNotAdvance`, `..._AboveResumeWindow_DoesNotAdvance`, `..._PausedWithinWindow_DoesNotAdvance`, `..._NoNextEpisode_DoesNotAdvance` — Fortschritts-Takt, Angesehen-Fenster, Auto-Fortsetzung
- `PlaybackEnded_MarksWatchedAndAdvances`, `PlaybackEnded_WithoutNext_MarksWatchedOnly`, `PlaybackEnded_MarksEpisodeWatchedInDetails`, `PlaybackEnded_AutoResume_StillMarksFinishedEpisode`, `PlaybackEnded_AlreadyWatchedEpisode_KeepsServerTimestamp`, `PlaybackEnded_Movie_DoesNotTouchEpisodeList`, `PlaybackEnded_UnknownEpisode_LeavesDetailsUnchanged` — FA-5: bestehender ungeprüfter Abschluss-Fluss (die Tests pinnen das Ist-Verhalten; kein Test erzeugt ein „verfrühtes" Ende-Event, da `PlaybackEndedMessage` parameterlos ist und `UpdatePlaybackEnded` nur `IsActive` prüft)
- `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` — Angesehen-Fenster im Takt
- `PlaybackFailed_ShowsErrorAndFallsBack` — Fehler-Rückfall
- `PlayerClosed_ReportsFinalAndResets`, `PlayerClosed_WithoutPosition_ResetsOnly` — Seitenende
- `PictureInPicture_TogglesFlag`, `PictureInPicture_InactivePlayer_Unchanged`, `Logout_ResetsPlayer`

### `AppUpdateTests_ContinueWatching` (`AppUpdateTests_ContinueWatching.cs`)

FA-4-Selektionslogik der Weiterschauen-Liste:

- `ContinueWatchingItemSelected_Movie_StartsPlayback` — Film → Direktwiedergabe (`StartPlayback`)
- `ContinueWatchingItemSelected_Episode_StartsPlaybackWithShowLoad`, `..._ClrEpisodeType_StartsPlaybackWithShowLoad` — Episode → Direktwiedergabe + `LoadUpcomingEpisodesEffect`
- `ContinueWatchingItemSelected_EpisodeWithoutShowId_NoShowLoad` — Episode ohne `ShowId` → kein Serienkontext-Nachlauf
- `ContinueWatchingItemSelected_NonPlayable_NavigatesToDetails` (Theory) — Serie/Staffel/Kollektion → `NavigateToMediaDetailsEffect`
- `ContinueWatchingItemSelected_NonSelectable_Unchanged` (Theory) — ungültige/Platzhalter-Einträge → keine Aktion
- `UpcomingEpisodesLoaded_MatchingPlayer_SetsQueue`, `UpcomingEpisodesLoaded_StaleOrOtherMedia_Discarded` (Theory) — Nachladen der Fortsetzungsliste

### `AppUpdateTests_PlaylistPlayback` (`AppUpdateTests_PlaylistPlayback.cs`)

Playlist-Kontext (FA-4-Variante playlist-gebundener Einträge, FA-5-Auto-Vorlauf im Playlist-Pfad):

- `PlaylistPlaybackStartRequested_DispatchesStartEffect`, `..._InvalidId_IsIgnored`, `PlaylistPlaybackStarted_SetsPlayerAndResolves`, `..._Error_ShowsPlayerError`
- `PlaylistEntrySelected_Playable_StartsPlaylistPlayback` (Theory), `..._NonPlayableOrInaccessible_OpensDetails` (Theory)
- `ContinueWatchingSelected_WithPlaylist_StartsPlaylistPlayback`, `..._WithoutPlaylist_KeepsSeriesPath`, `..._PlaylistWithoutEntry_KeepsSeriesPath` — FA-4: playlist-gebundene Weiterschauen-Einträge laufen über `StartPlaylistPlaybackEffect`, nicht über `StartPlayback`
- `NextRequested_InPlaylist_LoadsNextEntry`, `PreviousRequested_InPlaylist_LoadsPreviousEntry`, `NavigationRequested_WithoutPlaylistOrWhileResolving_NoEffect`, `NavigationCompleted_*` — manuelle Playlist-Navigation
- `PlaybackEnded_InPlaylist_AdvancesPlaylist`, `..._AutoResumeOff_NoAdvance`, `PlaybackProgress_NearEnd_InPlaylist_AdvancesPlaylist`, `..._WhileResolving_NoSecondAdvance`, `PlaybackFailed_InPlaylist_AdvancesPlaylist`, `SourceResolveFailed_InPlaylist_AdvancesPlaylist`, `..._Exhausted_KeepsErrorState`, `PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance` — FA-5: Ende-/Fehler-Weiterschaltung im Playlist-Kontext (ebenfalls ohne Ende-Plausibilisierung)

### `SourceBrowserServiceTests` (`SourceBrowserServiceTests.cs`)

Dienst-Ebene Quellen-Browsing (FA-1):

- `LoadSources_MapsServerList_WithIcons`, `LoadSources_WithoutClient_Throws` — Quellenliste
- `LoadGenres_PrependsAllEntry_AndMapsServerGenres` — FA-1: „Alle"-Eintrag wird vorangestellt, Server-Genres gemappt; Endpunkt `/api/SourceGenres/{id}` verifiziert. **Kein Test für leere Serverliste oder Ladefehler des Diensts selbst.**
- `LoadItems_FirstPage_MapsSummaries_AndRequestsQuery`, `LoadItems_FullPage_ReportsHasMore`, `LoadItems_NoFilter_OmitsSearchAndGenreParameters` — `api/items`-Query mit `search`/`genreId`-Parametern
- `MapSummaryType_MapsLegacyTypes` (Theory) — Typ-Normalisierung

### `SourcesContractTests` (`SourcesContractTests.cs`)

Textbasierte Contract-Prüfung der Quellen-UI-Schicht (FA-1/FA-2/FA-3, ohne UI-Instanziierung):

- `SourceOverviewPage_HasGenreBar_Search_AndIncrementalGrid` — prüft `Model.SourceBrowser.Genres`-Binding, `SearchBar`, `Model.SourceBrowser.Items`-Binding, `RemainingItemsThresholdReached` im XAML
- `SourceOverviewPage_DispatchesBrowserMessages` — prüft Dispatches aller Browser-Nachrichten im Code-Behind
- `AppShell_*` — Quellen-Menüeinträge, Fehler-/Lade-/Leer-Zeilen, Flyout-Schließen, Templates
- `MediaDetailsPage_HasSeasonPicker_Episodes_Collection_AndFavorite`, `MediaDetailsPage_DispatchesDetailsMessages` — Detailseiten-Verdrahtung
- `Shell_RegistersSourceOverviewRoute_WithQueryParameters`, `EffectHandler_HandlesStep8Effects_AndRefreshesFavoriteCaches`, `EffectHandler_SkipsPush_WhenSameSourceOverviewIsTopmost`, `MauiProgram_RegistersStep8ServicesAndPages`, `MauiProgram_RegistersPosterImageServiceForShell`

### `HomePageContractTests` (`HomePageContractTests.cs`)

Startseite/Weiterschauen (FA-4, textbasiert):

- `HomePage_ContinueWatching_UsesLandscapeCards`, `HomePage_ContinueWatchingCarousel_DispatchesOwnMessage` — eigene Auswahl-Nachricht der Weiterschauen-Liste
- `AppUpdate_ContinueWatchingSelection_BranchesPlaybackAndDetails` — pinnt den Zweig Direktwiedergabe vs. Detailseite im Update-Code
- `HomePage_SelectionDispatchesMediaItemSelected`, `MediaCarousel_ItemsAreTappable_PostersViaCache`, `ShellAppEffectHandler_HandlesLoadUpcomingEpisodesEffect`, `MediaDetails_RouteRegistered_AndPageReadsQueryParameters`, `MauiProgram_RegistersHomeServicesAndDetailsPage` u. a.

### `MediaDetailsPageContractTests` (`MediaDetailsPageContractTests.cs`)

Detailseite (FA-4-Ziel, textbasiert): Header-Aufbau/-Verhalten, Wiederanforderung der eigenen Identität (`MediaDetailsPage_Reappearing_RequestsOwnIdentityAfterForeignDetails`), Staffel-Picker-Stabilität (`..._SeasonPicker_StableItemsSourceAndEchoGuard`), Episodenliste, Angesehen-Badge (`..._EpisodeWatchedBadge_*`, `EpisodeItem_CarriesWatchedState`), Kopfbild-Pfad.

### `PlayerContractTests` (`PlayerContractTests.cs`)

Wiedergabeseite (FA-5, textbasiert): `PlayerPage_DispatchesPlaybackMessages` (pinnt die Event-Handler-Verdrahtung inkl. `PlaybackEndedMessage` — ohne Prüfung einer Ende-Validierung), `PlayerPage_ResumeSeek_WaitsForReadyState`, `PlayerPage_MarshalsNativeElementAccessOnMainThread`, `PlayerPage_NativeAccessesAreFailureIsolated`, `EffectHandler_HandlesPlayerEffects`, `MediaDetailsPage_HasPlayButton` u. a. Kein Test prüft einen Positions-/Dauer-Check vor `PlaybackEndedMessage`.

### `ContinueWatchingContextMenuContractTests` (`ContinueWatchingContextMenuContractTests.cs`)

Kontextmenü „Ausblenden"/„Überspringen" (Issue #43, laut Anforderung unverändert zu lassen): Tap-/Long-Press-Verdrahtung, ActionSheet mit deutschen Labels, Hide/Skip-Effekte, `ContinueWatchingMutationService`.

### `OfflineContinueWatchingServiceTests` (`OfflineContinueWatchingServiceTests.cs`)

Offline-Weiterschauen-Liste (FA-4-Offenpunkt Offline-Pfad): abgeschlossene Downloads erscheinen, fehlende Dateien werden gefiltert, Platzhalter fehlender Folge-Episoden (`ContinueWatching_EpisodeWithMissingNext_ShowsPlaceholder`), Fortschritt aus lokaler Outbox/Cache.

### `UpcomingEpisodesServiceTests` (`UpcomingEpisodesServiceTests.cs`)

Serienkontext des Direktstarts (FA-4): Online-Ladung mit Cache-Auffrischung, Cache-Fallback ohne Client/bei Fehlschlag, leere Liste ohne Kontext.

### `PlaybackSourceServiceTests` (`PlaybackSourceServiceTests.cs`)

Quellauflösung (FA-4/FA-5-Umfeld): Stream-URL mit Token, Fortsetzungsposition (Server/Cache/Override), lokale Datei vs. Stream-Fallback, Offline-Verhalten.

### `VideoPlayerApiClientTests_Endpoints` (`VideoPlayerApiClientTests_Endpoints.cs`)

Endpunkt-URLs/Methoden des API-Clients (Theory über `EndpointCases`): u. a. `SourceGenres` (`GET /api/SourceGenres/5`), `SourceItems` und `SourceItemsSearchGenre` (`GET /api/items?...&search=…&genreId=…`) — FA-1-Diagnosepfad auf Client-Ebene.

## Hilfsmethoden

### `VideoWebPlayer.Maui.Tests/Stubs`

- `RepositoryRoot.Find()` — lokalisiert die Repo-Wurzel für die textbasierten Contract-Tests
- `StubHttpMessageHandler` — warteschlangenbasierter HTTP-Stub (`EnqueueJson`, `Requests`) für Client-/Dienst-Tests
- `InMemorySettingsStore`, `InMemorySecretStore` — In-Memory-Ersatz der Einstellungs-/Geheimnis-Ablage (u. a. für `AuthSessionService`)
- `RecordingLoggerProvider` / `RecordedLogEntry` — Log-Aufzeichnung für Protokollierungs-Tests
- `SqliteDatabaseCollection`, `ClientGateCollection` — xUnit-Collection-Fixtures (geteilte SQLite-Testablage bzw. Gate-Schlüssel)
- `AppProfile` (aus Produktivcode, mit Test-Profilname) — Profil-Isolation der Ablage

### `VideoPlayer.E2E.Tests/Support` (nur E2E, nicht ausgeführt)

- `AppFixture` — einmaliger Windows-Debug-Build der App je Testlauf (`E2E`-Collection, sequentiell)
- `AppInstance` — startet die App mit isoliertem Profil (`VIDEOPLAYER_PROFILE`), wartet auf `devapi.json`, stellt `DevApiClient` + FlaUI-`UiDriver`, beendet den Prozess garantiert
- `MockVideoServer` — HTTP-Mock der Server-API (inkl. Test-Video-Asset `testvideo.mp4`)
- `E2ETestBase` — gemeinsame Flüsse: `ConnectToPairingRequiredAsync`, `PairAsync`, `LoginAsync`, `LoginOnHomeAsync`, `ContinueWatchingIndexAsync`
