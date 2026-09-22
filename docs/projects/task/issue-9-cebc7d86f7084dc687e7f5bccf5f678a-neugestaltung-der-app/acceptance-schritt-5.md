# Abnahmeprüfung – Entwicklungsschritt 5

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

- Keine.

## Hinweise

Geprüft auf Branch `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-5-verbindung-anmeldung` (HEAD: `18e997b`), Diff gegen `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app` (45 Dateien, +3904/−36).

### Start-Workflow mit vier Zustandsausgängen

- `App.xaml.cs:22-46` startet mit `LoadingPage` als Window-Root und dispacht `StartupRequestedMessage`. `AppUpdate.UpdateStartupRequested` (`VideoPlayer.Mvu/AppUpdate.cs:63-80`) setzt `ConnectionState.Connecting` und emittiert `[InitializeStorageEffect, LoadSettingsEffect, LoadServersEffect]`.
- `UpdateServersLoaded` (`AppUpdate.cs:97-127`): ohne hinterlegten Server → `NeedsServerSetup` + `NavigateToPageEffect(ServerSetup)`; mit aktivem Server → `CheckServerHealthEffect`.
- `UpdateHealthCheckCompleted` (`AppUpdate.cs:227-248`): erreichbar → `AttemptAutoLoginEffect` (automatische Anmeldung mit gespeicherten Zugangsdaten); nicht erreichbar → `CheckOfflineDataEffect`.
- `UpdateAutoLoginCompleted` (`AppUpdate.cs:279-311`): `Succeeded` → `Connected` + Home; `MissingCredentials`/Fehlschlag → `NeedsLogin` + `Login` mit Fehlertext-Mapping (`LoginErrorFor`, `AppUpdate.cs:391-400`) und Benutzernamen-Vorbefüllung.
- `UpdateOfflineDataCheckCompleted` (`AppUpdate.cs:250-277`): Offline-Daten vorhanden → `Offline` + Home; keine → `NeedsServerSetup` + Serverauswahl.
- Alle vier Ausgänge (verbunden, Anmeldung, Servereinrichtung, offline) sind nachvollziehbar implementiert und testabgedeckt (`AppUpdateTests_Startup.cs`, `AppUpdateTests_Connection.cs`, Sequenztest in `MvuHostTests.cs`).

### Verbindungszustände steuern die Navigation durchgehend

- `ConnectionState`-Enum (`VideoPlayer.Mvu/ConnectionState.cs`) mit `Unknown`, `Connecting`, `Connected`, `NeedsLogin`, `NeedsServerSetup`, `Offline`; im `AppModel` durchgehend bekannt (`AppModel.cs:26`), berechnetes `IsOffline` (`AppModel.cs:44`).
- Zustandsgesteuerte Guards: `UpdateNavigateTo`/`UpdateNavigated` (`AppUpdate.cs:402-444`) sperren Menüseiten bei `NeedsLogin`/`NeedsServerSetup` und führen Flyout-Navigation auf die ausstehende Workflow-Seite zurück; „Anderen Server einrichten" von der Anmeldemaske bleibt erlaubt. Testabgedeckt (`NavigateTo_MenuPage_BlockedWhileWorkflowPending`, `Navigated_MenuPageWhileWorkflowPending_NavigatesBack` u. a.).
- Offline-Banner auf der Startseite (`HomePage.xaml` `OfflineBanner`, Sichtbarkeit über `model.IsOffline`, `HomePage.xaml.cs:37-40`).

### Stille Token-Erneuerung (echte Neuanmeldung, nicht nur Client-Refresh)

- `AuthSessionService.CreateClient` (`VideoPlayer.Mvu/AuthSessionService.cs:143-152`) verdrahtet `VideoPlayerApiClientOptions.TokenRefresher` mit `SilentReloginAsync` (`Zeilen 161-197`): Bei 401 meldet ein eigener kurzlebiger `VideoPlayerApiClient` mit den gespeicherten Zugangsdaten erneut an (`api/auth/login`), das neue Token wird über `SetAuthTokenAsync` persistiert und die ursprüngliche Anfrage mit neuem Bearer-Token neu gesendet (`VideoPlayerApiClient.cs:429-443`, `TryRefreshTokenAsync` serialisiert per `SemaphoreSlim`).
- Fehlende oder endgültig abgelehnte Zugangsdaten lösen `SessionExpired` aus → `ShellAppEffectHandler.OnSessionExpired`/`DispatchSessionExpiredAsync` (`ShellAppEffectHandler.cs:132-151`) dispacht `SessionExpiredMessage` → `NeedsLogin` + `SessionExpiredError` auf der Anmeldemaske (`AppUpdate.cs:359-375`). Transiente Fehler lösen das Ereignis nicht aus.
- Testnachweis: `AuthSessionServiceTests.TokenRefresh_SilentlyReloginsAndRetriesRequest` (HTTP-Stub: 401 → stille Neuanmeldung → Neuversand mit `Bearer jwt-neu`, Token persistiert), `TokenRefresh_WithoutCredentials_RaisesSessionExpired`, `TokenRefresh_RejectedCredentials_RaisesSessionExpired`, `TokenRefresh_TransientError_DoesNotRaiseSessionExpired`; ergänzend `VideoPlayerApiClientTests_TokenRefresh` (einmaliger Refresh, Fehlschlag, Doppel-401, Login ohne Reauth).

