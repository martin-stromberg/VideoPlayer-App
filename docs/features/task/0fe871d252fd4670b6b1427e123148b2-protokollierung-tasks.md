# Tasks: Programmprotokoll mit diagnostisch verwertbarem Inhalt

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Konfiguration | `PackageReference` `Microsoft.Extensions.Logging.Abstractions` 10.0.10 in `VideoPlayer.Mvu.csproj` ergänzen | Offen | – |
| 2 | Testinfrastruktur | `RecordedLogEntry`-Datenklasse in `VideoWebPlayer.Maui.Tests/Stubs/` anlegen | Offen | – |
| 3 | Testinfrastruktur | `RecordingLoggerProvider` (aufzeichnender `ILoggerProvider`/`ILoggerFactory`-Ersatz) in `VideoWebPlayer.Maui.Tests/Stubs/` anlegen | Offen | – |
| 4 | Storage-Logging | `PendingProgressSyncService`: optionalen `ILogger<PendingProgressSyncService>?`-Parameter und aggregierte Einträge (`EnqueueAsync`, `RemoveForMediaAsync`) ergänzen | Offen | – |
| 5 | Storage-Logging | `CacheDatabase`: optionalen `ILogger<CacheDatabase>?`-Parameter ergänzen; `Debug.WriteLine` im Recovery-Pfad durch `LogWarning` ersetzen | Offen | – |
| 6 | Storage-Logging | `BackupDatabase`: optionalen `ILogger<BackupDatabase>?`-Parameter ergänzen; `Debug.WriteLine` im Recovery-Pfad durch `LogWarning` ersetzen | Offen | – |
| 7 | MVU-Logging | `DeviceTokenService`: `ILogger<DeviceTokenService>?`-Parameter an beiden Konstruktoren ergänzen; 6 `Debug.WriteLine`-Stellen ersetzen; Token-Verwerfen/Gate-Abweisung protokollieren | Offen | – |
| 8 | MVU-Logging | `AuthSessionService`: `ILoggerFactory?`-Parameter ergänzen und durch Convenience-Konstruktoren reichen (inkl. `CreateLogger<DeviceTokenService>()` beim internen `new`) | Offen | – |
| 9 | MVU-Logging | `AuthSessionService`: 9 `Debug.WriteLine`-Stellen ersetzen; `Information`-Einträge für Anmeldeergebnis (`AuthAttemptStatus`), stille Neuanmeldung, `SessionExpired`, Abmeldung | Offen | – |
| 10 | MVU-Logging | `AuthSessionService.CreateClient` (und kurzlebige Refresh-/Widerrufs-Clients): `VideoPlayerApiClientOptions.Logger` mit `CreateLogger<VideoPlayerApiClient>()` verdrahten | Offen | – |
| 11 | MVU-Logging | `ServerSyncService`: `ILogger<ServerSyncService>?`-Parameter; `Information`-Eintrag mit `SyncResult`-Zählern in `SynchronizeAsync`/`FlushPendingProgressAsync`; 3 `Debug.WriteLine` ersetzen | Offen | – |
| 12 | MVU-Logging | `ProgressReportService`: `ILogger<ProgressReportService>?`-Parameter; Einreihen (`Information`)/Verwerfen (`Warning`); 2 `Debug.WriteLine` ersetzen | Offen | – |
| 13 | MVU-Logging | `ServerEventsService`: `ILogger<ServerEventsService>?`-Parameter; Connect/Disconnect/`StatusChanged`/`ConnectionClosed` als `Information`; 5 `Debug.WriteLine` ersetzen | Offen | – |
| 14 | MVU-Logging | `DownloadService`: `ILogger<DownloadService>?`-Parameter an beiden Konstruktoren; Auftragszustandswechsel/`Notice`-Einträge mit Medientyp/-Id und Anzeigetitel (`title`-Parameter/`DownloadListItem.Title`); 7 `Debug.WriteLine` ersetzen | Offen | – |
| 15 | MVU-Logging | `AutoDownloadService`: `ILogger<AutoDownloadService>?`-Parameter; Lauf-Ergebnis/Throttle-Eintrag; 2 `Debug.WriteLine` ersetzen | Offen | – |
| 16 | MVU-Logging | `PlaybackSourceService`: `ILogger<PlaybackSourceService>?`-Parameter; `Information`-Eintrag mit Medientyp/-Id + `PlaybackSourceKind` (niemals `Location`); 3 `Debug.WriteLine` ersetzen | Offen | – |
| 17 | MVU-Logging | `MediaDetailsService`: `ILogger<MediaDetailsService>?`-Parameter; 4 `Debug.WriteLine` ersetzen; `ShowDetailsCache`-Aufrufe mit Logger | Offen | – |
| 18 | MVU-Logging | `HomeListService`: `ILogger<HomeListService>?`-Parameter; 5 `Debug.WriteLine` ersetzen | Offen | – |
| 19 | MVU-Logging | `OfflineContinueWatchingService`: `ILogger<OfflineContinueWatchingService>?`-Parameter; 3 `Debug.WriteLine` ersetzen; `ShowDetailsCache`-Aufruf mit Logger | Offen | – |
| 20 | MVU-Logging | `UpcomingEpisodesService`: `ILogger<UpcomingEpisodesService>?`-Parameter; 1 `Debug.WriteLine` ersetzen; `ShowDetailsCache`-Aufrufe mit Logger | Offen | – |
| 21 | MVU-Logging | `ProfileInfoService`: `ILogger<ProfileInfoService>?`-Parameter; 1 `Debug.WriteLine` ersetzen | Offen | – |
| 22 | MVU-Logging | `ContinueWatchingMutationService`: `ILogger<ContinueWatchingMutationService>?`-Parameter; Ergebnis-Einträge in `HideAsync`/`SkipAsync` | Offen | – |
| 23 | MVU-Logging | `ShowDetailsCache`: optionale `ILogger?`-Parameter an `WriteAsync`/`ReadAsync`; 2 `Debug.WriteLine`-Fehlerpfade → `LogWarning` | Offen | – |
| 24 | MAUI-Logging | `App`: erforderlichen `ILogger<App>`-Parameter; Start-Workflow-Eintrag in `RunStartupAsync`; 1 `Debug.WriteLine` ersetzen | Offen | – |
| 25 | MAUI-Logging | `AppShell`: erforderlichen `ILogger<AppShell>`-Parameter; 5 `Debug.WriteLine` ersetzen; `Information`-Eintrag beim Quellenwechsel | Offen | – |
| 26 | MAUI-Logging | `PlayerPage`: erforderlichen `ILogger<PlayerPage>`-Parameter; `OnMediaOpened`/`OnMediaEnded`/`OnMediaFailed`/`FailPlaybackStart`/Fortsetzungs-Seek protokollieren inkl. Anzeigetitel aus `mvuHost.Model.Player.Title` (kein 5-s-Takt); 5 `Debug.WriteLine` ersetzen | Offen | – |
| 27 | MAUI-Logging | `SettingsPage`: erforderlichen `ILogger<SettingsPage>`-Parameter; 1 `Debug.WriteLine` ersetzen | Offen | – |
| 28 | MAUI-Logging | `ServerSetupPage`: erforderlichen `ILogger<ServerSetupPage>`-Parameter; 1 `Debug.WriteLine` ersetzen | Offen | – |
| 29 | MAUI-Logging | `ServerHealthProbe`: erforderlichen `ILogger<ServerHealthProbe>`-Parameter; Prüfergebnis/Fehlschlag protokollieren; 2 `Debug.WriteLine` ersetzen | Offen | – |
| 30 | MAUI-Logging | `SignalRServerEventsConnection`: erforderlichen `ILogger<SignalRServerEventsConnection>`-Parameter; Hub-Ereignisse/`Closed`/Reconnect-Fehlschläge protokollieren | Offen | – |
| 31 | MAUI-Logging | `ConnectivityNetworkStatusMonitor`: erforderlichen `ILogger<ConnectivityNetworkStatusMonitor>`-Parameter; `AvailabilityChanged`-Wechsel protokollieren | Offen | – |
| 32 | MAUI-Logging | `PosterImageService`: erforderlichen `ILogger<PosterImageService>`-Parameter; 3 `Debug.WriteLine` → `LogWarning` (keine Erfolgseinträge pro Bildabruf) | Offen | – |
| 33 | MAUI-Logging | `PictureInPictureService`: erforderlichen `ILogger<PictureInPictureService>`-Parameter; `ToggleAsync`/`RestoreAsync`-Ergebnis protokollieren; 1 `Debug.WriteLine` ersetzen | Offen | – |
| 34 | MAUI-Logging | `SecureStorageSecretStore`: erforderlichen `ILogger<SecureStorageSecretStore>`-Parameter; Lesefehlschlag → `LogWarning`; 1 `Debug.WriteLine` ersetzen | Offen | – |
| 35 | Verdrahtung | `MauiProgram`: `Func<IServerEventsConnection>`-Lambda auf `sp`-Parameter umstellen und `ILogger<SignalRServerEventsConnection>` durchreichen | Offen | – |
| 36 | Verdrahtung | `MauiProgram`: `ISecretStore`-Lambda auf `sp`-Parameter umstellen und `ILogger<SecureStorageSecretStore>` durchreichen | Offen | – |
| 37 | Effekt-Handler | `ShellAppEffectHandler`: `LogInformation`-Breadcrumbs in den ausgewählten Effekt-Ausführungsmethoden (Initialisierung, Serverprüfung, An-/Abmeldung, Ereignisverbindung, Sync, Quellauflösung, Downloads, PiP, Protokollversand); Wiedergabe-/Download-Einträge mit Anzeigetitel (`Player.Title` aus `MvuHost.Model` bzw. `EnqueueDownloadEffect.Title`) | Offen | – |
| 38 | Effekt-Handler | `ShellAppEffectHandler`: `LogWarning`-Einträge in `SessionExpired`-/`DeviceTokenRejected`-Handlern und `LogInformation` im `AvailabilityChanged`-Handler | Offen | – |
| 39 | Tests | `AuthSessionServiceTests`: `LoginAsync_WritesEntries_WithoutCredentials` — Information-Eintrag ohne Passwort/Token/Benutzername | Offen | – |
| 40 | Tests | `AuthSessionServiceTests`: `CreateClient_WiresApiClientLogger` — `VideoPlayerApiClientOptions.Logger` wird gesetzt, 401-/Retry-Warnungen aufgezeichnet | Offen | – |
| 41 | Tests | `ServerSyncServiceTests`: `SynchronizeAsync_WritesResultCounters` — Zähler im Eintrag | Offen | – |
| 42 | Tests | `ProgressReportServiceTests`: `ReportProgressAsync_QueueAndDiscard_Logged` — Einreihen/Verwerfen-Level | Offen | – |
| 43 | Tests | `ServerEventsServiceTests`: `ConnectAsync_DisconnectAsync_StatusChanged_Logged` — Verbindungseinträge | Offen | – |
| 44 | Tests | `DownloadServiceTests`: `EnqueueAsync_TransferOutcome_Logged` — Auftragseinträge mit Medientyp/-Id und Anzeigetitel | Offen | – |
| 45 | Tests | `PlaybackSourceServiceTests`: `ResolveAsync_LogsKind_NotLocation` — `PlaybackSourceKind` ja, `access_token`/`Location` nein | Offen | – |
| 46 | Tests | `PendingProgressSyncServiceTests`: `Enqueue_Remove_LoggedAggregated` — Anzahl-Einträge | Offen | – |
| 47 | Tests | `AutoDownloadServiceTests`: `RunAsync_LogsOutcome` — Lauf-Ergebnis/Throttle | Offen | – |
| 48 | Tests | `LogProtocolContractTests`: `NoDebugWriteLine_RemainsInAppSources` — Quelltext-Scan auf `System.Diagnostics.Debug.WriteLine` in `VideoPlayer.Mvu`/`VideoPlayer.Maui`/`VideoPlayer.Storage` | Offen | – |
| 49 | E2E-Tests | `LogProtocolTests`: neuer Fact `LoggingEnabled_RealFlows_WriteDiagnosticEntries` — Protokollierung an, Neustart mit Profil, Auto-Anmeldung + Wiedergabe, Protokolldatei mit `[INFO]`-Facheinträgen ohne `access_token`/Zugangsdaten (Benutzername/Passwort) | Offen | – |
| 50 | Tests | `AuthSessionServiceTests`: `LogoutAsync_WritesEntry` — Abmelde-`Information`-Eintrag via `RecordingLoggerProvider`, ohne Zugangsdaten/Token-Werte | Offen | – |
| 51 | Tests | `LogProtocolContractTests`: `ConnectivityMonitor_LogsAvailabilityChange` — `LogInformation`-Pins in `ConnectivityNetworkStatusMonitor.cs` (`AvailabilityChanged`-Auslösung) und `ShellAppEffectHandler.cs` (`AvailabilityChanged`-Handler) | Offen | – |
| 52 | Tests | `LogProtocolContractTests`: `PictureInPictureService_LogsToggleRestore` — `LogInformation`-Pins in `PictureInPictureService.cs` (`ToggleAsync`/`RestoreAsync`) und `TogglePictureInPicture`-Breadcrumb in `ShellAppEffectHandler.cs` | Offen | – |
| 53 | Tests | `LogProtocolContractTests`: `PlayerPage_LogsPlaybackFailure` — `LogWarning`/`LogError`-Pin in `PlayerPage.xaml.cs` (`OnMediaFailed`/`FailPlaybackStart`, mit `logMessage`, ohne `Location`) | Offen | – |
| 54 | Tests | `LogProtocolContractTests`: `ShellAppEffectHandler_LogsLogoutBreadcrumb` — `LogInformation`-Pin im `LogoutAsync`-Pfad von `ShellAppEffectHandler.cs` | Offen | – |
| 55 | E2E-Tests | Optional: `LoggingEnabled_RealFlows_WriteDiagnosticEntries` (oder eigener Fact) um Teilflüsse erweitern — Abmeldung, PiP-Umschaltung, Wiedergabe-Fehlschlag via nicht abspielbarer `MockVideoServer`-Quelle; Online-/Offline-Wechsel ist nicht E2E-automatisierbar und bleibt Contract-Pin-Nachweis | Offen | – |
