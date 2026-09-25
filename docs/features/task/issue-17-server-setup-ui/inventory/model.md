# Detail: MVU-Modell und Dispatch-Messages

## `AppModel` (`VideoPlayer.Mvu/AppModel.cs`)

Seitenspezifisch relevante Felder (kein Feld muss für die Modernisierung ergänzt werden):

| Eigenschaft | Typ | Zeile | Zweck auf der Seite |
|-------------|-----|-------|---------------------|
| `ActiveServerAddress` | `string?` | 70 | Aktiver Server im Kopfbereich (`ActiveServerSection`/`ActiveServerLabel`) |
| `KnownServers` | `IReadOnlyList<KnownServer>` | 64 | ItemsSource der Bekannte-Server-Liste |
| `DiscoveredServers` | `IReadOnlyList<DiscoveredServer>` | 73 | ItemsSource der Gefundene-Server-Liste |
| `IsDiscoveryRunning` | `bool` | 76 | `DiscoveryIndicator`, `DiscoverButton.IsEnabled` |
| `DiscoveryAttempted` | `bool` | 79 | `DiscoveryEmptyLabel`-Sichtbarkeit |
| `IsServerCheckRunning` | `bool` | 82 | `CheckIndicator`, `ConnectButton.IsEnabled` — „Prüfung läuft" |
| `ServerSetupError` | `string?` | 88 | `ErrorLabel`-Text/Sichtbarkeit |
| `IsPairingRequired` | `bool` | 95 | `PairingSection`-Sichtbarkeit |
| `IsPairingRunning` | `bool` | 98 | `PairingIndicator`, `PairButton`/`PairingCodeEntry.IsEnabled` |
| `PairingError` | `string?` | 104 | `PairingErrorLabel`-Text/Sichtbarkeit |
| `ServerHealth` | `ServerHealthStatus` | 19 | **bislang auf dieser Seite ungenutzt** — `Unknown`/`Checking`/`Reachable`/`Unreachable` |
| `ServerHealthText` | `string` (abgeleitet) | 188–194 | „Serververbindung wird geprüft …", „Server: erreichbar", „Server: nicht erreichbar", „Server: unbekannt" |
| `ConnectionState` | `ConnectionState` | 26 | Kontext der Seite (NeedsServerSetup) — nur indirekt relevant |
| `IsOnline` | `bool` | 51 | Netzwerkstatus (wird auf der Seite aktuell nicht angezeigt) |

## `ServerHealthStatus` (`VideoPlayer.Mvu/ServerHealthStatus.cs`)

| Wert | Bedeutung |
|------|-----------|
| `Unknown` | Erreichbarkeit noch nicht geprüft |
| `Checking` | Prüfung läuft |
| `Reachable` | Server erreichbar |
| `Unreachable` | Server nicht erreichbar |

## Listen-Modelle

- `KnownServer` (`VideoPlayer.Storage/KnownServer.cs`): `Address` (kanonisch), `DisplayName` (leer möglich), `LastUsedAtUtc`, abgeleitet `NameOrAddress`.
- `DiscoveredServer` (`VideoPlayer.ApiClient/Discovery/DiscoveredServer.cs`): `Address`, `Source` (`DiscoverySource`), `Name?`, abgeleitet `NameOrAddress`.

## Dispatch-Messages (unverändert zu belassen)

| Handler | Message | Nutzlast |
|---------|---------|----------|
| `OnDiscoverClicked` | `ServerDiscoveryRequestedMessage` | – |
| `OnConnectClicked` | `ServerAddressSubmittedMessage` | `AddressEntry.Text` |
| `OnPairClicked` | `PairingCodeSubmittedMessage` | `PairingCodeEntry.Text` |
| `OnDiscoveredSelectionChanged` | `DiscoveredServerSelectedMessage` | `server.Address`, `server.Name` |
| `OnKnownSelectionChanged` | `ServerSwitchRequestedMessage` | `server.Address`, `server.DisplayName` |
| `OnAppearing` | `LoadServersRequestedMessage` | – |

Contract-Pin: `ServerSetupContractTests.ServerSetupPage_CodeBehind_DispatchesMvuMessages` prüft die vier Selection-/Submit-Messages plus `DoesNotContain("Guid")`.