### Offline-Pfad mit Daten-Bestandsprüfung

- `CheckOfflineDataEffect` → `ShellAppEffectHandler.CheckOfflineDataAsync` (`ShellAppEffectHandler.cs:113-130`) ruft `CacheDatabase.HasOfflineDataAsync` (`VideoPlayer.Storage/CacheDatabase.cs:42-58`): `true`, sobald `cached_lists`, `cached_favorites` oder `download_manifest` Einträge enthalten; Fehler der Prüfung gelten als „keine Offline-Daten" → Serverauswahl statt Hängenbleiben.
- Testabgedeckt: `LocalDatabaseTests.CacheDatabase_HasOfflineData_*` gegen reale SQLite-Ablage sowie die MVU-Verzweigungstests `OfflineDataCheckCompleted_WithData_EntersOfflineState`/`_WithoutData_GoesToServerSetup`.

### Anmeldemaske

- `VideoPlayer.Maui/Pages/LoginPage.xaml` + `.xaml.cs`: Benutzername/Passwort (`IsPassword="True"`), Laufzustand (`LoginIndicator`, Controls gesperrt bei `IsLoginRunning`), Inline-Fehler (`ErrorLabel` an `LoginError`), Anzeige des aktiven Servers, einmalige Vorbefüllung des bekannten Benutzernamens, „Anderen Server einrichten" → `NavigateToMessage(AppPage.ServerSetup)`; alle Controls mit `AutomationId`.
- Anbindung: `LoginSubmittedMessage` → `AttemptLoginEffect` → `AuthSessionService.LoginAsync` → `LoginCompletedMessage`; Shell-Route `login` ohne Menüeintrag (`AppShell.xaml.cs:21`), Navigation als Window-Root beim Start bzw. geschobene Route bei Sitzungsablauf (`ShellAppEffectHandler.cs:323-335`), DI-Registrierung (`MauiProgram.cs:19,49`).

### Wiederholungsstrategie

- In `VideoPlayerApiClient.SendWithPolicyAsync` (`VideoPlayer.ApiClient/VideoPlayerApiClient.cs:394-461`, unverändert aus Schritt 1): Initialversuch plus bis zu drei Wiederholungen mit den Warteintervallen 1 s/2 s/4 s (`RetryDelays`, `MaxRetries = 3`); wiederholt werden 5xx, 408, 429 sowie Transportfehler/Timeouts, übrige 4xx nicht; 401 löst die einmalige Token-Erneuerung außerhalb des Retry-Budgets aus. Die Auslegung „drei Warteintervalle = drei Wiederholungen nach dem Erstversuch" wurde bereits in Schritt 1 begründet und abgenommen (`acceptance-schritt-1.md`).
- Testabdeckung: `VideoPlayerApiClientTests_RetryPolicy` (exakte Delays, Erschöpfung nach 4 Requests, kein Retry bei 400/403/404/422, Retry bei 408/429/Transport/Timeout, kein Retry bei Caller-Abbruch).

### Backup-Wiederherstellung

- Serverliste und zuletzt verbundener Server liegen backup-pflichtig in der `BackupDatabase` (Schritt 4); Geheimnisse liegen über `SecureStorageSecretStore` (`SecureStorage`, iOS-Keychain/Windows-Credential-Ablage) außerhalb des Cloud-Backups; der nicht geheime Benutzername bleibt in der normalen Ablage erhalten (`AppSecretsService.cs:28-48`).
- Nach Restore: `ServersLoadedMessage` liefert die aktive Adresse → Erreichbarkeitsprüfung → `TryStoredLoginAsync` → `MissingCredentials` → Anmeldemaske mit vorbefülltem Benutzernamen. Testnachweis: `AuthSessionServiceTests.TryStoredLogin_UsernameOnly_ReturnsMissingCredentialsWithUsername`, `AppUpdateTests_Connection.AutoLoginCompleted_MissingCredentials_NavigatesToLoginWithoutError`.

### Testnachweis

- `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release` im Prüfschritt ausgeführt: **280/280 Tests bestanden** (0 fehlgeschlagen, 0 übersprungen, ~3 s). Deckt sich mit dem dokumentierten Implementierungslauf (`docs/features/task/…-schritt-5-verbindung-anmeldung/final-dotnet-test.log`, Baseline 229 → 280 Tests).

### Einschränkungen

- Ein UI-Laufzeittest auf iOS/Windows ist nicht möglich (kein UI-Automatisierungs-Framework im Repository; das Testprojekt kann `VideoPlayer.Maui` nicht referenzieren). Etablierter Ersatz wie in den Schritten 2–4: vollständige Message-Sequenz-Tests auf MVU-Ebene plus Contract-Tests der UI-Verdrahtung (`ConnectionWorkflowContractTests`). Der Windows-Build der MAUI-App ist im Implementierungslauf dokumentiert (0 Warnungen, 0 Fehler, `test-results.md`).
- Nach erfolgreicher Kandidatenprüfung in der Serververwaltung verbleibt die App bis zum Abschluss der automatischen Anmeldung auf der Einrichtungsseite mit laufender Anzeige (`IsServerCheckRunning`) — bewusste Gestaltung, da das Anmeldeergebnis über das Navigationsziel entscheidet (`AppUpdate.cs:210-224`).
