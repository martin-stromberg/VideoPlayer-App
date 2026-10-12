# Übersetzte Anforderung: Programmprotokoll mit diagnostisch verwertbarem Inhalt

Branch: `task/0fe871d252fd4670b6b1427e123148b2-protokollierung`
Quelle: Kundenanforderung (issue.md) — die aktivierbare Protokollierung
(`AppSettings.LoggingEnabled`, Schalter „Protokollierung" auf der
`SettingsPage`) schreibt aktuell nur die Marker-Zeile
„=== Protokollierung aktiviert ===" in `videoplayer.log`; selbst nach dem
Abspielen mehrerer Videos enthält das Protokoll keine weiteren Einträge.

## Fachliche Zusammenfassung

Die bestehende Protokoll-Infrastruktur (`AppLogService`, `FileLogger`,
`FileLoggerProvider`; registriert in `MauiProgram.CreateMauiApp`) bleibt
unverändert bestehen — sie funktioniert, erhält aber schlicht keine
Einträge: Der einzige injizierte `ILogger` sitzt im
`ShellAppEffectHandler` und wird ausschließlich auf Fehlerpfaden
(`logger.LogError`, ~60 Stellen) sowie einmal im Versand-Fallback
(`LogInformation`) aufgerufen. Sämtliche übrige Diagnoseausgaben laufen
als `System.Diagnostics.Debug.WriteLine` (~53 Stellen in
`VideoPlayer.Mvu`, ~20 in `VideoPlayer.Maui`, 2 in `VideoPlayer.Storage`)
an der Datei-Protokollierung vorbei, und der bereits loggerfähige
`VideoPlayerApiClient` (`VideoPlayerApiClientOptions.Logger`, Warnungen
bei Wiederholungen/401) wird von `AuthSessionService.CreateClient` ohne
`Logger` erzeugt. Fehlerfreie Abläufe erzeugen daher keinen einzigen
Eintrag.

Der Programmcode ist um sparsame Protokollierungs-Aufrufe zu ergänzen,
die bei aktivierter Protokollierung die fachlichen Abläufe der App
erkennbar machen (Start/Initialisierung, Serververbindung und Anmeldung,
Wiedergabe, Synchronisation, Downloads, Online-/Offline-Wechsel,
Fehler). Das Protokoll soll wenig, aber verwertbar enthalten: keine
periodischen Meldungen (Fortschrittstakt, Polling), keine
Masseneinträge (pro Listeneintrag, pro Bildabruf) und keine Geheimnisse
(Zugangsdaten, Tokens). Performance und Speicherbedarf der App und der
Protokolldatei (bestehende 1-MiB-Rotation) dürfen nicht spürbar leiden.

## Betroffene Klassen und Komponenten

- **Protokoll-Infrastruktur (bestehend, unverändert bzw. minimal erweitert):**
  - `VideoPlayer.Storage.AppLogService` — schreibt `Write(LogLevel, category, message, exception)` in `videoplayer.log` (Rotation ab `MaxLogFileBytes` = 1 MiB auf `videoplayer.previous.log`); Marker beim Ein-/Ausschalten existieren bereits.
  - `VideoPlayer.Storage.FileLogger` / `FileLoggerProvider` — `ILogger`-Brücke; Level-Filter `>= Information` bleibt als Rausch-Schutz bestehen.
- **Effekt-Handler (zentraler Erweiterungspunkt):**
  - `VideoPlayer.Maui.Services.ShellAppEffectHandler` — ergänzt Erfolgs-/Verlaufs-Protokollierung (`LogInformation`) an ausgewählten der ~50 `AppEffect`-Ausführungen (z. B. `InitializeStorageEffect`, `AttemptAutoLoginEffect`/`AttemptLoginEffect`, `RunFullSyncEffect`, `ConnectServerEventsEffect`, `ResolvePlaybackSourceEffect`, `EnqueueDownloadEffect`, `LogoutEffect`); die vorhandenen `LogError`-Pfade bleiben.
