# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Bei aktivierter Protokollierung erzeugen fachliche Abläufe verwertbare Einträge in `videoplayer.log` (Kerndefizit: bisher nur Marker-Zeile) | `ShellAppEffectHandler`-Breadcrumbs in den Effekt-Ausführungsmethoden + Logger-Umstellung aller Dienste (Programmabläufe, Tasks 4–38) | Neuer Pflicht-E2E-Fact `LoggingEnabled_RealFlows_WriteDiagnosticEntries` (Neustart, Auto-Anmeldung, Wiedergabe → `[INFO]`-Facheinträge ohne Geheimnisse) + Unit-Tests je Dienst | Abgedeckt |
| App-Start / Initialisierung protokollieren | `App.RunStartupAsync` (`ILogger<App>`), `InitializeStorage`-Breadcrumb (Tasks 24, 37) | E2E-Fact (Neustart-Flow löst die Einträge aus) | Abgedeckt |
| Serververbindung und Anmeldung protokollieren (`AuthAttemptStatus`, nie Zugangsdaten) | `AuthSessionService` (`ILoggerFactory?`), `DeviceTokenService`, `AttemptAutoLogin`-/`AttemptLogin`-Breadcrumbs, `ServerHealthProbe`, `ConnectServerEventsAsync` (Tasks 7–10, 13, 29, 30, 35, 37) | `LoginAsync_WritesEntries_WithoutCredentials`, `ConnectAsync_DisconnectAsync_StatusChanged_Logged`, `CreateClient_WiresApiClientLogger`, E2E-Fact | Abgedeckt |
| Abmeldung protokollieren („An-/Abmeldung" benannt) | `AuthSessionService.LogoutAsync`-Eintrag + `LogoutAsync`-Breadcrumb im `ShellAppEffectHandler` (Tasks 9, 37) | `LogoutAsync_WritesEntry` (Unit-Test), `ShellAppEffectHandler_LogsLogoutBreadcrumb` (Contract-Pin), optionaler E2E-Teilfluss — **Lücke aus plan-check.1 geschlossen** | Abgedeckt |
| Wiedergabe protokollieren (Start/Ende, Fortsetzungs-Seek, kein 5-s-Fortschrittstakt) | `PlaybackSourceService.ResolveAsync` (Kind/Medientyp/-Id + Titel auf Handler-Ebene), `PlayerPage`-Ereignisse, `ApplyResumeSeekAsync` (Tasks 16, 26, 37) | `ResolveAsync_LogsKind_NotLocation`, E2E-Fact (Erfolgswiedergabe) | Abgedeckt |
| Wiedergabe-Fehlschlag protokollieren (`OnMediaFailed`/`FailPlaybackStart`) | `PlayerPage`-Änderung `LogWarning`/`LogError` mit `logMessage` (Task 26) | `PlayerPage_LogsPlaybackFailure` (Contract-Pin; `PlayerPage` in `VideoPlayer.Maui` nicht testreferenzierbar) + optionaler E2E-Teilfluss — **Lücke aus plan-check.1 geschlossen** | Abgedeckt |
| Synchronisation protokollieren (Sync-Beginn/-Ergebnis mit `SyncResult`-Zählern, nachgesendete Fortschritte) | `ServerSyncService`, `PendingProgressSyncService`, `ProgressReportService`, `RunFullSync`-Breadcrumb (Tasks 4, 11, 12, 37) | `SynchronizeAsync_WritesResultCounters`, `Enqueue_Remove_LoggedAggregated`, `ReportProgressAsync_QueueAndDiscard_Logged` | Abgedeckt |
| Downloads protokollieren (Auftrag/Zustand/Fehlschlag, aggregiert, mit Anzeigetitel) | `DownloadService`, `AutoDownloadService`, Download-Effekt-Breadcrumbs (Tasks 14, 15, 37) | `EnqueueAsync_TransferOutcome_Logged`, `RunAsync_LogsOutcome` | Abgedeckt |
| Online-/Offline-Wechsel protokollieren | `ConnectivityNetworkStatusMonitor` (`ILogger`) + `AvailabilityChanged`-Handler-Eintrag im `ShellAppEffectHandler` (Tasks 31, 38) | `ConnectivityMonitor_LogsAvailabilityChange` (Contract-Pins in beiden Dateien) — **Lücke aus plan-check.1 geschlossen** | Abgedeckt |
| PiP-Umschaltung protokollieren | `PictureInPictureService.ToggleAsync`/`RestoreAsync`, `TogglePictureInPicture`-Breadcrumb (Tasks 33, 37) | `PictureInPictureService_LogsToggleRestore` (Contract-Pin) + optionaler E2E-Teilfluss — **Lücke aus plan-check.1 geschlossen** | Abgedeckt |
| Verbindungsauf-/-abbau der Ereignisverbindung, Hub-Ereignisse | `ServerEventsService`, `SignalRServerEventsConnection` über `MauiProgram`-Fabrik (Tasks 13, 30, 35) | `ConnectAsync_DisconnectAsync_StatusChanged_Logged` | Abgedeckt |
| Fehler protokollieren (71 `LogError`-Pfade bleiben, überholbare Fehler `Warning`) | Level-Einordnungsregel in den Designentscheidungen; `Debug.WriteLine`-Catch-Blöcke → `LogWarning`/`LogError` | `ReportProgressAsync_QueueAndDiscard_Logged` (Warning-Pfad), `PlayerPage_LogsPlaybackFailure`, `RunAsync_LogsOutcome`, bestehende `AppLogServiceTests`/`FileLoggerTests` | Abgedeckt |
| Sparsamkeit: kein 5-s-Takt, keine Einträge pro Listeneintrag/Bildabruf, keine pro-Nachricht-Einträge | Ausschlussliste in Designentscheidungen; `PosterImageService` nur Warnpfade; kein `ReportPlaybackProgressEffect`-Eintrag | Aggregations-Assertions in `Enqueue_Remove_LoggedAggregated`, `EnqueueAsync_TransferOutcome_Logged` („nicht pro Teilschritt"), `RunAsync_LogsOutcome` (Throttle) | Abgedeckt |
| Keine Geheimnisse: `access_token`/`PlaybackSource.Location`, Passwörter, Tokens, `AuthAttempt.Username` | Strikte Regel in Designentscheidungen und Seiteneffekten; `ResolveAsync` protokolliert nie `Location`; Titel nur an zwei Stellen | `ResolveAsync_LogsKind_NotLocation`, `LoginAsync_WritesEntries_WithoutCredentials`, `LogoutAsync_WritesEntry`, E2E-Fact (prüft `access_token`/Zugangsdaten in der Datei) | Abgedeckt |
| `Debug.WriteLine`-Umstellung in `VideoPlayer.Mvu` (53), `VideoPlayer.Maui` (20), `VideoPlayer.Storage` (2); `VideoWebPlayer.Maui` und `VideoPlayer.ApiClient`-Discovery ausgenommen | Volle Umstellung als Programmablauf + Tasks 4–34 (vom Anwender bestätigter Umfang) | Contract-Test `NoDebugWriteLine_RemainsInAppSources` (Task 48) | Abgedeckt |
| `VideoPlayerApiClientOptions.Logger` in `AuthSessionService.CreateClient` + kurzlebige Refresh-/Widerrufs-Clients verdrahten | `ILoggerFactory?`-Parameter, `CreateLogger<VideoPlayerApiClient>()` in `CreateClient` und den kurzlebigen Clients (Tasks 8, 10) | `CreateClient_WiresApiClientLogger` | Abgedeckt |
| `Microsoft.Extensions.Logging.Abstractions` explizit in `VideoPlayer.Mvu.csproj` | Konfigurationsänderung + Umsetzungsschritt 1 (Task 1) | Build-Kompilierung / Contract-Test mittelbar | Abgedeckt |
| Keine neue Konfiguration; Kopplung an `AppSettings.LoggingEnabled`; `Information`-Mindestlevel bleibt einzige Steuerung | Konfigurationsabschnitt: keine Änderung | Bestehende `FileLoggerTests`/`AppLogServiceTests` bleiben gültig | Abgedeckt |
| Performance/Speicher leiden nicht (1-MiB-Rotation, `IsEnabled`-Frühausstieg, `NullLogger` in Tests) | Seiteneffekte-Abschnitt (Kosten bei deaktiviertem Protokoll, Framework-Rauschen benannt) | Bestehende Rotations-/IsEnabled-Tests | Abgedeckt |
| `VideoWebPlayer.Maui` (Altbestand) unberührt | Designentscheidung + Ausnahme im Contract-Test | `NoDebugWriteLine_RemainsInAppSources` (Ausnahmeliste) | Abgedeckt |
| Bestehende Tests bleiben gültig (`AppLogServiceTests`, `FileLoggerTests`, `LogProtocolContractTests`, ~39 Dienst-Erzeugungsstellen) | Optionale Logger-Parameter (`= null`); `VideoPlayer.Maui`-Klassen nicht testreferenziert — dokumentiert unter „Betroffene bestehende Tests: Keine" | Nicht erforderlich (kein Bruch); Risiko in Seiteneffekten benannt | Abgedeckt |
| Protokollversand-Ergebnis-Eintrag | `SendLogProtocolAsync`-Breadcrumb (Task 37) | Bestehender E2E-Fact `SendLog_Click_InvokesPlatformShareOrMail_WithoutCrash` | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine — die vier in `plan-check.1.md` gemeldeten Testlücken (Online-/Offline-Wechsel, PiP-Umschaltung, Abmeldung, Wiedergabe-Fehlschlag) sind durch die Tasks 50–54 (Unit-Test `LogoutAsync_WritesEntry` + die Contract-Pins `ConnectivityMonitor_LogsAvailabilityChange`, `PictureInPictureService_LogsToggleRestore`, `PlayerPage_LogsPlaybackFailure`, `ShellAppEffectHandler_LogsLogoutBreadcrumb`) geschlossen. Das entspricht exakt der in `plan-check.1.md` empfohlenen Lösung.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Protokollierung aktivieren → reale Abläufe (Neustart, Auto-Anmeldung, Wiedergabe) → Protokolldatei mit Facheinträgen ohne Geheimnisse | Neuer Pflicht-Fact `LoggingEnabled_RealFlows_WriteDiagnosticEntries` in `VideoPlayer.E2E.Tests/Tests/LogProtocolTests.cs` (Task 49) | Abgedeckt |
| Schalter/Sichtbarkeit/Versand-UI (bestehender Funktionsumfang) | Bestehende 4 Facts in `LogProtocolTests` (unverändert gültig) | Abgedeckt |
| Abmeldung sichtbar im Protokoll | Optionaler Teilfluss im selben/eigenem Fact (Task 55); primärer Nachweis: `LogoutAsync_WritesEntry` (Unit-Test, `AuthSessionService` ist testreferenzierbar) + `ShellAppEffectHandler_LogsLogoutBreadcrumb` (Contract-Pin) | Abgedeckt (E2E optional begründet) |
| PiP-Umschaltung sichtbar im Protokoll | Optionaler Teilfluss (Task 55); primärer Nachweis: `PictureInPictureService_LogsToggleRestore`-Contract-Pin — `PictureInPictureService` liegt in `VideoPlayer.Maui` und ist nicht unit-testbar; PiP-Umschaltung ist plattformabhängig instabil in der UI-Automatisierung | Abgedeckt (E2E optional begründet) |
| Wiedergabe-Fehlschlag sichtbar im Protokoll | Optionaler Teilfluss über nicht abspielbare `MockVideoServer`-Quelle (Task 55); primärer Nachweis: `PlayerPage_LogsPlaybackFailure`-Contract-Pin — `MediaElement`-Fehlerpfad nicht unit-testbar | Abgedeckt (E2E optional begründet) |
| Online-/Offline-Wechsel sichtbar im Protokoll | Nicht geplant, mit Begründung: Netzverfügbarkeit ist über FlaUI-UI-Automatisierung nicht umschaltbar; Nachweis ausschließlich per `ConnectivityMonitor_LogsAvailabilityChange`-Contract-Pins | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

Keine — Umsetzungsschritte sind konkret und vollständig (alle betroffenen Klassen, Konstruktor-Strategie optional vs. erforderlich, `MauiProgram`-Fabriken, Paketreferenz, Umsetzungsreihenfolge mit Voraussetzungen, Seiteneffekte/Risiken inkl. DI-Konstruktorwahl, CS1591, Framework-Rauschen; Testdaten-Hilfsmittel `RecordedLogEntry`/`RecordingLoggerProvider` als Tasks 2–3; E2E-Hilfsmethoden `StartAppAsync`/`MockVideoServer`-Wiedergabe-Setup benannt).

## Hinweise

- Alle vier in `plan-check.1.md` gemeldeten Lücken sind geschlossen — die Nacharbeit folgt der dort empfohlenen „einfachsten Schließung" (Contract-Pins für `VideoPlayer.Maui`-Klassen, Unit-Test für `AuthSessionService.LogoutAsync`).
- Die Contract-Pins sind statische Quelltext-Nachweise (Vorhandensein der `LogInformation`/`LogWarning`-Aufrufe), kein Verhaltensnachweis; der optionale Task 55 (E2E-Teilflüsse Abmeldung/PiP/Wiedergabe-Fehlschlag) kann bei Bedarf das Vertrauen in die Endverdrahtung erhöhen, ist für die Vollständigkeit aber nicht erforderlich.
- Bei der Implementierung von `NoDebugWriteLine_RemainsInAppSources` ist auf sinnvolle Pfad-Filter zu achten (generierte/`obj`-/`bin`-Dateien sowie die dokumentierten Ausnahmen `VideoWebPlayer.Maui` und `VideoPlayer.ApiClient/Discovery`), damit der Quelltext-Scan nicht fehlschlägt.
- Die E2E-Assertion „≥ 1 `[INFO]`-Eintrag" ist minimal; eine Optional-Verbesserung wäre die Prüfung auf erwartete Ereignistypen (Anmeldung, Wiedergabe-Start als separate Einträge).
