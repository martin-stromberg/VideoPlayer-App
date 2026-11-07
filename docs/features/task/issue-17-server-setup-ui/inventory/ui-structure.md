# Detail: UI-Struktur der `ServerSetupPage`

Datei: `VideoPlayer.Maui/Pages/ServerSetupPage.xaml` (113 Zeilen)
Code-Behind: `VideoPlayer.Maui/Pages/ServerSetupPage.xaml.cs` (116 Zeilen)

## Layout

Wurzel-`Grid` `RowDefinitions="Auto,*,Auto"`, `Padding="24"`, `RowSpacing="12"` (Z. 8–10):

| Bereich | Zeilen | Inhalt |
|---------|--------|--------|
| Kopf | 13–22 | Titel „VideoPlayer-Server" (`FontSize 24`, Bold), Hinweissatz, `ActiveServerSection` (`VerticalStackLayout`, `IsVisible=false`) mit Label „Aktiver Server" + `ActiveServerLabel` (`AutomationId`, Z. 20) |
| Mitte | 26–74 | Inneres `Grid` `Auto,*,Auto,*`: `KnownServersHeader` („Bekannte Server", Z. 30–33), `KnownServersView` (`AutomationId`, `SelectionMode=Single`, `SelectionChanged=OnKnownSelectionChanged`, Z. 34–47), Discovery-Stack (`DiscoverButton` Z. 50–53, `DiscoveryIndicator` Z. 54, `DiscoveryEmptyLabel` Z. 55–58 mit `AutomationId`), `DiscoveredServersView` (`AutomationId`, `OnDiscoveredSelectionChanged`, Z. 60–73) |
| Fuß | 77–111 | Label „Adresse manuell eingeben", `AddressEntry` (`AutomationId`, Placeholder `http://server/videoplayer/`, `Keyboard=Url`, Z. 79–82), `ConnectButton` („Verbinden", Z. 83–86), `CheckIndicator` (Z. 87), `ErrorLabel` (`AutomationId`, `VpDanger`, Z. 88), `PairingSection` (Z. 92–110): Label „Gerät koppeln" + Hinweistext, `PairingCodeEntry` (kein `MaxLength`, `IsSpellCheckEnabled/IsTextPredictionEnabled=False`, Z. 98–103), `PairButton` („Koppeln", Z. 104–107), `PairingIndicator` (Z. 108), `PairingErrorLabel` (`AutomationId`, `VpDanger`, Z. 109) |

## DataTemplates (aktuelle Listenzeilen)

Beide `CollectionView`s nutzen denselben simplen Aufbau (`Grid RowDefinitions="Auto,Auto" Padding="0,4"`):

- `KnownServersView` (Z. 40–45, `x:DataType="storage:KnownServer"`): `NameOrAddress` fett, `Address` 12pt `VpTextMuted`.
- `DiscoveredServersView` (Z. 66–70, `x:DataType="discovery:DiscoveredServer"`): identisch.

Keine `Border`-Karten, kein Status/Health-Indikator, keine Sections-Überschriften außer `KnownServersHeader`.

## Code-Behind `ServerSetupPage.xaml.cs`

- Konstruktor (Z. 19–24): `InitializeComponent`, `UpdateUi(mvuHost.Model)`.
- `OnAppearing` (Z. 27–34): `ModelChanged`-Abo + `LoadServersRequestedMessage`-Dispatch.
- `OnDisappearing` (Z. 37–41): Abo-Lösung.
- `OnModelChanged` (Z. 43–46): `MainThread.BeginInvokeOnMainThread(UpdateUi)`.
- `UpdateUi(AppModel)` (Z. 48–82): setzt `ItemsSource`s, Sichtbarkeiten, `IsRunning`, `IsEnabled`, Fehlertexte — ausschließlich aus Model-Feldern (Liste in `model.md`).
- Dispatch-Handler (Z. 84–115): `OnDiscoverClicked` → `ServerDiscoveryRequestedMessage`; `OnConnectClicked` → `ServerAddressSubmittedMessage(AddressEntry.Text)`; `OnPairClicked` → `PairingCodeSubmittedMessage(PairingCodeEntry.Text)`; `OnDiscoveredSelectionChanged` → `DiscoveredServerSelectedMessage(server.Address, server.Name)` (Selection wird zurückgesetzt); `OnKnownSelectionChanged` → `ServerSwitchRequestedMessage(server.Address, server.DisplayName)` (Selection wird zurückgesetzt).

## Navigationseinbindung

- Route `serversetup` ohne FlyoutItem: `AppShell.xaml.cs` Z. 59 `Routing.RegisterRoute("serversetup", typeof(Pages.ServerSetupPage))`; kein Eintrag in `AppShell.xaml`.
- DI: `MauiProgram.cs` Z. 108 `AddTransient<Pages.ServerSetupPage>()`.
- Einstiegspunkte: Window-Root beim Erststart (`NeedsServerSetup`), Shell-Navigation über `ServerSwitchButton` der `SettingsPage` (Z. 89–92).