- **MVU-Dienste (`VideoPlayer.Mvu`) — Umstellung `Debug.WriteLine` → injizierter `ILogger<T>`:**
  - `AuthSessionService` (Anmeldung, stille Neuanmeldung, Session-Ablauf, Abmeldung), `ServerSyncService`/`PendingProgressSyncService` (Sync-Verlauf, nachgesendete Fortschritte), `ProgressReportService` (Einreihen/Verwerfen), `ServerEventsService` (Verbindungsauf-/-abbau, `StatusChanged`), `DownloadService`/`AutoDownloadService` (Auftragszustände, Fehler), `PlaybackSourceService` (Quellauflösung), `MediaDetailsService`, `HomeListService`, `OfflineContinueWatchingService`, `UpcomingEpisodesService`, `ShowDetailsCache`, `DeviceTokenService`, `ProfileInfoService`, `ContinueWatchingMutationService`.
- **MAUI-Schicht (`VideoPlayer.Maui`) — Umstellung `Debug.WriteLine` → `ILogger<T>` bzw. neue Meldungen:**
  - `Pages/PlayerPage.xaml.cs` — Wiedergabeereignisse des `MediaElement` (`OnMediaOpened`, `OnMediaEnded`, `OnMediaFailed`, `FailPlaybackStart`, Fortsetzungs-Seek), nicht der 5-Sekunden-Fortschrittstakt.
  - `App.xaml.cs` — Start-Workflow (`StartupRequestedMessage`).
  - `AppShell.xaml.cs` — Navigations-/Quellenmenü-Fehlerpfade.
  - `Services/*` — `ServerHealthProbe`, `SignalRServerEventsConnection`, `ConnectivityNetworkStatusMonitor`, `PosterImageService`, `PictureInPictureService`, `SecureStorageSecretStore`.
  - `Pages/SettingsPage.xaml.cs`, `Pages/ServerSetupPage.xaml.cs` — verbliebene `Debug.WriteLine`-Stellen.
- **API-Client:**
  - `VideoPlayer.Mvu.AuthSessionService.CreateClient` — `VideoPlayerApiClientOptions.Logger` verdrahten, damit die vorhandenen `LogWarning`-Ausgaben des `VideoPlayerApiClient` (Transportfehler-Wiederholungen, 401/Token-Erneuerung) ins Protokoll fließen.
  - `VideoPlayer.ApiClient.VideoPlayerApiClient`/`VideoPlayerApiClientOptions` — voraussichtlich unverändert (Logger-Haken existiert).
- **Projektdateien:**
  - `VideoPlayer.Mvu.csproj` — ggf. explizite `PackageReference` auf `Microsoft.Extensions.Logging.Abstractions` (derzeit nur transitiv über `VideoPlayer.Storage` verfügbar).
  - `VideoPlayer.Storage.csproj`, `VideoPlayer.Maui.csproj` — voraussichtlich unverändert.
- **Tests:**
  - `VideoWebPlayer.Maui.Tests` — Unit-Tests für die neuen Protokollierungs-Pfade (z. B. dass Effekt-Ausführungen Einträge erzeugen, dass Geheimnisse nicht im Protokoll landen); bestehende `AppLogServiceTests`/`FileLoggerTests`/`LogProtocolContractTests` bleiben gültig.
  - `VideoPlayer.E2E.Tests` (`Tests/LogProtocolTests.cs`) — Erweiterung um eine Prüfung, dass die Protokolldatei nach realen Abläufen (Anmeldung, Wiedergabe) mehr als die Marker-Zeile enthält.

## Implementierungsansatz

- Injektion von `ILogger<T>` per Konstruktor in die betroffenen Dienste (DI-Auflösung über die bestehende `Microsoft.Extensions.Logging`-Infrastruktur; `MauiProgram` registriert bereits den `ILoggerProvider`). Konstruktor-Änderungen an `VideoPlayer.Mvu`-Diensten erfordern die Anpassung der Erzeugung in `MauiProgram` bzw. der Tests.
- Protokollinhalte auf drei Stufen einordnen:
  - `Information` für fachliche Breadcrumbs: App-Start, Ablage-Initialisierung, Server-Prüfung/-Wechsel, An-/Abmeldung (Ergebnis, nie Zugangsdaten), Quellauflösung (Medientyp/-Id, Quellenart), Wiedergabe-Start/-Ende/-Fehlschlag, PiP-Umschaltung, Sync-Beginn/-Ergebnis (Zähler des `SyncResult`), Download-Auftrag/-Abschluss, Online-/Offline-Wechsel, Verbindungsauf-/-abbau der Ereignisverbindung.
  - `Warning` für überholbare Einzelfehler (verworfene Fortschrittsmeldung, nicht lesbarer Cache, Transfer-Fehlschlag eines Downloads, Wiederholungen im API-Client).
  - `Error` mit Exception für Fehlschläge (bestehende `LogError`-Pfade des `ShellAppEffectHandler` bleiben das Vorbild).
