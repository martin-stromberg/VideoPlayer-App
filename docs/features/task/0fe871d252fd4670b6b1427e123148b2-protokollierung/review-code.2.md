# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### ShellAppEffectHandler.cs (ShellAppEffectHandler)

- **Sparsamkeitsverstoß — Eintrag pro Server-Ereignis-Folgesync** — `RunPartialSyncAsync`, Zeile 583: `logger.LogInformation("Teil-Synchronisation abgeschlossen.")` läuft pro Teil-Synchronisation. Diese wird über `ServerEventReceivedMessage` → `SyncContinueWatchingEffect`/`SyncFavoritesEffect`/`SyncMetadataEffect` ausgelöst (`AppUpdate.UpdateServerEventReceived`, Z. 1242–1244). `ContinueWatchingUpdated` kann serverseitig im Takt eingehender Fortschrittsmeldungen feuern — die eigene App meldet Fortschritt alle ~5 s. Damit ist das in Iteration 1 aus `SignalRServerEventsConnection` entfernte Ereignis-Rauschen nur eine Ebene nachgelagert wieder im Protokoll. Der Eintrag benennt zudem nicht, welche Liste synchronisiert wurde (Weiterschauen/Favoriten/Metadaten).

  Empfehlung: Den Eintrag auf `LogDebug` absenken (vom `FileLogger`-`Information`-Filter verworfen) oder streichen — wie die Hub-Ereignis-Einträge in `SignalRServerEventsConnection`. Falls ein sichtbarer Sync-Breadcrumb gewollt ist, die Sync-Art mitgeben.

- **Restrauschen im Fortschrittstakt** — `ReportPlaybackProgressAsync`, Zeile 491: `logger.LogError(ex, "Fortschrittsmeldung fehlgeschlagen")` fängt alle aus `ProgressReportService` entkommenden Ausnahmen. Überholbare API-/HTTP-Fehler und permanente 4xx werden zwar im Dienst selbst behandelt (gededupt), aber persistente Fehler des `EnqueueAsync`-Pfads (z. B. SQLite-/Schreibfehler der `pending_progress`-Tabelle) erzeugen hier pro ~5-s-Takt einen ERROR-Eintrag.

  Empfehlung: Auf `LogWarning` senken (überholbarer Einzelfehler, der nächste Takt versucht erneut) oder die Ausnahme im Dienst mit in die `LogOutcomeOnce`-Deduplizierung aufnehmen.

### DownloadService.cs (DownloadService)

- **Sparsamkeitsverstoß — Information pro Dedup-Nicht-Ereignis** — `EnqueueAsync`, lokale Funktion `Logged` (Z. 245–269): schreibt für jede Rückgabe einen `Information`-Eintrag — inklusive `NoticeAlreadyRunning`, `NoticeAlreadyQueued` und `NoticeAlreadyPresent` (Z. 291–299), bei denen sich nichts geändert hat. `AutoDownloadService.RunAsync` ruft `EnqueueAsync` pro Weiterschauen-Eintrag und pro Folge-Episode auf — ein Lauf über eine bereits eingeplante Liste erzeugt so einen Eintrag pro Kandidat, obwohl kein Vorgang stattfand (pro-Item-Log).

  Empfehlung: Nur `NoticeEnqueued` als `Information` protokollieren (bzw. Dedup-Notices auf `LogDebug`/still); `IsError` bleibt `Warning`. Nebenbei: Der Quota-Pfad (Z. 343–349) erzeugt für ein Ereignis zwei Warnungen — `SetStateAsync(Failed)` + `Logged(NoticeQuotaExceeded)` — beim Umbau mitentschärfen.

### ServerSyncService.cs (ServerSyncService)

- **Doppelter Zähler-Eintrag / Nicht-Ereignis** — `FlushPendingProgressAsync`, Zeile 153: `"{SentCount} wartende Fortschritte nachgesendet."` läuft pro Sync-Lauf als `Information`. Derselbe Zähler steht bereits als `SentProgressReports` im „Synchronisation abgeschlossen"-Eintrag (Z. 91–97); bei `sent == 0` ist es ein Eintrag ohne Ereignis.

  Empfehlung: Den Eintrag nur bei `sent > 0` schreiben oder streichen — die Zähler trägt der aggregierte Abschluss-Eintrag.

### PictureInPictureService.cs (PictureInPictureService)

