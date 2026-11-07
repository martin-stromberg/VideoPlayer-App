# Abnahmeprüfung – Entwicklungsschritt 3

## Ergebnis

**Status:** Abweichungen gefunden

## Abweichungen

- [ ] **tvOS-Build bricht: `Spacing` auf `UIView`-Feldern (CS1061).**
  In `VideoPlayer.TvOS/Ui/ServerSetupViewController.cs` sind die Felder
  `addressStage` (Z. 36) und `authStage` (Z. 45) als `UIView?` deklariert,
  in `ViewDidLoad` wird ihnen aber `Spacing` zugewiesen (`addressStage.Spacing = 30;`
  Z. 159, `authStage.Spacing = 30;` Z. 202). `Spacing` ist ausschließlich ein
  Member von `UIStackView` (Rückgabetyp von `TvosControls.Section`) — `UIView`
  besitzt diese Eigenschaft nicht und eine Erweiterung existiert im
  Repository nicht. Das ist ein garantierter Kompilierfehler (CS1061) im
  `net10.0-tvos`-Ziel; der macOS-CI-Job `build-tvos` wird fehlschlagen und
  der Erststart-Fluss ist auf Simulator/Gerät nicht lauffähig. Unter
  Windows unsichtbar, weil das Projekt dort zur leeren Bibliothek degradiert
  (`EnableDefaultCompileItems=false`). Korrektur (nicht durchgeführt —
  Prüferrolle): Feldtyp `UIStackView?`.

## Hinweise

Prüfung anhand des Diffs `task/...-apple-tv-app-tvos-lean-back-pl...HEAD`
(20 Dateien, +1299/−175) sowie der geänderten und der zugehörigen
plattformneutralen Quelldateien im Ist-Stand.

**(a) Automatische Servererkennung — fachlich verdrahtet:**
`ServerSetupViewController.cs:125-133` („Server im Netzwerk suchen" →
`ServerDiscoveryRequestedMessage`, Trefferliste →
`DiscoveredServerSelectedMessage`); `AppUpdate.cs:439-461`
(`UpdateDiscoveryRequested`/`UpdateDiscoveryCompleted` mit
`IsDiscoveryRunning`/`DiscoveryAttempted`/`DiscoveredServers`);
`TvosAppEffectHandler.cs:102-104,264-277` → `ServerDiscoveryService`
(`AppComposition.cs:81`, `CreateDefault()` = mDNS + UDP-Broadcast,
`VideoPlayer.ApiClient/Discovery/ServerDiscoveryService.cs:22-29`).

**(b) Manuelle Adresseingabe + bekannte Server + Serverwechsel — verdrahtet:**
`ServerSetupViewController.cs:136-145` (`ServerAddressSubmittedMessage`),
`:115-119,262-270` (`KnownServers`-Liste), `:359-367`
(`ServerSwitchRequestedMessage`); `AppUpdate.cs:463-504` (Normalisierung
via `ServerAddress.TryNormalize`, `BeginServerCheck`),
`:506-596` (`UpdateServerCheckCompleted`: Persistieren via
`PersistActiveServerEffect`, Verzweigung Pairing/Auto-Login);
`TvosAppEffectHandler.cs:105-110,279-331` (Kandidatenprüfung via
`IServerHealthProbe`, `KnownServers.SetActiveServerAsync`).

**(c) Pairing-Code-Flow exchange/bootstrap mit Code-Eingabe — verdrahtet:**
`PairingCodeSubmittedMessage` (Pairing-Feld, `ServerSetupViewController.cs:175-184`)
→ `ExchangePairingCodeEffect` → `DevicePairingClient.ExchangeAsync`
(`api/pairing/exchange`, ECDH via `PairingCrypto`);
`BootstrapTicketSubmittedMessage` aus Ticket-Adresse (Stufe 1, Z. 147-156)
und Kurzcode (Stufe 2, Z. 164-173 bzw. `LoginViewController.cs:95-104`)
→ `CheckBootstrapServerEffect`/`ExchangeBootstrapTicketEffect` →
`DevicePairingClient.BootstrapAsync` (`api/pairing/bootstrap`, verschlüsseltes
Session-Paket inkl. Geräte-/Auth-/Refresh-Token). Reine Texteingabe — kein
QR-Scan, keine Code-Anzeige (Contract-Test `TvOsPages_DoNotWireQrScan`
sichert das ab: kein `QrScan`/`ZXing`/`AVCapture` in den tvOS-Quellen).

**(d) Anmeldung + Kurzcode-Alternative + automatische Wiederanmeldung —
verdrahtet:** `LoginSubmittedMessage` → `AttemptLoginEffect` →
`AuthSessionService.LoginAsync`; `AttemptAutoLoginEffect` →
`TryStoredLoginAsync` (`AuthSessionService.cs:122-155`: gültiges Token
direkt, sonst gespeicherte Zugangsdaten, `MissingCredentials` →
`ReloginRequiredNotice`). Sichere Ablage: `AppSecretsService` über
`KeychainSecretStore` (`SecKeyChain`, `AppComposition.cs:35-38`).

