# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Konfiguration / Testinfrastruktur

- [x] `PackageReference` `Microsoft.Extensions.Logging.Abstractions` 10.0.10 in `VideoPlayer.Mvu.csproj` — vorhanden (Zeile 13)
- [x] `RecordedLogEntry` (Datenmodellklasse) — angelegt in `VideoWebPlayer.Maui.Tests/Stubs/RecordedLogEntry.cs` (`Level`, `Category`, `Message`, `Exception`)
- [x] `RecordingLoggerProvider` (`ILoggerProvider` + `ILoggerFactory`-Ersatz) — angelegt in `VideoWebPlayer.Maui.Tests/Stubs/RecordingLoggerProvider.cs`

### `VideoPlayer.Storage`

- [x] `PendingProgressSyncService`: optionaler `ILogger<PendingProgressSyncService>?`-Parameter, `NullLogger`-Normierung; `EnqueueAsync` schreibt `Information` beim Zustandswechsel (nur neue Einreihung, Ersetzen im Takt still)
- [x] `CacheDatabase`: optionaler `ILogger<CacheDatabase>?`-Parameter; `Debug.WriteLine` im Recovery-Pfad (`InitializeWithRecoveryAsync`) durch `LogWarning` mit Exception ersetzt
- [x] `BackupDatabase`: optionaler `ILogger<BackupDatabase>?`-Parameter; `Debug.WriteLine` im Recovery-Pfad durch `LogWarning` mit Exception ersetzt

### `VideoPlayer.Mvu`

- [x] `DeviceTokenService`: `ILogger<DeviceTokenService>?` an beiden Konstruktoren; alle 6 `Debug.WriteLine` ersetzt; Token-Verwerfen als `Information` (`DiscardCurrentAsync`/`DiscardIfCurrentAsync`), Gate-Abweisung als `Warning` (`NotifyRejected`, `IsGateRejectedAsync`-Fehlerpfad)
- [x] `AuthSessionService`: `ILoggerFactory? loggerFactory = null` am längsten Konstruktor; Convenience-Konstruktoren reichen die Fabrik durch inkl. `CreateLogger<DeviceTokenService>()` beim internen `new DeviceTokenService`; `Information`-Einträge für Anmeldeergebnis (`AuthAttemptStatus`, nur `{ServerHost}` — keine Zugangsdaten), stille Neuanmeldung, Abmeldung; `Warning` bei `SessionExpired`; alle 9 `Debug.WriteLine` ersetzt
- [x] `AuthSessionService.CreateClient` sowie die kurzlebigen Clients in `TryRefreshSessionAsync`, `SilentReloginAsync` und `RevokeRefreshTokenAsync` verdrahten `VideoPlayerApiClientOptions.Logger = loggerFactory?.CreateLogger<VideoPlayerApiClient>()`
- [x] `ServerSyncService`: `ILogger<ServerSyncService>?`-Parameter; `Information`-Eintrag mit den `SyncResult`-Zählern (`SentProgressReports`, `RemainingProgressReports`, Sync-Flags) in `SynchronizeAsync`; `FlushPendingProgressAsync` protokolliert Nachsende-Ergebnis; `Debug.WriteLine` ersetzt
- [x] `ProgressReportService`: `ILogger<ProgressReportService>?`-Parameter; Verwerfen bei `IsPermanentClientError` als `Warning` (einmal je Medium via `LogOutcomeOnce`-Deduplizierung); Einreihen wird als `Warning` „wird eingereiht" bzw. `Information` auf der Warteschlangen-Schicht protokolliert; `Debug.WriteLine` ersetzt
- [x] `ServerEventsService`: `ILogger<ServerEventsService>?`-Parameter; `ConnectAsync`/`DisconnectAsync`/`OnStatusChanged`/`ConnectionClosed` als `Information` (`HubPath` ohne Token); Fehlerpfade als `Warning`; alle `Debug.WriteLine` ersetzt
- [x] `DownloadService`: `ILogger<DownloadService>?` am öffentlichen und am `internal`-Testkonstruktor; `EnqueueAsync` mit Medientyp/-Id + Anzeigetitel + `Notice` (`Information`, Fehler-Notice `Warning`); Zustandswechsel in `SetStateAsync` mit `DownloadListItem.Title` (`Information`/`Warning` bei `Failed`); `DeleteAsync`, `ResumeInterruptedAsync` (aggregiert); alle 7 `Debug.WriteLine` ersetzt
- [x] `AutoDownloadService`: `ILogger<AutoDownloadService>?` an beiden Konstruktoren; `RunAsync` protokolliert Lauf-Ergebnis (`{EnqueuedCount}`) und Throttle-Auslassung; `EnqueueNextEpisodesAsync`/`EnqueueNextPlaylistEntriesAsync` aggregiert; `Debug.WriteLine` ersetzt
- [x] `PlaybackSourceService`: `ILogger<PlaybackSourceService>?` an beiden Konstruktoren; `ResolveAsync` protokolliert Medientyp/-Id + `PlaybackSourceKind` + `ResumePosition` — `Location`/`access_token` wird niemals protokolliert; `Debug.WriteLine` ersetzt
- [x] `MediaDetailsService`: `ILogger<MediaDetailsService>?`-Parameter; `LoadAsync`-Fehlerpfade als `Warning`; `ShowDetailsCache`-Aufrufe reichen den Logger durch
- [x] `HomeListService`: `ILogger<HomeListService>?`-Parameter; 5 `Debug.WriteLine`-Fehlerpfade als `Warning`
- [x] `OfflineContinueWatchingService`: `ILogger<OfflineContinueWatchingService>?` an beiden Konstruktoren; `Debug.WriteLine` ersetzt; `ShowDetailsCache.ReadAsync` mit Logger
- [x] `UpcomingEpisodesService`: `ILogger<UpcomingEpisodesService>?`-Parameter; `Debug.WriteLine` ersetzt; `ShowDetailsCache`-Aufrufe mit Logger
- [x] `ShowDetailsCache` (statisch): `ILogger? logger = null` an `WriteAsync`/`ReadAsync`; beide Fehlerpfade als `logger?.LogWarning(ex, …)` mit Cache-Schlüssel
- [x] `ProfileInfoService`: `ILogger<ProfileInfoService>?`-Parameter; `Debug.WriteLine` → `LogWarning` (keine Profildaten im Eintragstext)
- [x] `ContinueWatchingMutationService`: `ILogger<ContinueWatchingMutationService>?`-Parameter; `HideAsync`/`SkipAsync` protokollieren Ergebnis (`Information`/`Warning`)

