# Übersetzte Anforderung: tvOS-Serververbindung — Einrichtung, Pairing und Anmeldung

## Fachliche Zusammenfassung

Die tvOS-App (`VideoPlayer.TvOS`) erhält die vollständige Serveranbindung als
fernbedienungstaugliche Oberfläche auf dem bereits verdrahteten
MVU-Verbindungs-Workflow: Erststart-Einrichtung (Serverwahl über
Netzwerk-Erkennung, Liste bekannter Server oder manuelle Adresseingabe),
Geräte-Kopplung über die bestehenden Pairing-/Bootstrap-Endpunkte
(Einmal-Code und Ticket-/Kurzcode-Eingabe), Anmeldung mit
Benutzername/Passwort sowie automatische Wiederanmeldung mit gespeicherten
Zugangsdaten, Geräte-Token-Verwaltung und Offline-Erkennung. Die gesamte
Fachlogik (Update-Funktion, Messages, Effekte, Dienste) existiert bereits
plattformneutral; der `TvosAppEffectHandler` führt die Verbindungs-Effekte
bereits aus — neu entstehen die tvOS-Eingabe-/Anzeige-Oberflächen der
Workflow-Seiten `AppPage.ServerSetup` und `AppPage.Login` sowie der
Einstieg „Server wechseln" im Menürahmen.

## Betroffene Klassen und Komponenten

### Neue UI-Komponenten (`VideoPlayer.TvOS/Ui/`)

- `ServerSetupViewController` — Einrichtungs-/Verwaltungsseite
  (`AppPage.ServerSetup`): aktiver Server mit Erreichbarkeitsstatus,
  Liste bekannter Server (`UITableView`), Netzwerk-Erkennung mit
  Lauf-/Leer-Zustand, Liste gefundener Server (`UITableView`), manuelle
  Adresseingabe (`UITextField`), Ticket-/Kurzcode-Eingabe, Pairing-Code-
  Eingabe (sichtbar bei `IsPairingRequired`), Zugangsdaten-Eingabe
  (sichtbar bei `NeedsLogin`), Inline-Fehlertexte, „Erneut prüfen".
- `LoginViewController` — Anmeldemaske (`AppPage.Login`): Serveranzeige,
  Benutzername (einmalige Vorbefüllung aus `LoginUsername`), Passwort
  (`SecureTextEntry`), „Anmelden", Kurzcode-Eingabe, „Anderen Server
  einrichten" (`NavigateToMessage(AppPage.ServerSetup)`), Laufzustand und
  Inline-Fehler.
- `ServerListTableSource` — `UITableViewSource` für bekannte und gefundene
  Server (Titel, Untertitel, Auswahl-Callback).
- `SettingsPageViewController` — Menüseite „Einstellungen" mit dem
  Einstieg „Server wechseln" (`NavigateToMessage(AppPage.ServerSetup)`).
- `ProfilePageViewController` — Menüseite „Profil" mit „Abmelden"
  (`LogoutRequestedMessage`).

### Geänderte Komponenten

- `TvosNavigator` — ersetzt die Platzhalter-Zustandsseiten durch die neuen
  ViewController; `Render(AppModel)` übergibt das Modell an die Seiten;
  Rücknavigation per Siri-Remote-Menü-Taste vom Einrichtungsdialog zum
  Menürahmen.
- `TvosAppEffectHandler` — `LogoutEffect` wird verdrahtet
  (`AuthSessionService.LogoutAsync`).
- `MenuTabBarController` — Menüseiten „Einstellungen"/„Profil" erhalten
  die neuen ViewController statt generischer Platzhalter.
- `ConnectionStatePageViewController` — entfällt (durch die vollständigen
  Seiten ersetzt).
- `TvOsAppContractTests` / neue Contract-Testklasse — Quell-Contract-Tests
  der neuen Verdrahtung.

## Implementierungsansatz

- Die Seiten sind reine „Ereignis-Übersetzer": Benutzeraktionen dispatchen
  die vorhandenen Messages (`ServerDiscoveryRequestedMessage`,
  `DiscoveredServerSelectedMessage`, `ServerAddressSubmittedMessage`,
  `ServerSwitchRequestedMessage`, `PairingCodeSubmittedMessage`,
  `BootstrapTicketSubmittedMessage`, `LoginSubmittedMessage`,
  `NavigateToMessage`, `LogoutRequestedMessage`,
  `StartupRequestedMessage`, `LoadServersRequestedMessage`); die
  Darstellung wird ausschließlich aus `AppModel` gerendert
  (`KnownServers`, `DiscoveredServers`, `ActiveServerAddress`,
  `ServerHealth`, `IsDiscoveryRunning`, `DiscoveryAttempted`,
  `IsServerCheckRunning`, `IsPairingRequired`, `IsPairingRunning`,
  `IsBootstrapRunning`, `IsLoginRunning`, `ConnectionState`,
  `ServerSetupError`, `PairingError`, `BootstrapError`, `LoginError`,
  `LoginUsername`, `ServerHealthText`).
- Stufenlogik analog `ServerSetupPage` (MAUI): Adresswahl, solange weder
  `NeedsLogin` noch `IsPairingRequired` gilt; danach Kurzcode- und
  Zugangsdaten-Eingabe; die Pairing-Eingabe ersetzt die Zugangsdaten,
  solange `IsPairingRequired` gilt.
- Texteingabe über `UITextField` (tvOS-Tastatur inkl. Diktat/Continuity);
  QR-Scan wird nicht verdrahtet (keine Kamera).
- Keine neuen Messages/Effekte und keine Änderungen an der
  plattformneutralen Logik — Abweichung nur, wenn sich in der
  Implementierung eine Lücke zeigt.
- Validierung: lokale `net10.0`-Testsuite (Contract-Tests nach dem Muster
  `TvOsAppContractTests`); tvOS-Kompiliernachweis über den macOS-CI-Job
  `build-tvos` (erst nach Push).

## Konfiguration

Keine neuen Konfigurationseinträge. `Info.plist` enthält bereits
`NSBonjourServices` (`_http._tcp`) und `NSLocalNetworkUsageDescription` —
ausreichend für die Netzwerk-Erkennung (mDNS `MdnsServerDiscovery` +
UDP-Ausweich `UdpServerDiscovery`).

## Offene Fragen

Keine — alle fachlichen Punkte sind durch die bestehende MAUI-Vorlage
(`ServerSetupPage`, `LoginPage`, `SettingsPage`, `ProfilePage`) und die
plattformneutrale Logik eindeutig belegt.
