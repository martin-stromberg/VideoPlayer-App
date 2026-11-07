# Detail: Dev-API-Verträge und Item-Auswahl-Lücke

Quellen: `docs/help/developer-api.md`, `VideoPlayer.Mvu/DevApi/*`,
`VideoPlayer.Maui/DevApi/*`, Live-Verifikation gegen laufende Debug-App.

## Metadaten

- `devapi.json` im Exe-Verzeichnis (`AppContext.BaseDirectory`);
  Inhalt: `{"port":<int>,"pid":<int>,"startedUtc":"<iso>"}`.
  Port wird vom System vergeben (`TcpListener(IPAddress.Loopback, 0)`).
- Jede Verbindung beantwortet genau eine Anfrage (`Connection: close`);
  Verbindungen werden sequentiell bedient → der Test-Client muss
  Requests seriell stellen (kein Pipelining/Parallelität).

## `GET /model` — Response-Felder (camelCase, verifiziert live)

`currentPage` (`"Loading"|"Home"|"Favorites"|"Downloads"|"Profile"|
"Settings"|"ServerSetup"|"Login"` — `AppPage.ToString()`),
`connectionState` (`"Connecting"|"Connected"|"NeedsServerSetup"|
"NeedsLogin"|"Offline"` — `ConnectionState.ToString()`),
`serverHealth`/`serverHealthText`, `isStartupRunning`, `isLoginRunning`,
`loginError`, `loginUsername`, `isOnline`, `isOffline`, `hasOfflineData`,
`isSyncRunning`, `activeServerAddress` (kanonische Adresse oder null),
`knownServers[]` (`address`,`displayName`,`nameOrAddress`,`lastUsedAtUtc`),
`discoveredServers[]` (`address`,`name`,`nameOrAddress`,`source`),
`isDiscoveryRunning`, `discoveryAttempted`, `isServerCheckRunning`,
`serverSetupError`, `isPairingRequired`, `isPairingRunning`,
`pairingError`, `storageNotice`, `heroTitle`, `continueWatchingCount`,
`favoritesCount`, `recentEntriesCount`.

## `GET /tree` — Knotenformat

`automationId` (string|null), `type` (CLR-Name), `isVisible`,
`bounds {x,y,width,height}` (null bei Nicht-Visuals; `−1` bei
ungelayouteten Elementen), `text`, `placeholder`, `children[]`,
`items[]` (nur CollectionView: `{index,type,text,detail}` — `detail`
= `Address`-Property des Item-Modells).

## `POST /invoke` / `POST /set-text`

- Body `{"automationId":"…"}` → `SendClicked()` (Button/ImageButton),
  Toggle (Switch/CheckBox), `Activate()` (MenuItem), sonst
  `Command`-Ausführung inkl. Tap-Gesten.
- Body `{"automationId":"…","text":"…"}` → `InputView.Text = text`
  (feuert `TextChanged`).
- Fehler: `400` ungültiger Body/nicht auslösbar, `404` AutomationId
  unbekannt, `500` kein aktives Fenster; Fehlerbody `{"error":"…"}`.

## Lücke: Item-Auswahl in CollectionViews

- `GET /tree` macht Items **lesbar** (`items` mit `index`).
- `POST /invoke` kann Items **nicht** auslösen:
  `FindByAutomationId` traversiert nur Visual-Elemente; Items sind
  Datenobjekte ohne AutomationId.
- Die Seitenlogik hängt an `SelectionChanged`
  (`OnKnownSelectionChanged` → `ServerSwitchRequestedMessage`,
  `OnDiscoveredSelectionChanged` → `DiscoveredServerSelectedMessage`).
- **Geplante Erweiterung (klein, gleicher Endpunkt):**
  `POST /invoke` akzeptiert optional `"itemIndex":<n>`; ist das
  Element eine `CollectionView`, wird auf dem UI-Thread
  `SelectedItem = <Item n der ItemsSource>` gesetzt →
  `SelectionChanged` feuert → echte UI-Ereigniskette wie ein
  Fingertipp. Contract-Test `DevApiContractTests` (genau 4
  `router.Map(`-Aufrufe, kein „dispatch") bleibt gültig.

## AutomationIds (gepinnt, `ServerSetupPage.xaml` u. a.)

`AddressEntry`, `ConnectButton`, `CheckIndicator`(x:Name),
`ErrorLabel`, `PairingSection`(x:Name), `PairingCodeEntry`,
`PairButton`, `PairingErrorLabel`, `KnownServersView`,
`DiscoveredServersView`, `DiscoverButton`, `DiscoveryEmptyLabel`,
`ActiveServerLabel`, `LoginUsernameEntry`, `LoginPasswordEntry`,
`LoginButton`, `LoginErrorLabel`, `LoginServerSetupButton`,
`ServerSwitchButton` (SettingsPage).

Hinweis: `CheckIndicator`, `PairingSection` und `ActiveServerSection`
sind `x:Name` ohne `AutomationId` — über `/tree` nur als untypisierte
Knoten/Label sichtbar; Sichtbarkeits-Assertions laufen daher über
`/model`-Flags (`isPairingRequired`, `isServerCheckRunning`) statt
über den Baum.