- **Irreführender Ergebnis-Eintrag nach verwehrtem/fehlgeschlagenem Wechsel** — `ToggleAsync`, Zeile 41: `LogInformation("PiP-Umschaltung: Mini-Player {State}.", …)` läuft nach `SetCompactOverlay` unabhängig vom Ausgang. `SetCompactOverlay` kehrt ohne Zustandsänderung zurück, wenn das Fenster/die PlatformView nicht greifbar ist (Z. 66–69), und schluckt Ausnahmen intern als `Warning` (Z. 78) — in beiden Fällen meldet der `Information`-Eintrag den unveränderten Zustand als Umschalt-Ergebnis (gleiche Befundklasse wie der in Iteration 1 beanstandete Handler-Breadcrumb, nun auf Dienst-Ebene).

  Empfehlung: Den `Information`-Eintrag nur schreiben, wenn `isActive` sich tatsächlich geändert hat (z. B. `SetCompactOverlay` einen Erfolgs-/Wechsel-Rückgabewert liefern lassen), oder den verweigerten Wechsel explizit als solchen protokollieren.

### PlayerPage.xaml.cs + AppLogService.cs (Geheimnis-Maskierung)

- **Doppelter Code** — Die `access_token`-Maskierung existiert zweimal identisch: `PlayerPage.AccessTokenPattern`/`SanitizeForLog` (PlayerPage.xaml.cs Z. ~37, ~317–320) und `AppLogService.MaskAccessToken` (AppLogService.cs Z. 186–191) — gleiche Regex `(access_token=)[^&\s"'<>]*` → `$1***`. Bei zwei Stellen können die Muster auseinanderlaufen.

  Empfehlung: Eine Implementierung teilen — z. B. die Bereinigung als `public`/`internal static` am `AppLogService` (bzw. einer kleinen Hilfsklasse in `VideoPlayer.Storage`, die `VideoPlayer.Maui` referenziert) freigeben und in `PlayerPage` wiederverwenden; die Sinken-Maskierung in `AppLogService.Write` bleibt als zweite Verteidigungslinie bestehen.

### AuthSessionService.cs + ContinueWatchingMutationService.cs (wiederholte Log-Blöcke)

- **Fehlende Kapselung / nahezu identische Blöcke** — `AuthSessionService`: der Block `logger.LogInformation("Anmeldung an {ServerHost}: {Status}.", serverAddress.Host, …)` steht viermal fast identisch im Code (Z. 138–141, 149–152, 159–162, 186–189). `ContinueWatchingMutationService`: `HideAsync` (Z. 69–76) und `SkipAsync` (Z. 99–106) enthalten denselben `if (mutated) LogInformation … else LogWarning`-Block, nur das Verb unterscheidet sich.

  Empfehlung: In `AuthSessionService` eine private Hilfsmethode `LogLoginOutcome(Uri serverAddress, AuthAttemptStatus status)` einführen; in `ContinueWatchingMutationService` das Loggen in `MutateAsync` (mit Aktionsparameter) oder eine `LogMutationOutcome`-Hilfsmethode verlagern.

## Verifizierung der Iteration-1-Befunde

Alle Befunde aus `review-code.1.md` wurden geprüft und als behoben festgestellt:

- `PlayerPage`: `location` aus `FailPlaybackStart` entfernt; `ex.Message`/`e.ErrorMessage` laufen durch `SanitizeForLog`; zusätzlich maskiert `AppLogService.MaskAccessToken` am Sinkenpunkt (Meldung + Exception). Contract-Pin `PlayerPage_LogsPlaybackFailure` prüft nun alle `FailPlaybackStart($"…")`-Zeilen auf `{location}`/`{ex.Message}`.
- `ProgressReportService`: `LogOutcomeOnce` dedupliziert Warnungen je Medium/Outcome (Zustandswechsel statt Takt); `PendingProgressSyncService.EnqueueAsync` protokolliert nur die erste Einreihung eines Mediums (`replaced == 0`).
- `PendingProgressSyncService`: Pro-Item-Eintrag in `RemoveForMediaAsync` und Lese-Eintrag in `GetPendingAsync` entfernt.
- `SignalRServerEventsConnection`: Hub-Ereignisse auf `LogDebug`; `Reconnecting`/`Reconnected`/`Closed`-Breadcrumbs erhalten.
- Schicht-Duplikate: `ShellAppEffectHandler` beschränkt sich auf Fehlerpfade; Ergebnis-Einträge liegen auf den Diensten (inkl. dokumentierter „eine Schicht pro Ereignis"-Kommentare).
- `DownloadService.Logged`: `IsError` → `LogWarning`.
- `AutoDownloadService`: `int`-Sentinel durch `int?` ersetzt; Zählung nur über `NoticeEnqueued`.
- `ServerHealthProbe`: `ILoggerFactory` → `apiClientLogger` (`CreateLogger<VideoPlayerApiClient>()`) in `VideoPlayerApiClientOptions.Logger` verdrahtet.
- `SliceMethod`: schneidet nun am nächsten Member-Marker beliebiger Sichtbarkeit (inkl. `///`, Attributen, `#if`, `}`).
- Kosmetischer Plural-Befund: mit der pro-Item-Meldung obsolet.

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