### `VideoPlayer.Maui`

- [x] `App`: erforderlicher `ILogger<App>`-Parameter; Start-Workflow-Eintrag in `RunStartupAsync` (`Information`); `Debug.WriteLine` → `LogError` im Fehlerpfad
- [x] `AppShell`: erforderlicher `ILogger<AppShell>`-Parameter; 5 `Debug.WriteLine` → `LogWarning`/`LogError`; `Information`-Eintrag beim Quellenwechsel (Quellname)
- [x] `PlayerPage`: erforderlicher `ILogger<PlayerPage>`-Parameter; `OnMediaOpened` (`Information` mit Titel aus `mvuHost.Model.Player.Title` + Fortsetzung), `OnMediaEnded` (`Information`), `OnMediaFailed`/`FailPlaybackStart` (`LogError` mit `logMessage`, bereinigt über `SanitizeForLog` — `access_token` wird maskiert), `ApplyResumeSeekAsync` (`Information`/`Warning`); Fortschrittstakt-Catches als `Warning`; kein 5-s-Takt-Eintrag
- [x] `SettingsPage`: erforderlicher `ILogger<SettingsPage>`-Parameter; `DispatchSaveAsync`-Catch → `LogWarning`
- [x] `ServerSetupPage`: erforderlicher `ILogger<ServerSetupPage>`-Parameter; QR-Scan-Start-Fehlschlag → `LogWarning`
- [x] `ServerHealthProbe`: erforderliche `ILogger<ServerHealthProbe>`-/`ILoggerFactory`-Parameter; `CheckAsync` protokolliert Ergebnis (`Information` mit Host + `HealthCheckResult`, keine Credentials) und Fehlschläge (`Warning`); `apiClientLogger` wird in `VideoPlayerApiClientOptions.Logger` durchgereicht
- [x] `SignalRServerEventsConnection`: erforderlicher `ILogger<SignalRServerEventsConnection>`-Parameter; Reconnect-Verlust `Warning`, Wiederherstellung `Information`, Verbindungsaufbau-Fehlschlag `Warning`; Hub-Ereignisse als `LogDebug` (siehe Hinweise)
- [x] `ConnectivityNetworkStatusMonitor`: erforderlicher `ILogger<ConnectivityNetworkStatusMonitor>`-Parameter; `AvailabilityChanged`-Auslösung als `LogInformation` (Online/Offline + WLAN-Kontext)
- [x] `PosterImageService`: erforderlicher `ILogger<PosterImageService>`-Parameter; 3 `Debug.WriteLine` → `LogWarning`; keine Erfolgseinträge pro Bildabruf
- [x] `PictureInPictureService`: erforderlicher `ILogger<PictureInPictureService>`-Parameter; `ToggleAsync`/`RestoreAsync` als `Information` (Ergebnis); Fehlerpfad `Warning`
- [x] `SecureStorageSecretStore`: erforderlicher `ILogger<SecureStorageSecretStore>`-Parameter; Lesefehlschlag `LogWarning` (Schlüsselname ohne Wert)
- [x] `MauiProgram`: `Func<IServerEventsConnection>`-Lambda auf `sp`-Parameter umgestellt (`sp => () => new SignalRServerEventsConnection(sp.GetRequiredService<ILogger<SignalRServerEventsConnection>>())`); `ISecretStore`-Lambda reicht `ILogger<SecureStorageSecretStore>` durch
- [x] `ShellAppEffectHandler`-Breadcrumbs: `LogInformation` an den Handler-eigenen Stellen (Ablage-Initialisierung `„Ablage initialisiert (Wiederherstellung: {Recovered})"`, Wiedergabestart mit Medientyp/-Id + Anzeigetitel + `PlaybackSourceKind`, Teil-Synchronisation, Teilen-Fallback des Protokollversands); alle übrigen Effekt-Einträge liegen auf der jeweiligen Dienst-Schicht (siehe Hinweise — bewusste Schicht-Konsolidierung aus dem Code-Review)
- [x] `ShellAppEffectHandler`-Ereignishandler: `SessionExpired` → `Warning` in `AuthSessionService.OnSessionExpired`; `DeviceTokenRejected` → `Warning` in `DeviceTokenService.NotifyRejected`; `AvailabilityChanged` → `Information` in `ConnectivityNetworkStatusMonitor` (Delegationsmuster „eine Schicht pro Ereignis", per Contract-Test gepinnt)
- [x] `Debug.WriteLine`-Umstellungsumfang: kein Vorkommen mehr in `VideoPlayer.Mvu`, `VideoPlayer.Maui`, `VideoPlayer.Storage` (per Suche verifiziert); Restbestand nur in `VideoWebPlayer.Maui` (Altbestand, ausgenommen), `VideoPlayer.ApiClient`-Discovery und `msTools.DeveloperApi` (außerhalb Geltungsbereich)

### Tests

- [x] `AuthSessionServiceTests.LoginAsync_WritesEntries_WithoutCredentials` — vorhanden (Zeile 290)
- [x] `AuthSessionServiceTests.CreateClient_WiresApiClientLogger` — vorhanden (Zeile 311)
- [x] `AuthSessionServiceTests.LogoutAsync_WritesEntry` — vorhanden (Zeile 333)
- [x] `ServerSyncServiceTests.SynchronizeAsync_WritesResultCounters` — vorhanden (Zeile 143)
- [x] `ProgressReportServiceTests.ReportProgressAsync_QueueAndDiscard_Logged` — vorhanden (Zeile 160)
- [x] `ServerEventsServiceTests.ConnectAsync_DisconnectAsync_StatusChanged_Logged` — vorhanden (Zeile 150)
- [x] `DownloadServiceTests.EnqueueAsync_TransferOutcome_Logged` — vorhanden (Zeile 151)
- [x] `PlaybackSourceServiceTests.ResolveAsync_LogsKind_NotLocation` — vorhanden (Zeile 282)
- [x] `PendingProgressSyncServiceTests.Enqueue_Remove_LoggedAggregated` — vorhanden (Zeile 115)
- [x] `AutoDownloadServiceTests.RunAsync_LogsOutcome` — vorhanden (Zeile 640)
- [x] `LogProtocolContractTests.ConnectivityMonitor_LogsAvailabilityChange` — vorhanden (Zeile 116)
- [x] `LogProtocolContractTests.PictureInPictureService_LogsToggleRestore` — vorhanden (Zeile 135)
- [x] `LogProtocolContractTests.PlayerPage_LogsPlaybackFailure` — vorhanden (Zeile 158)
- [x] `LogProtocolContractTests.ShellAppEffectHandler_LogsLogoutBreadcrumb` — vorhanden (Zeile 196)
- [x] `LogProtocolContractTests.NoDebugWriteLine_RemainsInAppSources` — vorhanden (Zeile 216)

### E2E-Tests

- [x] `LogProtocolTests.LoggingEnabled_RealFlows_WriteDiagnosticEntries` (Pflicht) — vorhanden (Zeile 171): Protokollierung an → Neustart mit selbem Profil → Auto-Anmeldung + Wiedergabe → `[INFO]`-Facheintrag, ohne `access_token`/Benutzername/Passwort
- [x] `LogProtocolTests.LoggingEnabled_LogoutAndPipSubFlows_WriteDiagnosticEntries` (optional, Task #55) — vorhanden (Zeile 263): PiP-Umschaltung + Abmeldung über die UI → Einträge in der Protokolldatei, ohne Geheimnisse
- [x] `LogProtocolTests.LoggingEnabled_PlaybackFailureSubFlow_WritesErrorEntry` (optional, Task #55) — vorhanden (Zeile 330): `MockVideoServer.PlaybackStreamFails = true` → `[ERROR]`-Eintrag ohne `access_token`

## Hinweise

- **Schicht-Konsolidierung der Breadcrumbs (bewusste Abweichung vom Planwortlaut):** Der Plan sah `LogInformation`-Einträge *in den Ausführungsmethoden des `ShellAppEffectHandler`* vor. In Iteration 2 wurde das als Code-Review-Befund auf „eine Schicht pro Ereignis" konsolidiert: Der Handler protokolliert nur, wo er eigenen Kontext besitzt (Wiedergabestart mit `Player.Title`, Teil-Sync, Ablage-Initialisierung, Teilen-Fallback); Anmeldung, Sync-Zähler, Ereignisverbindung, Download-Zustände, Abmeldung, PiP und Netzwechsel werden auf der jeweiligen Dienst-Schicht protokolliert (`AuthSessionService`, `ServerSyncService`, `ServerEventsService`, `DownloadService`, `PictureInPictureService`, `ConnectivityNetworkStatusMonitor`, `DeviceTokenService`). Jeder geplante Eintrag entsteht weiterhin — die Contract-Pins wurden entsprechend invertiert (z. B. `Assert.DoesNotContain("LogInformation", body)` im `LogoutAsync`-/`OnNetworkAvailabilityChanged`-Rumpf bei vorhandenem Dienst-Eintrag). Fachlich erfüllt; als Abweichung zum Planwortlaut dokumentiert.
- **`SignalRServerEventsConnection`:** Hub-Ereignisse (`ContinueWatchingUpdated`, `FavoritesChanged`, `NewVideosScanned`, `Closed`, Connect/Disconnect-Erfolg) werden per `LogDebug` protokolliert statt `Information` — begründet im Code damit, dass `ContinueWatchingUpdated` im Fortschrittstakt feuern kann und den FileLogger (Mindestschwelle `Information`) nicht fluten soll; Reconnect-/Fehlschlag-Pfade sind `Warning`/`Information`. Deckt sich mit der Sparsamkeits-Regel des Plans.
- **`PendingProgressSyncService`/`ProgressReportService` (Log-Deduplizierung):** Einreihungs-Eintrag nur beim Zustandswechsel (neues Medium), Entfernen pro Medium bewusst still (Gesamtzahl trägt `ServerSyncService.FlushPendingProgressAsync` via `{SentCount}`); Fehler-Warnungen einmal je Medium und Outcome (`LogOutcomeOnce`). Entspricht der Ausschlussliste (kein 5-s-Fortschrittstakt) — kleiner als der Planwortlaut („Einreihen/Entfernen schreiben Anzahl-Einträge"), aber getestet und begründet.
- **Geheimnis-Schutz:** `access_token`-Maskierung in `PlayerPage.SanitizeForLog` vorhanden; `PlaybackSourceService` protokolliert `Location` nicht; Anmeldeeinträge enthalten nur `{ServerHost}` + Status. Verifiziert.
- Tests wurden in diesem Review-Durchlauf nicht ausgeführt (reine Code-Prüfung gegen den Plan); die Tasks-Datei nennt bei Task #55 „beide bestanden".
- Die Tasks-Datei (`0fe871d252fd4670b6b1427e123148b2-protokollierung-tasks.md`) wurde geprüft: Alle 55 Aufgaben tragen zutreffend den Status `Erledigt` mit korrekten Testnachweisen — keine Aktualisierung erforderlich.
