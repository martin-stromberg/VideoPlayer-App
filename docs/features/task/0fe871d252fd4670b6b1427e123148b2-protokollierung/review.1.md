# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

- [x] `PackageReference` `Microsoft.Extensions.Logging.Abstractions` 10.0.10 in `VideoPlayer.Mvu/VideoPlayer.Mvu.csproj` — vorhanden (Zeile 13)
- [x] `RecordedLogEntry` (`VideoWebPlayer.Maui.Tests/Stubs/RecordedLogEntry.cs`) — angelegt (Level, Category, Message, Exception)
- [x] `RecordingLoggerProvider` (`VideoWebPlayer.Maui.Tests/Stubs/RecordingLoggerProvider.cs`) — angelegt (`ILoggerProvider` + `ILoggerFactory`-Ersatz, `CreateLogger<T>()`)
- [x] `PendingProgressSyncService` (`VideoPlayer.Storage`) — `ILogger<PendingProgressSyncService>?`-Parameter (Zeile 24), `LogInformation` in `EnqueueAsync`/`GetPendingAsync`/`RemoveForMediaAsync` mit Anzahl/Medien-Id
- [x] `CacheDatabase` (`VideoPlayer.Storage`) — `ILogger<CacheDatabase>?`-Parameter (Zeile 23), Recovery-Pfad als `LogWarning` mit Exception (Zeile 74)
- [x] `BackupDatabase` (`VideoPlayer.Storage`) — `ILogger<BackupDatabase>?`-Parameter (Zeile 23), Recovery-Pfad als `LogWarning` mit Exception (Zeile 68)
- [x] `DeviceTokenService` (`VideoPlayer.Mvu`) — `ILogger<DeviceTokenService>?` an beiden Konstruktoren (Zeilen 34, 49), Token-Verwerfen (`LogInformation`, Zeilen 168/217), Gate-Abweisung (`LogWarning`, Zeile 282), alle `Debug.WriteLine` ersetzt
- [x] `AuthSessionService` (`VideoPlayer.Mvu`) — `ILoggerFactory?`-Parameter am längsten Konstruktor (Zeile 81), Convenience-Konstruktoren reichen die Fabrik durch inkl. `CreateLogger<DeviceTokenService>()` (Zeilen 35, 60); `Information`-Einträge für Anmeldeergebnis (Zeilen 138/149/159/186), stille Neuanmeldung (Zeile 389), `SessionExpired` (Zeile 577), Abmeldung (Zeile 210)
- [x] `AuthSessionService.CreateClient` + kurzlebige Clients — `VideoPlayerApiClientOptions.Logger = loggerFactory?.CreateLogger<VideoPlayerApiClient>()` in `CreateClient` (Zeile 371), `RevokeRefreshTokenAsync` (Zeile 245), `SilentReloginAsync` (Zeile 416), `TryRefreshSessionAsync` (Zeile 485)
- [x] `ServerSyncService` (`VideoPlayer.Mvu`) — `ILogger<ServerSyncService>?` (Zeile 57), `Information`-Eintrag mit `SyncResult`-Zählern in `SynchronizeAsync` (Zeile 91) und Nachsende-Ergebnis in `FlushPendingProgressAsync` (Zeile 153)
- [x] `ProgressReportService` (`VideoPlayer.Mvu`) — `ILogger<ProgressReportService>?` (Zeile 33), Einreihen als `Information` (Zeile 99), Verwerfen bei `IsPermanentClientError` als `LogWarning` (Zeile 79)
- [x] `ServerEventsService` (`VideoPlayer.Mvu`) — `ILogger<ServerEventsService>?` (Zeile 34), Connect (Zeile 97)/Disconnect (Zeile 115)/`StatusChanged` (Zeile 168)/`ConnectionClosed` (Zeile 187) als `Information`
- [x] `DownloadService` (`VideoPlayer.Mvu`) — `ILogger<DownloadService>?` am öffentlichen (Zeile 136) und `internal`-Testkonstruktor (Zeile 160); `EnqueueAsync`-`Notice`-Einträge mit Medientyp/-Id + Titel (Zeile 247), Zustandswechsel in `SetStateAsync` mit `DownloadListItem.Title` (Zeilen 802/812), Abbruch/Löschen/Wiederaufnahme protokolliert (Zeilen 400, 447, 531); alle `Debug.WriteLine` ersetzt
- [x] `AutoDownloadService` (`VideoPlayer.Mvu`) — `ILogger<AutoDownloadService>?` an beiden Konstruktoren (Zeilen 63, 83), `MinRunInterval`-Auslassung (Zeile 126) und Lauf-Ergebnis (Zeile 182) als `Information`, aggregierte Enqueue-Einträge (Zeilen 245, 350)
- [x] `PlaybackSourceService` (`VideoPlayer.Mvu`) — `ILogger<PlaybackSourceService>?` an beiden Konstruktoren (Zeilen 44, 60), `ResolveAsync`-Eintrag mit Medientyp/-Id + `PlaybackSourceKind` + Fortsetzungsposition (Zeilen 115/156), niemals `Location`/`access_token`
- [x] `MediaDetailsService` (`VideoPlayer.Mvu`) — `ILogger<MediaDetailsService>?` (Zeilen 39, 53), Fehlerpfade als `LogWarning`, `ShowDetailsCache`-Aufrufe reichen den Logger durch (Zeilen 160, 189, 202, 344)
- [x] `HomeListService` (`VideoPlayer.Mvu`) — `ILogger<HomeListService>?` (Zeile 32), 5 `Debug.WriteLine`-Fehlerpfade als `LogWarning`
- [x] `OfflineContinueWatchingService` (`VideoPlayer.Mvu`) — `ILogger<OfflineContinueWatchingService>?` an beiden Konstruktoren (Zeilen 46, 62), `ShowDetailsCache.ReadAsync` mit Logger (Zeile 187)
- [x] `UpcomingEpisodesService` (`VideoPlayer.Mvu`) — `ILogger<UpcomingEpisodesService>?` (Zeile 32), `ShowDetailsCache`-Aufrufe mit Logger (Zeilen 77, 92)
- [x] `ProfileInfoService` (`VideoPlayer.Mvu`) — `ILogger<ProfileInfoService>?` (Zeile 30), `LogWarning` im Token-Parse-Pfad (Zeile 149), keine Profildaten im Eintragstext
- [x] `ContinueWatchingMutationService` (`VideoPlayer.Mvu`) — `ILogger<ContinueWatchingMutationService>?` (Zeile 43), `Information`/`Warning`-Ergebniseinträge in `HideAsync`/`SkipAsync` (Zeilen 71/75, 101/105)
- [x] `ShowDetailsCache` (`VideoPlayer.Mvu`) — optionale `ILogger?`-Parameter an `WriteAsync` (Zeile 107) und `ReadAsync` (Zeile 169), beide Fehlerpfade als `LogWarning` mit Cache-Schlüssel
- [x] `App` (`VideoPlayer.Maui/App.xaml.cs`) — erforderlicher `ILogger<App>` (Zeile 22), Start-Workflow-Eintrag in `RunStartupAsync` (Zeile 67), `Debug.WriteLine` ersetzt
- [x] `AppShell` (`VideoPlayer.Maui/AppShell.xaml.cs`) — erforderlicher `ILogger<AppShell>` (Zeile 54), 5 Fehlerpfade als `LogWarning`, `Information`-Eintrag beim Quellenwechsel (Zeile 308)
- [x] `PlayerPage` (`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`) — erforderlicher `ILogger<PlayerPage>` (Zeile 56); `OnMediaOpened` mit Titel + Fortsetzung (Zeile 303), `OnMediaEnded` (Zeile 401), `OnMediaFailed`/`FailPlaybackStart` mit `logMessage` ohne `Location` (Zeilen 410/293), `ApplyResumeSeekAsync` (Zeile 387), Fortschrittstakt-Catches als `LogWarning` — kein 5-s-Takt-Eintrag
- [x] `SettingsPage` (`VideoPlayer.Maui/Pages/SettingsPage.xaml.cs`) — erforderlicher `ILogger<SettingsPage>` (Zeile 22), Speicher-Catch als `LogWarning` (Zeile 214)
- [x] `ServerSetupPage` (`VideoPlayer.Maui/Pages/ServerSetupPage.xaml.cs`) — erforderlicher `ILogger<ServerSetupPage>` (Zeile 24), QR-Scan-Fehlschlag als `LogWarning` (Zeile 238)
- [x] `ServerHealthProbe` (`VideoPlayer.Maui/Services/ServerHealthProbe.cs`) — erforderlicher `ILogger<ServerHealthProbe>` (Zeile 30), Ergebnis als `Information` (Zeile 67), Fehlschläge als `LogWarning`
- [x] `SignalRServerEventsConnection` (`VideoPlayer.Maui/Services/SignalRServerEventsConnection.cs`) — erforderlicher `ILogger<SignalRServerEventsConnection>` (Zeile 20), Hub-Ereignisse inkl. `NewVideosScanned` mit `SourceId`/`Count` (Zeile 74), `Closed`/Reconnect-Einträge, Verbindungsauf-/-abbau
- [x] `ConnectivityNetworkStatusMonitor` (`VideoPlayer.Maui/Services/ConnectivityNetworkStatusMonitor.cs`) — erforderlicher `ILogger<ConnectivityNetworkStatusMonitor>` (Zeile 22), `AvailabilityChanged`-Wechsel als `LogInformation` mit `IsOnline`/`IsWifi` (Zeile 57)
- [x] `PosterImageService` (`VideoPlayer.Maui/Services/PosterImageService.cs`) — erforderlicher `ILogger<PosterImageService>` (Zeile 30), 3 Fehlerpfade als `LogWarning`, keine Erfolgseinträge pro Bildabruf
- [x] `PictureInPictureService` (`VideoPlayer.Maui/Services/PictureInPictureService.cs`) — erforderlicher `ILogger<PictureInPictureService>` (Zeile 20), `ToggleAsync`/`RestoreAsync` als `LogInformation` (Zeilen 41, 53), Fehlerpfad als `LogWarning`
- [x] `SecureStorageSecretStore` (`VideoPlayer.Maui/Services/SecureStorageSecretStore.cs`) — erforderlicher `ILogger<SecureStorageSecretStore>` (Zeile 22), Lesefehlschlag als `LogWarning` mit Schlüsselname (Zeile 39)
- [x] `MauiProgram` (`VideoPlayer.Maui/MauiProgram.cs`) — `ISecretStore`-Lambda mit `sp`-Parameter und `ILogger<SecureStorageSecretStore>` (Zeilen 61–63); `Func<IServerEventsConnection>`-Lambda mit `sp`-Parameter und `ILogger<SignalRServerEventsConnection>` (Zeilen 89–91)
- [x] `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`) — `LogInformation`-Breadcrumbs: Ablage initialisiert (Zeile 1710), Serverprüfung (Zeile 1918), Auto-/manuelle Anmeldung (Zeilen 1612/1631), Ereignisverbindung auf-/abbau (Zeilen 533/548), Voll- und Teil-Sync mit `SyncResult`-Zählern (Zeilen 569/584), Quellauflösung mit Medientyp/-Id + Titel + `Kind` ohne `Location` (Zeile 436), Download-Einplanung/-Löschung/-Abbruch/-Wiederaufnahme/Auto-Lauf (Zeilen 1490, 1521, 1534, 1547, 1560), Abmeldung (Zeile 1440), PiP-Umschaltung (Zeile 508), Protokollversand-Fallback (Zeile 375); `LogWarning` in `OnSessionExpired` (Zeile 1658) und `OnDeviceTokenRejected` (Zeile 2175), `LogInformation` in `OnNetworkAvailabilityChanged` (Zeile 1576)
- [x] `Debug.WriteLine`-Umstellung — kein `Debug.WriteLine` mehr in `VideoPlayer.Mvu`, `VideoPlayer.Maui` und `VideoPlayer.Storage` (Quelltext-Scan)
- [x] `AuthSessionServiceTests.LoginAsync_WritesEntries_WithoutCredentials` — vorhanden (Zeile 290)
- [x] `AuthSessionServiceTests.CreateClient_WiresApiClientLogger` — vorhanden (Zeile 311)
- [x] `AuthSessionServiceTests.LogoutAsync_WritesEntry` — vorhanden (Zeile 333)
- [x] `ServerSyncServiceTests.SynchronizeAsync_WritesResultCounters` — vorhanden (Zeile 143)
- [x] `ProgressReportServiceTests.ReportProgressAsync_QueueAndDiscard_Logged` — vorhanden (Zeile 155)
- [x] `ServerEventsServiceTests.ConnectAsync_DisconnectAsync_StatusChanged_Logged` — vorhanden (Zeile 150)
- [x] `DownloadServiceTests.EnqueueAsync_TransferOutcome_Logged` — vorhanden (Zeile 151)
- [x] `PlaybackSourceServiceTests.ResolveAsync_LogsKind_NotLocation` — vorhanden (Zeile 282)
- [x] `PendingProgressSyncServiceTests.Enqueue_Remove_LoggedAggregated` — vorhanden (Zeile 111)
- [x] `AutoDownloadServiceTests.RunAsync_LogsOutcome` — vorhanden (Zeile 640)
- [x] `LogProtocolContractTests.ConnectivityMonitor_LogsAvailabilityChange` — vorhanden (Zeile 116)
- [x] `LogProtocolContractTests.PictureInPictureService_LogsToggleRestore` — vorhanden (Zeile 135)
- [x] `LogProtocolContractTests.PlayerPage_LogsPlaybackFailure` — vorhanden (Zeile 154)
- [x] `LogProtocolContractTests.ShellAppEffectHandler_LogsLogoutBreadcrumb` — vorhanden (Zeile 178)
- [x] `LogProtocolContractTests.NoDebugWriteLine_RemainsInAppSources` — vorhanden (Zeile 196)
- [x] `LogProtocolTests.LoggingEnabled_RealFlows_WriteDiagnosticEntries` (`VideoPlayer.E2E.Tests/Tests/LogProtocolTests.cs`, Zeile 171) — vorhanden: Protokollierung aktivieren, Neustart mit Profil, Auto-Anmeldung + Wiedergabe, `[INFO]`-Facheintrag ohne `access_token`/Benutzername/Passwort

