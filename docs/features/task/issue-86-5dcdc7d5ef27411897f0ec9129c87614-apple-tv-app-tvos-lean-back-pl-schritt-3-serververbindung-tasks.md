# Tasks: tvOS-Serververbindung — Einrichtung, Pairing und Anmeldung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI-Bausteine | `TvosControls` (Label-/Button-/TextField-Fabriken, Abschnitts-Stack) anlegen | Offen | — |
| 2 | UI-Bausteine | `IModelRenderable`-Interface anlegen | Offen | — |
| 3 | UI-Bausteine | `ServerListTableSource` (UITableViewSource, Untertitel-Zellen, Auswahl-Callback) anlegen | Offen | — |
| 4 | ServerSetup-Seite | `ServerSetupViewController`: Kopfbereich, aktiver Server + `ServerHealthText` | Offen | — |
| 5 | ServerSetup-Seite | Bekannte-Server-Liste an `KnownServers` + `ServerSwitchRequestedMessage` | Offen | — |
| 6 | ServerSetup-Seite | Erkennungs-Abschnitt (`ServerDiscoveryRequestedMessage`, `DiscoveredServers`-Liste → `DiscoveredServerSelectedMessage`, Lauf-/Leer-Zustand) | Offen | — |
| 7 | ServerSetup-Seite | Manuelle Adresseingabe → `ServerAddressSubmittedMessage` + Prüfstatus + `ServerSetupError` | Offen | — |
| 8 | ServerSetup-Seite | Ticket-Eingabe (Stufe 1) → `BootstrapTicketSubmittedMessage` + `BootstrapError` | Offen | — |
| 9 | ServerSetup-Seite | Stufenlogik `NeedsLogin`/`IsPairingRequired` (AuthStage ↔ AddressStage) | Offen | — |
| 10 | ServerSetup-Seite | Pairing-Eingabe (`IsPairingRequired`) → `PairingCodeSubmittedMessage` + `PairingError` | Offen | — |
| 11 | ServerSetup-Seite | Kurzcode-Eingabe (Stufe 2) → `BootstrapTicketSubmittedMessage` + Sperr-/Laufzustände | Offen | — |
| 12 | ServerSetup-Seite | Zugangsdaten-Eingabe (`!IsPairingRequired`) → `LoginSubmittedMessage` + `LoginError` + `LoginUsername`-Vorbefüllung | Offen | — |
| 13 | ServerSetup-Seite | „Erneut prüfen" (`StartupRequestedMessage`), „Zurück zum Menü" + Menü-Taste bei `Connected`, `ViewWillAppear` → `LoadServersRequestedMessage` | Offen | — |
| 14 | Login-Seite | `LoginViewController`: Serveranzeige, Benutzername/Passwort (`SecureTextEntry`) → `LoginSubmittedMessage`, einmalige Vorbefüllung | Offen | — |
| 15 | Login-Seite | Kurzcode-Abschnitt → `BootstrapTicketSubmittedMessage`, „Anderen Server einrichten" → `NavigateToMessage(AppPage.ServerSetup)`, Fehler-/Laufzustände | Offen | — |
| 16 | Menüseiten | `SettingsPageViewController` („Server wechseln" → `NavigateToMessage(AppPage.ServerSetup)`, Serverstatus) | Offen | — |
| 17 | Menüseiten | `ProfilePageViewController` („Abmelden" → `LogoutRequestedMessage`, Verbindungsanzeige) | Offen | — |
| 18 | Navigation | `TvosNavigator`: neue Seiten einhängen, `LastMenuPage`, `NavigateBackToMenu`, `Render`-Verteilung | Offen | — |
| 19 | Navigation | `MenuTabBarController`: Seiten-Fabrik (Settings/Profile neu) + `Render(AppModel)` | Offen | — |
| 20 | Effekt-Handler | `LogoutEffect`-Zweig + `LogoutAsync` in `TvosAppEffectHandler` | Offen | — |
| 21 | Aufräumen | `ConnectionStatePageViewController.cs` löschen | Offen | — |
| 22 | Tests | `TvOsConnectionContractTests` anlegen (6 Tests gemäß Plan) | Offen | — |
| 23 | Tests | `TvOsUi_UsesTabBarAndModelDrivenNavigation` an neue Dateien anpassen | Offen | — |
| 24 | Validierung | `dotnet test VideoPlayer.Maui.Tests` — Suite grün | Offen | — |
