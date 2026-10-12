# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### DownloadService.cs (DownloadService)

- **Eintrag ohne Ereignis — Wiederaufnahme-Zähler pro Online-Meldung** — `ResumeInterruptedAsync`, Zeile 565: `logger.LogInformation("{RequeuedCount} unterbrochene Downloads wieder eingeplant.", toRequeue.Count)` läuft bei jedem `ResumeDownloadsEffect` unbedingt — auch bei `toRequeue.Count == 0`. Der Effekt feuert bei jedem Übergang in `Connected` **und** bei jeder Online-Meldung im bereits verbundenen Zustand (`AppUpdate.UpdateNetworkAvailabilityChanged`, Z. 1102–1106 — deckt WLAN↔Mobilfunk-Wechsel). Jede Netzwerkflanke ohne wartende Downloads erzeugt damit den Eintrag „0 unterbrochene Downloads wieder eingeplant." — dieselbe Befundklasse wie der in dieser Iteration behobene `{SentCount}`-Eintrag in `ServerSyncService.FlushPendingProgressAsync`.

  Empfehlung: Den Eintrag nur bei `toRequeue.Count > 0` schreiben (analog zur `sent > 0`-Bedingung in `FlushPendingProgressAsync`).

- **Doppelte Warnung pro Transfer-Fehlschlag** — `TransferAsync`, Zeilen 765–784: die `catch (IOException)`- und `catch (Exception)`-Blöcke schreiben je eine `LogWarning` mit der Ausnahme und rufen anschließend `SetStateIfPresentAsync(entry, Failed, …)` auf, das in `SetStateAsync` (Z. 834–843) eine zweite `LogWarning` mit dem Fehlercode schreibt — zwei Warn-Einträge für denselben Vorgang. Der Quota-Pfad wurde in dieser Iteration bewusst auf einen einzigen Eintrag (über `SetStateAsync`) entschärft; hier entsteht die Doppelung weiter (gleiches gilt sinngemäß für den `HttpRequestException`-Pfad, der nur über `SetStateIfPresentAsync` meldet — dort ist es konsistent gelöst).

  Empfehlung: Auf einen Eintrag je Fehlschlag reduzieren — z. B. die Ausnahme an `SetStateIfPresentAsync`/`SetStateAsync` durchreichen und dort im `Failed`-Eintrag mitgeben, oder den Fehlschlag-Eintrag in den catch-Blöcken belassen und `SetStateIfPresentAsync` ohne eigenen Log-Aufruf aufrufen.

### ShellAppEffectHandler.cs (ShellAppEffectHandler)

- **Residual — nicht abgefangene Ausnahmen weiterhin als `Error` pro Fortschrittstakt** — `ReportPlaybackProgressAsync`, Zeilen 488–492: `logger.LogError(ex, "Fortschrittsmeldung fehlgeschlagen")` bleibt der Catch-all hinter `ProgressReportService`. Die in dieser Iteration hinzugefügte `LogOutcomeOnce`-Dedup im Dienst deckt den `EnqueueAsync`-Pfad (z. B. SQLite-Fehler) sowie `ApiException`/`HttpRequestException`/`TaskCanceledException` ab; Ausnahmen außerhalb dieses Filters (z. B. `JsonException`, `InvalidOperationException` aus dem Client) entkommen weiter und erzeugen bei anhaltendem Fehler alle ~5 s einen ERROR-Eintrag. Zudem wird das vom Dienst bei abgebrochenem Token rethrown `OperationCanceledException` hier als `Error` protokolliert — ein routinemäßiger Abbruch ist kein Fehlschlag. Der Kommentar „nicht fatal — der nächste Takt meldet erneut" widerspricht zudem der Level-Einordnung (Error = Fehlschlag).

  Empfehlung: `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)` still durchreichen/zurückkehren lassen und den Rest auf `LogWarning` senken (überholbarer Einzelfehler; ein echter Dauerfehler wird bereits im Dienst je Medium gededupt gemeldet) — oder die Ausnahme analog in den Dienst verlagern.

## Verifizierung der Iteration-2-Befunde

Alle Befunde aus `review-code.2.md` wurden geprüft und als behoben festgestellt:

- `ShellAppEffectHandler.RunPartialSyncAsync` (Z. 578–586): Abschluss-Eintrag auf `LogDebug` abgesenkt, jetzt mit Listenangabe (`Weiterschauen`/`Favoriten`/`Metadaten` als `listName`-Parameter an den drei Aufrufstellen Z. 161, 170, 173); vom `FileLogger`-`Information`-Filter verworfen. Contract-Pin `ShellAppEffectHandler_PartialSync_StaysBelowInformation`.
- `ProgressReportService`: `EnqueueAsync`-Schreibfehler werden im Dienst gefangen und über `LogOutcomeOnce` (`"enqueue_failed"`) als `Warning` je Medium gededupt (Z. 121–135); Test `ReportProgress_EnqueueFailure_LogsWarningOncePerMedia` pinnt einmalige Warnung über zwei Takte. Residual im Handler-Catch s. Befund oben.
- `DownloadService.Logged` (Z. 243–271): nur `NoticeEnqueued` → `Information`, `IsError` → `Warning`, Dedup-Notices → `LogDebug`; Quota-Pfad (Z. 355–366) ohne zweiten Eintrag (Zustandswechsel trägt das Ereignis). Tests `EnqueueAsync_DedupNotice_LogsBelowInformation`, `EnqueueAsync_QuotaExceeded_LogsSingleWarning`.
- `ServerSyncService.FlushPendingProgressAsync` (Z. 156–162): `{SentCount}`-Eintrag nur bei `sent > 0`; Test `FlushPendingProgress_LogsSentCount_OnlyWhenSent`.
- `PictureInPictureService.SetCompactOverlay` (Z. 65–91): gibt `bool` zurück; `ToggleAsync`/`RestoreAsync` schreiben `LogInformation` nur bei echter Umschaltung, verweigerter Wechsel → `LogWarning` („Fenster nicht greifbar"), Ausnahme → `LogWarning` mit Exception. Contract-Pin `PictureInPictureService_LogsToggleRestore`.
- `AppLogService.MaskAccessToken` (Z. 194–199): `public static`, einzige Regex-Implementierung; `PlayerPage.SanitizeForLog` (Z. 307–310) delegiert. Contract-Pin `AccessTokenMasking_SharedBetweenPageAndSink` verbietet eine zweite `(access_token=)`-Stelle.
- `AuthSessionService.LogLoginOutcome` (Z. 566–572) und `ContinueWatchingMutationService.LogMutationOutcome` (Z. 127–144): gemeinsame Log-Blöcke gekapselt; Contract-Pins `AuthSessionService_LoginOutcome_SingleSharedBlock`, `ContinueWatchingMutationService_MutationOutcome_SingleSharedBlock`.

## Geheimnis- und Sparsamkeitsprüfung

- Kein `access_token`, kein `AuthAttempt.Username`, keine Passwörter in Protokolleinträgen: `PlaybackSourceService` protokolliert `Kind`/`ResumePosition`, nie `Location` (Test `ResolveAsync_LogsKind_NotLocation`); `AuthSessionService` nur Host + `AuthAttemptStatus` (Test `LoginAsync_WritesEntries_WithoutCredentials`); Sinken-Maskierung in `AppLogService.Write` als zweite Verteidigungslinie.
- Kein Eintrag im 5-s-Fortschrittstakt und keine pro-Item-Einträge: Warteschlangen-Einreihung nur beim Zustandswechsel (`replaced == 0`), Hub-Ereignisse `LogDebug`, `SetStateAsync` nur bei Zustandswechseln (Fortschritt läuft ohne Eintrag über `ReportProgressThrottledAsync`).
- `Debug.WriteLine`-Restbestand in den App-Projekten: keiner (Contract-Test `NoDebugWriteLine_RemainsInAppSources`).

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `VideoPlayer.E2E.Tests/Tests/LogProtocolTests.cs`
- `VideoPlayer.Maui/App.xaml.cs`
- `VideoPlayer.Maui/AppShell.xaml.cs`
- `VideoPlayer.Maui/MauiProgram.cs`
- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml.cs`
- `VideoPlayer.Maui/Pages/SettingsPage.xaml.cs`
- `VideoPlayer.Maui/Services/ConnectivityNetworkStatusMonitor.cs`
- `VideoPlayer.Maui/Services/PictureInPictureService.cs`
- `VideoPlayer.Maui/Services/PosterImageService.cs`
- `VideoPlayer.Maui/Services/SecureStorageSecretStore.cs`
- `VideoPlayer.Maui/Services/ServerHealthProbe.cs`
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`
- `VideoPlayer.Maui/Services/SignalRServerEventsConnection.cs`
- `VideoPlayer.Mvu/AuthSessionService.cs`
- `VideoPlayer.Mvu/AutoDownloadService.cs`
- `VideoPlayer.Mvu/ContinueWatchingMutationService.cs`
- `VideoPlayer.Mvu/DeviceTokenService.cs`
- `VideoPlayer.Mvu/DownloadService.cs`
- `VideoPlayer.Mvu/HomeListService.cs`
- `VideoPlayer.Mvu/MediaDetailsService.cs`
- `VideoPlayer.Mvu/OfflineContinueWatchingService.cs`
- `VideoPlayer.Mvu/PlaybackSourceService.cs`
- `VideoPlayer.Mvu/ProfileInfoService.cs`
- `VideoPlayer.Mvu/ProgressReportService.cs`
- `VideoPlayer.Mvu/ServerEventsService.cs`
- `VideoPlayer.Mvu/ServerSyncService.cs`
- `VideoPlayer.Mvu/ShowDetailsCache.cs`
- `VideoPlayer.Mvu/UpcomingEpisodesService.cs`
- `VideoPlayer.Mvu/VideoPlayer.Mvu.csproj`
- `VideoPlayer.Storage/AppLogService.cs`
- `VideoPlayer.Storage/BackupDatabase.cs`
- `VideoPlayer.Storage/CacheDatabase.cs`
- `VideoPlayer.Storage/PendingProgressSyncService.cs`
- `VideoWebPlayer.Maui.Tests/AuthSessionServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/AutoDownloadServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/DownloadServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/LogProtocolContractTests.cs`
- `VideoWebPlayer.Maui.Tests/PairingContractTests.cs`
- `VideoWebPlayer.Maui.Tests/PendingProgressSyncServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/PlaybackSourceServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/ProgressReportServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/ServerEventsServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/ServerSyncServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/Stubs/RecordedLogEntry.cs` (neu, untracked)
- `VideoWebPlayer.Maui.Tests/Stubs/RecordingLoggerProvider.cs` (neu, untracked)