- Sparsamkeits-Regeln: kein Protokollieren im 5-s-Fortschrittstakt (`ReportPosition`/`PlaybackProgressMessage`), nicht pro Listeneintrag/Bildabruf (`PosterImageService` ggf. nur aggregiert oder auf `Warning`-Fehlerpfade beschränkt), nicht pro MVU-Nachricht. Bewusste Auswahl von Eintrittspunkten — der `ShellAppEffectHandler.HandleAsync`-Switch ist der natürliche Breadcrumb-Punkt (ein Eintrag pro ausgewähltem Effekt statt pro Nachricht).
- Structured-Logging-Konvention: Message-Templates mit benannten Platzhaltern (`logger.LogInformation("Synchronisation abgeschlossen: {ProgressCount} Fortschritte", …)`) statt String-Interpolation; Einträge enthalten Ids/Medientypen statt kompletter URLs mit Query-Geheimnissen und niemals Passwörter/Tokens.
- `Debug.WriteLine`-Restbestände werden nicht doppelt protokolliert: Umstellung auf `ILogger` ersetzt den bisherigen Aufruf (Muster des Vorgänger-Umbaus im `ShellAppEffectHandler`); die `#if DEBUG`-`AddDebug()`-Sicht am Debugger bleibt unberührt.
- `VideoPlayerApiClientOptions.Logger` in `AuthSessionService.CreateClient` (und den kurzlebigen Refresh-/Widerrufs-Clients, soweit sinnvoll) mit einem `ILogger<VideoPlayerApiClient>` bzw. über `ILoggerFactory` verdrahten — `AuthSessionService` benötigt dafür einen injizierten `ILoggerFactory`-/`ILogger<AuthSessionService>`-Parameter.

## Konfiguration

Keine neue Konfiguration: Das Verhalten bleibt vollständig an den
bestehenden, persistenten Benutzerschalter `AppSettings.LoggingEnabled`
(Einstellungen → „Protokoll", Standard `false`) gekoppelt; bei
deaktivierter Protokollierung wird weiterhin nichts geschrieben und die
Aufrufe sind durch `FileLogger.IsEnabled` kostenneutral. Ein
konfigurierbares Protokoll-Level ist nicht gefordert (Annahme: die
eingebaute `Information`-Schwelle des `FileLogger` bleibt die einzige
Steuerung).

## Offene Fragen

- **Detailtiefe der Inhalte:** Dürfen Medientitel/Anzeigenamen ins
  Protokoll, oder sollen nur Medientyp + Id protokolliert werden
  (Datensparsamkeit beim Versand per E-Mail)? — Annahme zur Planung:
  Ids und Medientypen immer, Titel wo hilfreich und ohne Geheimnisse.
- **Umfang der Umstellung:** Sollen sämtliche ~75 verbliebenen
  `Debug.WriteLine`-Stellen der App-Projekte auf `ILogger` umgestellt
  werden, oder nur die fachlich relevanten Abläufe? — Annahme: volle
  Umstellung der App-Projekte (`VideoPlayer.Mvu`, `VideoPlayer.Maui`,
  `VideoPlayer.Storage`), da `Debug.WriteLine` im Release-Betrieb ohnehin
  unsichtbar ist; `VideoWebPlayer.Maui` (Altbestand) bleibt unberührt.
- **Geltungsbereich:** Die Anforderung bezieht sich auf die neue
  `VideoPlayer.Maui`-App (dort existiert der Protokoll-Schalter); die
  Alt-App `VideoWebPlayer.Maui` besitzt diese Einstellung nicht — zu
  bestätigen, dass dort nichts zu tun ist.
- **Protokollumfang im Fehlerfall:** Sind zusätzliche Diagnosen
  gewünscht (z. B. App-Version/Betriebssystem als Kopfzeile beim
  Aktivieren)? — Annahme: nicht erforderlich, kann optional bei der
  Planung ergänzt werden.
