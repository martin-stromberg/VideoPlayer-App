# Detail: Gepinnte Contract-Tests

## `ServerSetupContractTests` (`VideoWebPlayer.Maui.Tests/ServerSetupContractTests.cs`)

| Test | Zeilen | Pin |
|------|--------|-----|
| `ServerSetupPage_ExistsWithManualEntryAndDiscovery` | 19–37 | XAML enthält: `AddressEntry`, `ConnectButton`, `DiscoverButton`, `DiscoveredServersView`, `KnownServersView`, `ActiveServerLabel`, `ErrorLabel` |
| `ServerSetupPage_ControlsHaveAutomationIds` | 41–60 | AutomationId-Menge enthält: `KnownServersView`, `ActiveServerLabel`, `DiscoverButton`, `DiscoveredServersView`, `DiscoveryEmptyLabel`, `AddressEntry`, `ConnectButton`, `ErrorLabel` |
| `ServerSetupPage_CodeBehind_DispatchesMvuMessages` | 64–74 | Code enthält `ServerDiscoveryRequestedMessage`, `ServerAddressSubmittedMessage`, `DiscoveredServerSelectedMessage`, `ServerSwitchRequestedMessage`; enthält kein `Guid` |
| `SettingsPage_ContainsDiscreetServerSwitchEntry` | 78–84 | `SettingsPage.xaml` enthält „Server wechseln" + `ServerSwitchButton` |
| `AppShell_RegistersServerSetupRoute_WithoutFlyoutItem` | 88–98 | `serversetup`+`ServerSetupPage` in `AppShell.xaml.cs`, **nicht** in `AppShell.xaml` |
| `MauiProgram_RegistersServerManagementServices` | 102–109 | `KnownServersService`, `ServerDiscoveryService`, `ServerSetupPage` in `MauiProgram.cs` |
| `EffectHandler_HandlesAllServerEffects` | 113–127 | Effekte/Messages in `ShellAppEffectHandler.cs` + `CheckHealthAsync` in `ServerHealthProbe.cs` |
| `Discovery_UsesDocumentedProtocol` | 130–143 | Discovery-Protokoll-Konstanten (unabhängig von der Seite) |
| `KnownServersService_StoresInBackupDatabase` | 146–156 | Storage-Schicht (unabhängig) |

## `PairingContractTests` (`VideoWebPlayer.Maui.Tests/PairingContractTests.cs`)

| Test | Zeilen | Pin |
|------|--------|-----|
| `ServerSetupPage_ContainsPairingSection` | 19–41 | XAML enthält `PairingSection`, `PairingCodeEntry`, `PairButton`, `PairingIndicator`, `PairingErrorLabel`; **`DoesNotContain("MaxLength=")`** (Z. 32 — gilt für die gesamte Datei!); `IsSpellCheckEnabled="False"`, `IsTextPredictionEnabled="False"`; Hinweistext enthält „Verwaltung", **nicht** „-stellig" |
| `ServerSetupPage_PairingControlsHaveAutomationIds` | 45–59 | AutomationIds `PairingCodeEntry`, `PairButton`, `PairingErrorLabel` |
| `ServerSetupPage_PairButton_DispatchesPairingCodeSubmitted` | 63–75 | `OnPairClicked`, `Clicked="OnPairClicked"`, `PairingCodeSubmittedMessage`; `model.IsPairingRequired`/`IsPairingRunning`/`PairingError` in `UpdateUi` |
| übrige | 79–339 | Effekt-/Storage-/Plattform-Pins — von der Seiten-Restrukturierung unberührt |

**Wichtig:** `DoesNotContain("MaxLength=")` prüft die gesamte `ServerSetupPage.xaml` — kein Element der Seite darf `MaxLength` setzen.

## `DesignTokensContractTests`

- `PagesAndControls_ContainNoHardcodedColors`: keine Hexfarben/Farbliterale in `Pages/*.xaml`.
- `Xaml_UsesOnlyKnownResourceKeys`: jeder `StaticResource`-Schlüssel muss definiert sein.
- `Styles_PinsButtonAndCardRadius`/`Styles_PinsPillShapes`: Radius-Konventionen (Karte 8, Button 4, Pill 999).

## `DevApiContractTests` / `HomePageContractTests` u. a.

Nicht direkt betroffen — werden aber im Gesamtlauf mitgeprüft.
