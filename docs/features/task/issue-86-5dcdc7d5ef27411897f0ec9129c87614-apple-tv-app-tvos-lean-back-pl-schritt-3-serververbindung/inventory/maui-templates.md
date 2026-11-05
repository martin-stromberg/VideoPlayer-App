# MAUI-Oberflächen als Interaktionsvorlage

Die tvOS-Seiten bilden dieselbe Interaktion wie die abgenommenen
MAUI-Seiten ab — dieselben Messages, dieselben Modellfelder,
dieselbe Stufenlogik.

## `ServerSetupPage` (`VideoPlayer.Maui/Pages/ServerSetupPage.xaml[.cs]`)

- `OnAppearing` → `LoadServersRequestedMessage` (Serververwaltung nachladen).
- Kopfbereich: aktiver Server (`ActiveServerSection`, `ActiveServerLabel`)
  + Status-Badge (`ServerHealthLabel` = `model.ServerHealthText`,
  `ServerHealthIndicator` bei `Checking`).
- „Bekannte Server": `KnownServersView` an `model.KnownServers`
  (sichtbar nur bei Einträgen); Auswahl → `ServerSwitchRequestedMessage(Address, DisplayName)`.
- „Im Netzwerk suchen": `DiscoverButton` → `ServerDiscoveryRequestedMessage`;
  `DiscoveryIndicator`/`DiscoveryRunningLabel` bei `IsDiscoveryRunning`;
  `DiscoveryEmptyLabel` bei `DiscoveryAttempted && !IsDiscoveryRunning && Count == 0`;
  `DiscoveredServersView` an `model.DiscoveredServers`; Auswahl →
  `DiscoveredServerSelectedMessage(Address, Name)`.
- „Adresse selbst eingeben": `AddressEntry` + `ConnectButton` →
  `ServerAddressSubmittedMessage`; `CheckIndicator`/`CheckStatusLabel` bei
  `IsServerCheckRunning` (Text „Verbindung wird geprüft …" bzw. während
  `Connecting` „Verbindung wird hergestellt …"); `ErrorLabel` = `ServerSetupError`.
- Stufenlogik (`UpdateUi`, Z. 105–151): `authChoice =
  ConnectionState == NeedsLogin || IsPairingRequired` → `AddressStage`/
  `AuthStage` gegenseitig sichtbar.
- Stufe 1 (`AddressStage`): Kachel „Mit QR-Code verbinden" —
  `BootstrapTicketEntry` + `BootstrapButton` („Ticket einlösen") →
  `BootstrapTicketSubmittedMessage`; `BootstrapErrorLabel`; Sperrung bei
  `IsBootstrapRunning || IsServerCheckRunning`. (**tvOS: kein QR-Scan,
  nur Ticket-Texteingabe.**)
- Stufe 2 (`AuthStage`): „Mit Kurzcode anmelden" — `SetupShortCodeEntry` +
  `SetupShortCodeButton` → `BootstrapTicketSubmittedMessage`;
  `ShortCodeErrorLabel` = `BootstrapError`; Sperrung bei
  `IsBootstrapRunning || IsLoginRunning`. „Mit Zugangsdaten anmelden":
  `PairingSection` (nur bei `IsPairingRequired`) — `PairingCodeEntry` +
  `PairButton` („Koppeln") → `PairingCodeSubmittedMessage`;
  `PairingErrorLabel`; `CredentialsBlock` (nur bei `!IsPairingRequired`) —
  `LoginUsernameEntry`/`LoginPasswordEntry` + `LoginButton` →
  `LoginSubmittedMessage`; `LoginErrorLabel`; Benutzername einmalig aus
  `model.LoginUsername` vorbefüllt (`usernamePrefilled`-Guard).

## `LoginPage` (`VideoPlayer.Maui/Pages/LoginPage.xaml[.cs]`)

- `ServerLabel` = `Server: {ActiveServerAddress}` (nur bei Adresse).
- `UsernameEntry` (einmalige Vorbefüllung aus `LoginUsername`),
  `PasswordEntry` (`IsPassword`), `LoginButton` („Anmelden") →
  `LoginSubmittedMessage`; `LoginIndicator`/`Enabled`-Sperrung bei
  `IsLoginRunning`; `ErrorLabel` = `LoginError`.
- Kurzcode-Kachel: `BootstrapCodeEntry` + `BootstrapLoginButton` →
  `BootstrapTicketSubmittedMessage`; `BootstrapIndicator`; Sperrung bei
  `IsLoginRunning || IsBootstrapRunning`; `BootstrapErrorLabel`.
- `ServerSetupButton` („Anderen Server einrichten") →
  `NavigateToMessage(AppPage.ServerSetup)`.

## `SettingsPage` (`VideoPlayer.Maui/Pages/SettingsPage.xaml.cs`)

- `OnServerSwitchClicked` (Z. 160): „Server wechseln" →
  `NavigateToMessage(AppPage.ServerSetup)` — öffnet die Serververwaltung
  bei bestehender Verbindung.

## `ProfilePage` (`VideoPlayer.Maui/Pages/ProfilePage.xaml.cs`)

- `OnLogoutClicked` (Z. 75): „Abmelden" → `LogoutRequestedMessage`
  (Update: `DisconnectServerEventsEffect` + `LogoutEffect`, Reset → Login).
