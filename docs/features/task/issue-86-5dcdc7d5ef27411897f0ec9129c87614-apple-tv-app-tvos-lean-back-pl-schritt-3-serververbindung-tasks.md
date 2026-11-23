# Tasks: tvOS-Serververbindung — Einrichtung, Pairing und Anmeldung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI-Bausteine | `TvosControls` (Label-/Button-/TextField-Fabriken, Abschnitts-Stack) anlegen | Erledigt | `TvOsConnectionContractTests.ServerSetupPage_DispatchesAllConnectionMessages` |
| 2 | UI-Bausteine | `IModelRenderable`-Interface anlegen | Erledigt | `TvOsConnectionContractTests.MenuPages_ExposeServerSwitchAndLogout` |
| 3 | UI-Bausteine | `ServerListTableSource` (UITableViewSource, Untertitel-Zellen, Auswahl-Callback) anlegen | Erledigt | `TvOsConnectionContractTests.ServerSetupPage_DispatchesAllConnectionMessages` |
| 4 | ServerSetup-Seite | `ServerSetupViewController`: Kopfbereich, aktiver Server + `ServerHealthText` | Erledigt | `TvOsConnectionContractTests.ServerSetupPage_RendersAllModelStates` |
| 5 | ServerSetup-Seite | Bekannte-Server-Liste an `KnownServers` + `ServerSwitchRequestedMessage` | Erledigt | `TvOsConnectionContractTests.ServerSetupPage_RendersAllModelStates` |
| 6 | ServerSetup-Seite | Erkennungs-Abschnitt (`ServerDiscoveryRequestedMessage`, `DiscoveredServers`-Liste → `DiscoveredServerSelectedMessage`, Lauf-/Leer-Zustand) | Erledigt | `TvOsConnectionContractTests.ServerSetupPage_DispatchesAllConnectionMessages` |
| 7 | ServerSetup-Seite | Manuelle Adresseingabe → `ServerAddressSubmittedMessage` + Prüfstatus + `ServerSetupError` | Erledigt | `TvOsConnectionContractTests.ServerSetupPage_DispatchesAllConnectionMessages` |
| 8 | ServerSetup-Seite | Ticket-Eingabe (Stufe 1) → `BootstrapTicketSubmittedMessage` + `BootstrapError` | Erledigt | `TvOsConnectionContractTests.ServerSetupPage_DispatchesAllConnectionMessages` |
| 9 | ServerSetup-Seite | Stufenlogik `NeedsLogin`/`IsPairingRequired` (AuthStage ↔ AddressStage) | Erledigt | `TvOsConnectionContractTests.ServerSetupPage_RendersAllModelStates` |
| 10 | ServerSetup-Seite | Pairing-Eingabe (`IsPairingRequired`) → `PairingCodeSubmittedMessage` + `PairingError` | Erledigt | `TvOsConnectionContractTests.ServerSetupPage_RendersAllModelStates` |
| 11 | ServerSetup-Seite | Kurzcode-Eingabe (Stufe 2) → `BootstrapTicketSubmittedMessage` + Sperr-/Laufzustände | Erledigt | `TvOsConnectionContractTests.ServerSetupPage_RendersAllModelStates` |
| 12 | ServerSetup-Seite | Zugangsdaten-Eingabe (`!IsPairingRequired`) → `LoginSubmittedMessage` + `LoginError` + `LoginUsername`-Vorbefüllung | Erledigt | `TvOsConnectionContractTests.ServerSetupPage_RendersAllModelStates` |
| 13 | ServerSetup-Seite | „Erneut prüfen" (`StartupRequestedMessage`), „Zurück zum Menü" + Menü-Taste bei `Connected`, `ViewWillAppear` → `LoadServersRequestedMessage` | Erledigt | `TvOsConnectionContractTests.ServerSetupPage_AllowsMenuBackNavigationWhenConnected` |
| 14 | Login-Seite | `LoginViewController`: Serveranzeige, Benutzername/Passwort (`SecureTextEntry`) → `LoginSubmittedMessage`, einmalige Vorbefüllung | Erledigt | `TvOsConnectionContractTests.LoginPage_ProvidesCredentialAndShortCodeEntry` |
| 15 | Login-Seite | Kurzcode-Abschnitt → `BootstrapTicketSubmittedMessage`, „Anderen Server einrichten" → `NavigateToMessage(AppPage.ServerSetup)`, Fehler-/Laufzustände | Erledigt | `TvOsConnectionContractTests.LoginPage_ProvidesCredentialAndShortCodeEntry` |
| 16 | Menüseiten | `SettingsPageViewController` („Server wechseln" → `NavigateToMessage(AppPage.ServerSetup)`, Serverstatus) | Erledigt | `TvOsConnectionContractTests.MenuPages_ExposeServerSwitchAndLogout` |
| 17 | Menüseiten | `ProfilePageViewController` („Abmelden" → `LogoutRequestedMessage`, Verbindungsanzeige) | Erledigt | `TvOsConnectionContractTests.MenuPages_ExposeServerSwitchAndLogout` |
| 18 | Navigation | `TvosNavigator`: neue Seiten einhängen, `LastMenuPage`, `NavigateBackToMenu`, `Render`-Verteilung | Erledigt | `TvOsAppContractTests.TvOsUi_UsesTabBarAndModelDrivenNavigation` |
| 19 | Navigation | `MenuTabBarController`: Seiten-Fabrik (Settings/Profile neu) + `Render(AppModel)` | Erledigt | `TvOsConnectionContractTests.MenuPages_ExposeServerSwitchAndLogout` |
| 20 | Effekt-Handler | `LogoutEffect`-Zweig + `LogoutAsync` in `TvosAppEffectHandler` | Erledigt | `TvOsConnectionContractTests.TvOsEffectHandler_HandlesLogoutEffect` |
| 21 | Aufräumen | `ConnectionStatePageViewController.cs` löschen | Erledigt | `TvOsAppContractTests.TvOsUi_UsesTabBarAndModelDrivenNavigation` |
| 22 | Tests | `TvOsConnectionContractTests` anlegen (7 Tests) | Erledigt | alle Tests grün (1460/1460) |
| 23 | Tests | `TvOsUi_UsesTabBarAndModelDrivenNavigation` an neue Dateien anpassen | Erledigt | Test läuft grün |
| 24 | Validierung | `dotnet test VideoPlayer.Maui.Tests` — Suite grün | Erledigt | 1460/1460 bestanden |