**(e) Geräte-Token-Fluss und Offline-Erkennung — verdrahtet:**
`DeviceTokenService` (`LoadForServerAsync`, `SetPairedTokenIfCurrentAsync`,
`DiscardCurrent/IfCurrent`, `DeviceTokenRejected` →
`DeviceTokenRevokedMessage` → Re-Pairing-Zustand mit `DeviceRevokedError`,
`AppUpdate.cs:899-936`); Gate-Ablehnung in `RunHealthCheckAsync`
(`TvosAppEffectHandler.cs:347-368`). Offline: `CheckOfflineDataEffect` →
`OfflineDataCheckCompletedMessage` → `ConnectionState.Offline`
(`AppUpdate.cs:652-682`), plus `TvosNetworkStatusMonitor` (`NWPathMonitor`)
→ `NetworkAvailabilityChangedMessage` → Offline↔Reconnect-Kette
(`AppUpdate.cs:1091-1137`).

**(f) Fehlerzustände verständlich + Wiederholung — erfüllt:**
`ServerSetupError` (`InvalidAddressError`, `UnreachableServerError`),
`PairingError` (`PairingCodeFormatError`, `InvalidPairingCodeError` für
ungültig/abgelaufen, `PairingRateLimitedError` für Versuchssperre,
`PairingNotSupportedError`, `DeviceRevokedError` für verworfenes Token),
`BootstrapError` (`InvalidBootstrapInputError`, `BootstrapNeedsServerError`,
`InvalidBootstrapTicketError`, `BootstrapRateLimitedError`,
`BootstrapNotSupportedError`, `BootstrapServerUnreachableError`,
`BootstrapFailedError`), `LoginError` (`CredentialsRequiredError`,
`InvalidCredentialsError` für abgelehnte Anmeldung, `LoginUnreachableError`,
`SessionExpiredError`, `ReloginRequiredNotice`) — Mapping in
`AppUpdate.cs:750-757,889-897,3604-3613`, Inline-Darstellung in den
`Render`-Methoden der beiden Workflow-Seiten; Felder bleiben editierbar
und „Erneut prüfen" dispatcht `StartupRequestedMessage`
(`ServerSetupViewController.cs:206-208`).

**(g) Persistenz über App-Neustart — erfüllt:** `KnownServersService`
über `BackupDatabase` (aktiver Server, `PersistActiveServerEffect` →
`SetActiveServerAsync`); Geheimnisse (Zugangsdaten, Tokens, Geräte-Tokens)
über `KeychainSecretStore`/`AppSecretsService`; `LoadServersEffect` beim
Start und bei `ViewWillAppear` (`LoadServersRequestedMessage`).

**(h) Auto-Start in verbundenen Zustand — erfüllt:**
`AppDelegate.FinishedLaunching` dispatcht `StartupRequestedMessage` →
`InitializeStorage`/`LoadSettings`/`LoadServers`/`StartNetworkMonitoring`/
`LoadHomeLists` → `CheckServerHealthEffect` → erreichbar →
`AttemptAutoLoginEffect` → `AuthAttemptStatus.Succeeded` →
`EnterConnected` (`AppUpdate.cs:1032-1081`: Connected + Startseite,
Ereignisverbindung, Sync).

**(i) Kein Schreibzugriff auf Inhalte — erfüllt:** Inhalts-/Playlist-/
Download-Mutations-Effekte laufen in das protokollierte No-Op
(`TvosAppEffectHandler.cs:156-161`); Schreibzugriffe beschränken sich auf
Einstellungen, Schlüsselbund, Serververwaltung und lokale Ablagen.

**(j) Testsuite — grün:** `dotnet test VideoPlayer.Maui.Tests` (net10.0)
lokal ausgeführt: **1460/1460 bestanden, 0 Fehler** (inkl. der 7 neuen
`TvOsConnectionContractTests`). Warnung NU1903 (bekannte
SignalR.Common-Sicherheitsanfälligkeit) ist Bestand, nicht durch diesen
Schritt eingeführt.

**Einschränkungen/Randfälle (keine Anforderungsabweichungen):**
- tvOS-Kompilier- und Simulator-Nachweis steht wie vorgesehen über den
  macOS-CI-Job `build-tvos` aus — er wird mit dem obigen Befund
  fehlschlagen; lokale tvOS-Kompilierung ist unter Windows nicht möglich
  (Workload nicht installierbar). Der Befund wurde per Code-Inspektion
  festgestellt; die fachliche Verdrahtung selbst ist vollständig.
- Randfall Rückweg im Offline-Zustand: „Server wechseln" (Einstellungen)
  ist auch bei `ConnectionState.Offline` erreichbar (`UpdateNavigateTo`
  sperrt Menüseiten nur bei `NeedsLogin`/`NeedsServerSetup`), doch
  „Zurück zum Menü" und die Menü-Tasten-Umleitung sind nur bei
  `Connected` aktiv (`ServerSetupViewController.cs:231,343`) — der
  Anwender kommt im Offline-Fall nur über das Systemverhalten der
  Menü-Taste bzw. eine erfolgreiche Serverwahl zurück. Kleines
  UX-Manko außerhalb des Erststart-Flusses; Workaround vorhanden
  (App-Neustart führt bei Offline-Daten wieder ins Menü).
- `LoadSourcesEffect`/`ResumeDownloadsEffect` aus `EnterConnected`
  laufen auf tvOS bewusst ins No-Op (Inhaltsbereiche folgen in späteren
  Schritten); fachlich ohne Oberfläche und daher unschädlich.
