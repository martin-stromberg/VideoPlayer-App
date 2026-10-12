# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Bei aktivierter Protokollierung erzeugen fachliche Abläufe verwertbare Einträge in `videoplayer.log` (Kernproblem: bisher nur Marker-Zeile) | `ShellAppEffectHandler`-Breadcrumbs in den Effekt-Ausführungsmethoden + Logger-Umstellung aller Dienste (Programmabläufe §1–8, Tasks 4–38) | Neuer E2E-Fact `LoggingEnabled_RealFlows_WriteDiagnosticEntries` (Neustart, Auto-Anmeldung, Wiedergabe → ≥ 1 `[INFO]`-Eintrag) + Unit-Tests je Dienst | Abgedeckt |
| App-Start / Initialisierung protokollieren | `App.RunStartupAsync` (`ILogger<App>`), `InitializeStorageAsync`-Breadcrumb (Task 24, 37) | E2E-Fact (Neustart-Flow löst die Einträge aus) | Abgedeckt |
| Serververbindung und Anmeldung protokollieren (`AuthAttemptStatus`, nie Zugangsdaten) | `AuthSessionService` (`ILoggerFactory?`), `AttemptAutoLogin`-/`AttemptLogin`-Breadcrumbs, `ServerHealthProbe`, `ConnectServerEventsAsync` (Tasks 8–10, 13, 29–30, 37) | `LoginAsync_WritesEntries_WithoutCredentials`, `ConnectAsync_DisconnectAsync_StatusChanged_Logged`, E2E-Fact | Abgedeckt |
| Abmeldung protokollieren („An-/Abmeldung" benannt) | `AuthSessionService.LogoutAsync` + `LogoutAsync`-Breadcrumb im `ShellAppEffectHandler` (Task 9, 37) | **Kein Test geplant** — nur Anmeldung wird getestet | Lücke |
| Wiedergabe protokollieren (Start/Ende/Fehlschlag, Fortsetzungs-Seek, kein Fortschrittstakt) | `PlaybackSourceService.ResolveAsync` (Kind/Medientyp/-Id), `PlayerPage`-Ereignisse, `ApplyResumeSeekAsync` (Tasks 16, 26) | `ResolveAsync_LogsKind_NotLocation`, E2E-Fact (Erfolgswiedergabe) | Abgedeckt (Erfolgspfad) |
| Wiedergabe-Fehlschlag protokollieren (`OnMediaFailed`/`FailPlaybackStart`) | `PlayerPage`-Änderung `LogWarning`/`LogError` mit `logMessage` (Task 26) | **Kein Test geplant** — E2E deckt nur Erfolgswiedergabe; `PlayerPage` (`VideoPlayer.Maui`) nicht unit-testbar, kein Contract-Test | Lücke |
| Synchronisation protokollieren (Sync-Beginn/-Ergebnis mit `SyncResult`-Zählern, nachgesendete Fortschritte) | `ServerSyncService`, `PendingProgressSyncService`, `ProgressReportService`, `RunFullSync`-Breadcrumb (Tasks 4, 11–12) | `SynchronizeAsync_WritesResultCounters`, `Enqueue_Remove_LoggedAggregated`, `ReportProgressAsync_QueueAndDiscard_Logged` | Abgedeckt |
| Downloads protokollieren (Auftrag/Zustand/Fehlschlag, aggregiert) | `DownloadService`, `AutoDownloadService`, Download-Effekt-Breadcrumbs (Tasks 14–15) | `EnqueueAsync_TransferOutcome_Logged`, `RunAsync_LogsOutcome` | Abgedeckt |
| Online-/Offline-Wechsel protokollieren | `ConnectivityNetworkStatusMonitor` (`ILogger`) + `AvailabilityChanged`-Handler-Eintrag (Tasks 31, 38) | **Kein Test geplant** — Klasse liegt in `VideoPlayer.Maui` (nicht unit-testbar), E2E schaltet das Netz nicht um, kein Contract-Test | Lücke |
| PiP-Umschaltung protokollieren | `PictureInPictureService.ToggleAsync`/`RestoreAsync`, `TogglePictureInPicture`-Breadcrumb (Task 33, 37) | **Kein Test geplant** — Maui-Klasse nicht unit-testbar, E2E toggelt PiP nicht, kein Contract-Test | Lücke |
| Verbindungsauf-/-abbau der Ereignisverbindung, Hub-Ereignisse | `ServerEventsService`, `SignalRServerEventsConnection` über `MauiProgram`-Fabrik (Tasks 13, 30, 35) | `ConnectAsync_DisconnectAsync_StatusChanged_Logged` | Abgedeckt |
| Fehler protokollieren (vorhandene `LogError`-Pfade bleiben, überholbare Fehler `Warning`) | 71 `LogError` bleiben; `Debug.WriteLine`-Catch-Blöcke → `LogWarning`/`LogError` nach Stufen-Regel | `ReportProgressAsync_QueueAndDiscard_Logged` (Warning-Pfad), `ResolveAsync_LogsKind_NotLocation`, bestehende `AppLogServiceTests`/`FileLoggerTests` | Abgedeckt |
| Sparsamkeit: kein 5-s-Takt, keine Einträge pro Listeneintrag/Bildabruf, keine pro-Nachricht-Einträge | Ausschlussliste in Designentscheidungen; `PosterImageService` nur Warnpfade; kein `ReportPlaybackProgressEffect`-Eintrag | Aggregations-Assertions in `Enqueue_Remove_LoggedAggregated`, `EnqueueAsync_TransferOutcome_Logged` („nicht pro Teilschritt"), `RunAsync_LogsOutcome` (Throttle) | Abgedeckt |
| Keine Geheimnisse: `access_token`/`PlaybackSource.Location`, Passwörter, Tokens, `AuthAttempt.Username` | Strikte Regel in Designentscheidungen und Seiteneffekten; `ResolveAsync` protokolliert nie `Location` | `ResolveAsync_LogsKind_NotLocation`, `LoginAsync_WritesEntries_WithoutCredentials`, E2E-Fact (prüft `access_token`/Zugangsdaten in der Datei) | Abgedeckt |
| `Debug.WriteLine`-Umstellung in `VideoPlayer.Mvu` (53), `VideoPlayer.Maui` (20), `VideoPlayer.Storage` (2); `VideoWebPlayer.Maui` und `VideoPlayer.ApiClient`-Discovery ausgenommen | Volle Umstellung als Programmablauf §8 + Tasks 4–34 | Contract-Test `NoDebugWriteLine_RemainsInAppSources` | Abgedeckt |
| `VideoPlayerApiClientOptions.Logger` in `AuthSessionService.CreateClient` + kurzlebige Clients verdrahten | `ILoggerFactory?`-Parameter, `CreateLogger<VideoPlayerApiClient>()` in `CreateClient`/Refresh-/Widerrufs-Clients (Task 10) | `CreateClient_WiresApiClientLogger` | Abgedeckt |
| `Microsoft.Extensions.Logging.Abstractions` explizit in `VideoPlayer.Mvu.csproj` | Konfigurationsänderung + Umsetzungsschritt 1 (Task 1) | Build-Kompilierung / Contract-Test mittelbar | Abgedeckt |
| Keine neue Konfiguration; Kopplung an `AppSettings.LoggingEnabled`; `Information`-Mindestlevel bleibt einzige Steuerung | Konfigurationsabschnitt: keine Änderung | Bestehende `FileLoggerTests`/`AppLogServiceTests` bleiben gültig | Abgedeckt |
| Performance/Speicher leiden nicht (1-MiB-Rotation, `IsEnabled`-Frühausstieg, `NullLogger` in Tests) | Seiteneffekte-Abschnitt | Bestehende Rotations-/IsEnabled-Tests | Abgedeckt |
| `VideoWebPlayer.Maui` (Altbestand) unberührt | Designentscheidung + Ausnahme im Contract-Test | `NoDebugWriteLine_RemainsInAppSources` (Ausnahmeliste) | Abgedeckt |
| Bestehende Tests bleiben gültig (`AppLogServiceTests`, `FileLoggerTests`, `LogProtocolContractTests`, ~39 Dienst-Erzeugungsstellen) | Optionale Logger-Parameter (`= null`); Maui-Klassen nicht testreferenziert — dokumentiert unter „Betroffene bestehende Tests" | Nicht erforderlich (kein Bruch) | Abgedeckt |
| Protokollversand-Ergebnis-Eintrag | `SendLogProtocolAsync`-Breadcrumb (Task 37) | Bestehender E2E-Fact `SendLog_Click_InvokesPlatformShareOrMail_WithoutCrash` | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] **Online-/Offline-Wechsel ohne Testnachweis:** Der Wechsel-Logeintrag (`ConnectivityNetworkStatusMonitor.AvailabilityChanged` → `LogInformation`, `ShellAppEffectHandler.AvailabilityChanged`-Handler) ist ein benannter fachlicher Ablauf der Anforderung. `ConnectivityNetworkStatusMonitor` liegt in `VideoPlayer.Maui` und ist vom Testprojekt nicht referenzierbar; der geplante E2E-Fact schaltet die Netzverfügbarkeit nicht um. Es fehlt mindestens ein Contract-Test (z. B. Quelltext-Pin in `LogProtocolContractTests`) oder ein E2E-Szenario.
- [ ] **PiP-Umschaltung ohne Testnachweis:** `PictureInPictureService.ToggleAsync`/`RestoreAsync`-Einträge und der `TogglePictureInPicture`-Breadcrumb sind benannte Information-Breadcrumbs ohne Unit-, Contract- oder E2E-Test.
- [ ] **Abmeldung ohne Testnachweis:** Die Anforderung benennt „An-/Abmeldung"; geplant ist nur der Login-Test (`LoginAsync_WritesEntries_WithoutCredentials`). Für `AuthSessionService.LogoutAsync` (unit-testbar) bzw. den `LogoutEffect`-Breadcrumb fehlt ein Test.
- [ ] **Wiedergabe-Fehlschlag ohne Testnachweis:** Die Anforderung nennt „Wiedergabe-Start/-Ende/-Fehlschlag". Der E2E-Fact deckt nur die Erfolgswiedergabe; `OnMediaFailed`/`FailPlaybackStart` (`PlayerPage`, nicht unit-testbar) werden weder per Contract-Test noch per E2E-Fehlerszenario geprüft.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Protokollierung aktivieren → reale Abläufe (Neustart, Auto-Anmeldung, Wiedergabe) → Protokolldatei mit Facheinträgen ohne Geheimnisse | Neuer Fact `LoggingEnabled_RealFlows_WriteDiagnosticEntries` in `VideoPlayer.E2E.Tests/Tests/LogProtocolTests.cs` (Task 49) | Abgedeckt |
| Schalter/Sichtbarkeit/Versand-UI (bestehender Funktionsumfang) | Bestehende 4 Facts in `LogProtocolTests` (unverändert) | Abgedeckt |
| Online-/Offline-Wechsel sichtbar im Protokoll | Nicht geplant (Netzumschaltung im E2E nicht vorgesehen; auch kein Contract-Test) | Lücke |
| PiP-Umschaltung sichtbar im Protokoll | Nicht geplant | Lücke |
| Wiedergabe-Fehlschlag sichtbar im Protokoll | Nicht geplant (E2E nur Erfolgswiedergabe) | Lücke |
| Abmeldung sichtbar im Protokoll | Nicht geplant | Lücke |

## Fehlende oder unvollständige Planbestandteile

Die Umsetzungsschritte selbst sind vollständig und konkret (alle betroffenen Klassen, Konstruktor-Strategie, `MauiProgram`-Fabriken, Paketreferenz, Reihenfolge mit Voraussetzungen, Seiteneffekte/Risiken, Testdaten-Hilfsmittel `RecordedLogEntry`/`RecordingLoggerProvider`). Lücken bestehen ausschließlich auf Testseite — siehe oben.

## Hinweise

- Einfachste Schließung der Lücken: Die `LogProtocolContractTests` (Quelltext-Contract-Tests, da `VideoPlayer.Maui` nicht referenzierbar ist) um Pins für `LogInformation`-/`LogWarning`-Aufrufe in `ConnectivityNetworkStatusMonitor.cs`, `PictureInPictureService.cs` und `PlayerPage.cs` (Fehlschlag-Pfad) erweitern; für `AuthSessionService.LogoutAsync` reicht ein Unit-Test mit `RecordingLoggerProvider`.
- Alternativ kann der E2E-Fact um Teilflüsse (Logout, Netz-Offline-Umschaltung, PiP-Toggle, Fehler-Wiedergabe) ergänzt werden — aufwendiger, aber näher am realen Nutzerfluss.
- Die geplante E2E-Assertion „≥ 1 `[INFO]`-Eintrag" ist minimal; eine Optional-Verbesserung wäre die Prüfung auf erwartete Ereignistypen (z. B. Anmeldung und Wiedergabe-Start als separate Einträge), was die fehlenden Sub-Flow-Nachweise jedoch nicht ersetzt.