## Offene Aufgaben

- [ ] Tasks #55: Optionale E2E-Teilflüsse (Abmeldung, PiP-Umschaltung, Wiedergabe-Fehlschlag via nicht abspielbarer `MockVideoServer`-Quelle) — fehlt vollständig: `LogProtocolTests.LoggingEnabled_RealFlows_WriteDiagnosticEntries` deckt nur Auto-Anmeldung und Wiedergabe ab; die optionalen Teilflüsse sind nicht integriert und es existiert kein eigener Fact. Der Plan markiert diesen Baustein ausdrücklich als `Optional` — die Pflicht-Anforderung (`[INFO]`-Facheinträge aus realen Abläufen) ist durch den vorhandenen Fact erfüllt.

## Hinweise

- Das einzig offene Element (Tasks #55) ist im Plan mit Priorität „Optional" geführt; seine primären Nachweise existieren bereits als Unit-Test (`AuthSessionServiceTests.LogoutAsync_WritesEntry`) und Contract-Pins (`PictureInPictureService_LogsToggleRestore`, `PlayerPage_LogsPlaybackFailure`). Die Entscheidung, ob die optionalen E2E-Teilflüsse nachgerüstet oder der Task als entbehrlich geschlossen wird, liegt beim Anwender.
- `dotnet build VideoWebPlayer.Maui.Tests -c Release` läuft fehlerfrei (0 Warnungen, 0 Fehler) — die optionalen Logger-Parameter halten alle ~39 direkten `new …Service(…)`-Erzeugungsstellen der Tests kompilierbar.
- Die Quellen liegen im Repository-Root (`VideoPlayer.Mvu`, `VideoPlayer.Maui`, `VideoPlayer.Storage` …), nicht unter `src/` — die Prüfung wurde entsprechend auf die Projektverzeichnisse angewendet.
- Die Sparsamkeits-/Geheimnisregeln sind eingehalten: `PlaybackSourceService`/`ShellAppEffectHandler` protokollieren `Kind`/`Title`, nie `Location` (`access_token`); `AuthSessionService` protokolliert nur Host + `AuthAttemptStatus`; kein Eintrag im 5-s-Fortschrittstakt und kein `ReportPlaybackProgressEffect`-Breadcrumb; `PosterImageService` nur Warnpfade.
