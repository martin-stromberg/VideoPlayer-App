# Umsetzungsplan: Serververwaltung und Erststart-Einrichtung

## Übersicht

Die App erhält eine Mehr-Server-Verwaltung auf der bereits vorhandenen
backup-pflichtigen Ablage (`BackupDatabase`: `known_servers` +
`backup_state.LastConnectedServer`). Der Start-Workflow verzweigt: ist kein
Server hinterlegt, erscheint statt der Health-Prüfung gegen die
Default-Adresse ein Einrichtungsdialog mit manueller Adresseingabe und
automatischer Netzwerk-Erkennung (mDNS `_http._tcp.local.` via Zeroconf +
UDP-Broadcast `VIDEOWEBPLAYER_DISCOVERY` → `VIDEOWEBPLAYER_SERVER:` auf Port
5001, neu implementiert in `VideoPlayer.ApiClient`). Nur erfolgreich per
`CheckHealthAsync` geprüfte Verbindungen werden gespeichert. In den
Einstellungen kommt ein bewusst unauffälliger Eintrag „Server wechseln"
dazu, der dieselbe Verwaltungsseite öffnet (bekannte Server anzeigen,
hinzufügen, wechseln) — ohne die übrigen Einstellungen zu berühren.

Discovery-Verifikation (vorab durchgeführt, `inventory/discovery-verification.md`):
UDP-Erkennung serverseitig implementiert und live beantwortet ✅; mDNS wird
serverseitig nicht beantwortet → Issue
[martin-stromberg/VideoPlayer#223](https://github.com/martin-stromberg/VideoPlayer/issues/223);
gemeldete Adresse bei Nicht-Default-Bereitstellung unbrauchbar → Issue
[#224](https://github.com/martin-stromberg/VideoPlayer/issues/224).
Keine App-Workarounds — die App prüft gefundene Adressen ohnehin per
`CheckHealthAsync`.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|------------------|------------|
| Platzierung Serververwaltung | `KnownServersService` in `VideoPlayer.Storage` (arbeitet auf `BackupDatabase`; zusätzlich `ISettingsStore`+`AppProfile` für die Legacy-Migration des Schritt-3-Schlüssels `ServerAddress`) | Plattformneutral und aus `VideoWebPlayer.Maui.Tests` laufzeittestbar — gleiches Muster wie `AppSettingsService`. |
| Domänentyp Server | `KnownServer` (record: `Address`, `DisplayName`, `LastUsedAtUtc`) als Rückgabe der Service-Schicht; `KnownServerEntry` bleibt die SQLite-Entität | Entkopplung Persistenz ↔ MVU-Model; das Model transportiert keine ORM-Entität. |
| Adress-Normalisierung | `ServerAddress.Normalize`/`TryNormalize` in `VideoPlayer.Storage`: trimmen, fehlendes Schema → `http://`, abschließender `/`; eindeutige kanonische Form für Vergleich und Speicherung | Einheitliche Form verhindert Duplikate (`Unique`-Index auf `Address`) und liefert direkt verwendbare `BaseAddress`es. |
| Platzierung Discovery | Neuer Namespace `VideoPlayer.ApiClient.Discovery`: `DiscoveryProtocol` (Konstanten + Parsing), `IServerDiscovery` (Probe-Interface), `UdpServerDiscovery`, `MdnsServerDiscovery` (Zeroconf 3.7.16 — Paket wird dem ApiClient-Projekt hinzugefügt), `ServerDiscoveryService` (Composite: Probes parallel, Merge + Dedupe nach normalisierter Adresse) | Discovery gehört fachlich zum clientseitigen Serverzugriff; plattformneutral (`net10.0`), damit Protokoll-Logik und UDP-Probe testbar sind. Zeroconf läuft auf netstandard2.0 und damit überall. |
| UDP-Testbarkeit | `UdpServerDiscovery` erhält einen internen Konstruktor mit injizierbarem Ziel-`IPEndPoint` (Produktiv: `IPAddress.Broadcast:5001`; Test: `127.0.0.1:{freier Port}`) | Ermöglicht einen echten Socket-Roundtrip-Test im Testprozess ohne Broadcast-Abhängigkeit und ohne den produktiven Port 5001 zu belegen. |
| `localhost`-Meldungen aus der Ferne | `UdpServerDiscovery` deutet eine `localhost`/Loopback-Meldung eines entfernten Antwortenden als „Responder-Adresse + gemeldeter Port/Pfad" | Protokollkonforme Interpretation (der Antwortende meint sich selbst); dokumentiert, verweist auf Server-Issue #224 — kein Protokoll-Workaround. |
| „Konfiguriert?" vs. „Effektive Adresse" | `IServerAddressProvider` wird async und zweigeteilt: `GetConfiguredServerAddressAsync()` (zuletzt verbundener Server aus `BackupDatabase` → Legacy-`ServerAddress`-Präferenz (mit Migration) → `VIDEOPLAYER_SERVER_URL` → `null`) und `GetServerAddressAsync()` (konfiguriert, sonst IIS-Default als Entwicklungs-Fallback) | Erststart-Erkennung darf den IIS-Default nicht als „hinterlegt" werten; die Health-Probe/Spätere API-Nutzung braucht weiterhin eine nie-leere Adresse. |
| Einrichtungsdialog vs. Wechsel | Eine Seite `ServerSetupPage` (`AppPage.ServerSetup`) für beide Flüsse: Erststart als Window-Root (vor der Shell), „Server wechseln" als per `Routing.RegisterRoute("serversetup")` auf den Navigationsstack geschobene Shell-Route | Ein Dialog, zwei Kontexte; kein FlyoutItem (Contract-Test `AppShell_ContainsAllMenuEntries` bleibt bei sechs Einträgen). Die Seite zeigt die bekannte-Server-Liste immer — beim Erststart ist sie leer. |
| Wechsel ohne Verlust | „Server wechseln" schreibt ausschließlich `BackupDatabase` (`known_servers`, `LastConnectedServer`); `AppSettings` werden nie angefasst | Akzeptanzkriterium 4; fachlich sind Serverdaten ohnehin kein Teil von `AppSettings`. |
| Prüfung vor Speicherung/Wechsel | Einheitlicher Prüfpfad: manuelle Eingabe, Discovery-Auswahl und Wechsel aus der Liste dispatcht jeweils `CheckCandidateServerEffect`; erst `ServerCheckCompletedMessage(success: true)` persistiert (`PersistActiveServerEffect`) und navigiert | Anforderung: „eine erfolgreich geprüfte Verbindung wird gespeichert". |

## Programmabläufe

### App-Start mit hinterlegtem Server (Akzeptanzkriterium 3)

1. `App.RunStartupAsync` dispatcht `StartupRequestedMessage`.
2. `AppUpdate.Update` → Model `{ IsStartupRunning = true, ServerHealth = Checking }`,
   Effekte `[InitializeStorageEffect, LoadSettingsEffect, LoadServersEffect]`
   (der bisherige `CheckServerHealthEffect` entfällt hier — er braucht den
   geladenen Serverzustand).
3. `ShellAppEffectHandler` auf `LoadServersEffect`: `KnownServersService`
   migriert eine ggf. vorhandene Legacy-`ServerAddress`-Präferenz in die
   Datenbank (Upsert + `LastConnectedServer` setzen + Präferenz entfernen),
   liest `known_servers` und ermittelt über `IServerAddressProvider.
   GetConfiguredServerAddressAsync()` die aktive Adresse → Rückdispatch
   `ServersLoadedMessage(servers, activeAddress)` auf dem UI-Thread.
4. `AppUpdate.Update` auf `ServersLoadedMessage` mit `activeAddress != null`
   → Model `{ KnownServers, ActiveServerAddress }`, Effekt
   `[CheckServerHealthEffect]` → bestehender Abschluss:
   `ServerHealthCheckCompletedMessage` → `NavigateToPageEffect(Home)`.
   Die App startet künftig direkt mit dem gespeicherten Server.

### Erststart ohne hinterlegten Server (Akzeptanzkriterium 1)

1.–3. wie oben.
4. `AppUpdate.Update` auf `ServersLoadedMessage` mit `activeAddress == null`
   → Model `{ IsStartupRunning = false, CurrentPage = ServerSetup,
   ServerHealth = Unknown }`, Effekt `[NavigateToPageEffect(ServerSetup)]`.
5. `ShellAppEffectHandler.NavigateAsync`: `window.Page` ist noch keine
   `AppShell` → `window.Page = ServerSetupPage` (aus DI). Der
   Einrichtungsdialog erscheint.

### Einrichtung: automatische Erkennung (Akzeptanzkriterium 2)

1. Nutzer tippt „Automatisch suchen" → `ServerDiscoveryRequestedMessage` →
   Model `{ IsDiscoveryRunning = true }`, Effekt `[DiscoverServersEffect]`.
2. `ShellAppEffectHandler` → `ServerDiscoveryService.DiscoverAsync` führt
   `MdnsServerDiscovery` und `UdpServerDiscovery` parallel aus, merged und
   dedupliziert → Rückdispatch `ServerDiscoveryCompletedMessage(found)`.
3. `AppUpdate.Update` → Model `{ IsDiscoveryRunning = false,
   DiscoveredServers = found }` → die Seite listet die gefundenen Adressen
   (bei leerem Ergebnis Hinweistext „Kein Server gefunden").

### Einrichtung: Server übernehmen (manuell oder Auswahl)

1. Manuell: Nutzer gibt Adresse ein, tippt „Verbindung prüfen" →
   `ServerAddressSubmittedMessage(text)`. `AppUpdate` normalisiert via
   `ServerAddress.TryNormalize`; ungültig → Model mit Inline-Fehlertext,
   kein Effekt. Gültig → Model `{ IsServerCheckRunning = true }`, Effekt
   `[CheckCandidateServerEffect(address)]`.
   Auswahl: Tipp auf einen Discovery-Eintrag oder einen bekannten Server →
   `DiscoveredServerSelectedMessage` bzw. `ServerSwitchRequestedMessage`
   → gleicher Prüfpfad.
2. `ShellAppEffectHandler` → `IServerHealthProbe.CheckAsync(uri, ct)` gegen
   die Kandidatenadresse → Rückdispatch
   `ServerCheckCompletedMessage(address, success)`.
3. Erfolg: `AppUpdate` → Model `{ ActiveServerAddress = address,
   KnownServers += server, IsServerCheckRunning = false,
   ServerHealth = Reachable, CurrentPage = Home }`, Effekte
   `[PersistActiveServerEffect(server), NavigateToPageEffect(Home)]`.
   `PersistActiveServerEffect` → `KnownServersService.SetActiveServerAsync`:
   Upsert in `known_servers` (`LastUsedAtUtc = now`) +
   `backup_state[LastConnectedServer] = address`.
   `NavigateAsync` tauscht die Window-Root-Page gegen die `AppShell`
   (Erststart) bzw. navigiert in der Shell nach `home` (Wechsel).
4. Misserfolg: Model `{ IsServerCheckRunning = false,
   ServerSetupError = "Server nicht erreichbar…" }` — die Seite bleibt,
   der Fehlertext wird angezeigt; nichts wird gespeichert.

### „Server wechseln" aus den Einstellungen (Akzeptanzkriterium 4)

1. `SettingsPage` zeigt am Ende einen unauffälligen Text-Eintrag
   „Server wechseln" (`TapGestureRecognizer`/linkgestylter `Button`) →
   dispatcht `NavigateToMessage(AppPage.ServerSetup)`.
2. `NavigateAsync`: `window.Page` ist `AppShell` →
   `Shell.Current.GoToAsync("serversetup")` (registrierte Route, auf den
   Stack geschoben — Zurück-Navigation funktioniert nativ).
3. Die Seite zeigt `Model.KnownServers` (Adresse + Anzeigename + „zuletzt
   verwendet"), den Discovery-Bereich und die manuelle Eingabe — identisch
   zum Erststart-Dialog. Tipp auf einen Eintrag → `ServerSwitchRequestedMessage`
   → Prüfpfad wie oben → Erfolg navigiert nach `home`.
4. `AppSettings` werden in keinem dieser Schritte gelesen oder geschrieben —
   die übrigen Einstellungen bleiben unverändert erhalten.

Beteiligte Klassen: `App`, `MvuHost`, `AppUpdate`, `AppModel`,
`ServersLoadedMessage`, `ServerDiscoveryRequestedMessage`,
`ServerDiscoveryCompletedMessage`, `ServerAddressSubmittedMessage`,
`DiscoveredServerSelectedMessage`, `ServerSwitchRequestedMessage`,
`ServerCheckCompletedMessage`, `LoadServersEffect`, `DiscoverServersEffect`,
`CheckCandidateServerEffect`, `PersistActiveServerEffect`,
`NavigateToPageEffect`, `ShellAppEffectHandler`, `KnownServersService`,
`ServerAddressProvider`, `ServerHealthProbe`, `ServerDiscoveryService`,
`UdpServerDiscovery`, `MdnsServerDiscovery`, `ServerSetupPage`,
`SettingsPage`, `AppShell`, `MauiProgram`.

## Neue Klassen

### Projekt `VideoPlayer.Storage` (net10.0)

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `ServerAddress` | statische Klasse | `TryNormalize(string?, out string)`/`Normalize` — kanonische Form `scheme://host[:port][/pfad]/` |
| `KnownServer` | record | Domänenmodell eines bekannten Servers (`Address`, `DisplayName`, `LastUsedAtUtc`) |
| `KnownServersService` | Klasse | `GetAllAsync`, `SetActiveServerAsync(address, displayName?)` (Upsert + `LastConnectedServer` + `LastUsedAtUtc`), `GetLastConnectedAddressAsync`, `MigrateLegacyServerAddressAsync` (einmalige Übernahme des `ISettingsStore`-Schlüssels `ServerAddress`) |

### Projekt `VideoPlayer.ApiClient` (net10.0), Namespace `VideoPlayer.ApiClient.Discovery`

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `DiscoverySource` | enum | `Mdns`, `Udp` |
| `DiscoveredServer` | record | `Address` (normalisiert), `Source`, `Name` (Anzeigename, optional) |
| `DiscoveryProtocol` | statische Klasse | `UdpPort = 5001`, `QueryText = "VIDEOWEBPLAYER_DISCOVERY"`, `ResponsePrefix = "VIDEOWEBPLAYER_SERVER:"`, `MdnsServiceType = "_http._tcp.local."`, `TryParseResponse` |
| `IServerDiscovery` | Interface | `DiscoverAsync(CancellationToken) → IReadOnlyList<DiscoveredServer>` — ein Protokoll-Weg je Implementierung |
| `UdpServerDiscovery` | Klasse (`IServerDiscovery`) | UDP-Broadcast-Abfrage; interner Konstruktor mit Ziel-Endpunkt für Tests; loopback→Responder-Normalisierung |
| `MdnsServerDiscovery` | Klasse (`IServerDiscovery`) | `ZeroconfResolver.ResolveAsync("_http._tcp.local.")` → `http://{ip}:{port}` |
| `ServerDiscoveryService` | Klasse | Composite über alle `IServerDiscovery`-Probes; Merge + Dedupe nach `ServerAddress.TryNormalize` |

### Projekt `VideoPlayer.Mvu` (net10.0)

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `ServersLoadedMessage` | record (`AppMessage`) | `IReadOnlyList<KnownServer> Servers`, `string? ActiveAddress` |
| `LoadServersRequestedMessage` | record (`AppMessage`) | Serververwaltung (erneut) laden — z. B. beim Öffnen der Wechsel-Seite |
| `ServerDiscoveryRequestedMessage` | record (`AppMessage`) | Netzwerk-Erkennung starten |
| `ServerDiscoveryCompletedMessage` | record (`AppMessage`) | `IReadOnlyList<DiscoveredServer>` übernehmen |
| `ServerAddressSubmittedMessage` | record (`AppMessage`) | manuelle Eingabe prüfen (`string Text`) |
| `DiscoveredServerSelectedMessage` | record (`AppMessage`) | Discovery-Eintrag gewählt (`string Address`, `string? DisplayName`) |
| `ServerSwitchRequestedMessage` | record (`AppMessage`) | bekannter Server gewählt (`string Address`, `string? DisplayName`) |
| `ServerCheckCompletedMessage` | record (`AppMessage`) | Prüfergebnis (`string Address`, `string? DisplayName`, `bool Success`) |
| `LoadServersEffect` | record (`AppEffect`) | Serverliste + aktive Adresse laden (inkl. Legacy-Migration) |
| `DiscoverServersEffect` | record (`AppEffect`) | Discovery ausführen |
| `CheckCandidateServerEffect` | record (`AppEffect`) | Kandidatenadresse per Health-Check prüfen (`Address`, `DisplayName`) |
| `PersistActiveServerEffect` | record (`AppEffect`) | aktiven Server speichern (`Address`, `DisplayName`) |

Enum-Änderung: `AppPage` erhält `ServerSetup` (keine Menü-Route).

### Projekt `VideoPlayer.Maui`

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `Pages/ServerSetupPage.xaml(.cs)` | ContentPage | Einrichtungs-/Wechsel-Dialog: Liste bekannter Server, „Automatisch suchen" + Ergebnisliste, manuelle Eingabe, Prüf-/Fehleranzeige; dispatcht die neuen Messages |

## Änderungen an bestehenden Klassen

### `AppModel` (record, `VideoPlayer.Mvu`)

- **Neue Eigenschaften:**
  - `KnownServers` (`IReadOnlyList<KnownServer>`, Default `[]`)
  - `ActiveServerAddress` (`string?`)
  - `DiscoveredServers` (`IReadOnlyList<DiscoveredServer>`, Default `[]`)
  - `IsDiscoveryRunning` (`bool`)
  - `IsServerCheckRunning` (`bool`)
  - `ServerSetupError` (`string?`) — Inline-Fehlertext der Einrichtungsseite

### `AppUpdate` (statische Klasse, `VideoPlayer.Mvu`)

- `StartupRequestedMessage`: Effektliste → `[InitializeStorageEffect, LoadSettingsEffect, LoadServersEffect]`.
- Neu: `ServersLoadedMessage` (Verzweigung Server vorhanden → `CheckServerHealthEffect`; sonst → `CurrentPage = ServerSetup` + `NavigateToPageEffect`), `LoadServersRequestedMessage` (→ `LoadServersEffect`), `ServerDiscoveryRequestedMessage`, `ServerDiscoveryCompletedMessage`, `ServerAddressSubmittedMessage` (Normalisierung + Fehlerpfad), `DiscoveredServerSelectedMessage`, `ServerSwitchRequestedMessage`, `ServerCheckCompletedMessage` (Erfolg → persistieren + Home; Misserfolg → `ServerSetupError`).

### `IServerAddressProvider` / `ServerAddressProvider` (`VideoPlayer.Maui`)

- Interface async: `Task<Uri?> GetConfiguredServerAddressAsync(CancellationToken)` und `Task<Uri> GetServerAddressAsync(CancellationToken)`.
- Impl: `KnownServersService` (zuletzt verbunden, inkl. Legacy-Migration) → `VIDEOPLAYER_SERVER_URL` → `null` bzw. `DefaultAddress`.

### `IServerHealthProbe` / `ServerHealthProbe` (`VideoPlayer.Maui`)

- Neues Overload `CheckAsync(Uri baseAddress, CancellationToken)` — prüft eine beliebige Kandidatenadresse; `CheckAsync(ct)` delegiert an das Overload mit der aufgelösten Adresse.

### `ShellAppEffectHandler` (`VideoPlayer.Maui`)

- Neue Effekt-Zweige: `LoadServersEffect` (Migration + Liste + `GetConfiguredServerAddressAsync` → `ServersLoadedMessage`), `DiscoverServersEffect` (`ServerDiscoveryService` → `ServerDiscoveryCompletedMessage`), `CheckCandidateServerEffect` (`IServerHealthProbe.CheckAsync(uri)` → `ServerCheckCompletedMessage`), `PersistActiveServerEffect` (`KnownServersService.SetActiveServerAsync`).
- `NavigateAsync`: Sonderfall `AppPage.ServerSetup` — `window.Page` ist `AppShell` → `Shell.Current.GoToAsync("serversetup")`; sonst → `window.Page = ServerSetupPage` (aus DI).

### `AppShell` (`VideoPlayer.Maui`)

- `Routing.RegisterRoute("serversetup", typeof(Pages.ServerSetupPage))` im Konstruktor.

### `SettingsPage` (`VideoPlayer.Maui`)

- XAML: unauffälliger Eintrag „Server wechseln" am Ende der Seite (`AutomationId="SwitchServerLink"`, klein/grau) + in der Wechsel-/Einrichtungsseite eingebundene Bereiche kommen dort nicht hin.
- Code-Behind: Tap-Handler dispatcht `NavigateToMessage(AppPage.ServerSetup)`.

### `MauiProgram` (`VideoPlayer.Maui`)

- Registrierungen: `KnownServersService` (Singleton), `IServerDiscovery`-Probes + `ServerDiscoveryService` (Singleton), `Pages.ServerSetupPage` (Transient).

### `VideoPlayer.ApiClient.csproj`

- `PackageReference` `Zeroconf` Version `3.7.16` (Version wie Alt-App).

## Datenbankmigrationen

Keine Schema-Migration — die Tabellen `known_servers` und `backup_state`
existieren bereits (`BackupDatabase.InitializeAsync` aus Schritt 3).
Datenmigration: einmalige Übernahme des bestehenden
`ISettingsStore`-Schlüssels `ServerAddress` (Schritt 3) in `known_servers`
+ `LastConnectedServer` beim ersten Laden (`MigrateLegacyServerAddressAsync`),
anschließend wird der Legacy-Schlüssel entfernt.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Manuelle Serveradresse | nicht leer; nach `TryNormalize` mit Host; Schema `http`/`https` | Inline-Fehlertext „Bitte eine gültige Adresse eingeben.", kein Dispatch-Effekt |
| Discovery-Antwort | nur Datagramme mit Präfix `VIDEOWEBPLAYER_SERVER:` und nicht-leerer, normalisierbarer Adresse | Datagramm wird ignoriert |
| Übernahme/Wechsel | `CheckHealthAsync` gegen die Kandidatenadresse erfolgreich | Inline-Fehlertext „Server nicht erreichbar", nichts wird gespeichert |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `VIDEOPLAYER_SERVER_URL` | Umgebungsvariable (bestehend) | — | Fallback-Serveradresse, wenn kein Server hinterlegt (zählt als „konfiguriert") |
| `DiscoveryProtocol.UdpPort` | Konstante | `5001` | UDP-Discovery-Port |
| `DiscoveryProtocol.QueryText`/`ResponsePrefix`/`MdnsServiceType` | Konstanten | siehe Protokoll | Abfrage-/Antwortschema |

## Seiteneffekte und Risiken

- **Start-Workflow:** `StartupRequestedMessage` emittiert `LoadServersEffect`
  statt `CheckServerHealthEffect`; der Health-Check folgt erst nach
  `ServersLoadedMessage` mit aktivem Server. Tests `AppUpdateTests_Startup`
  und `MvuHostTests` (Effektlisten) müssen angepasst werden.
- **Umgebungsvariable:** `VIDEOPLAYER_SERVER_URL` zählt als „konfiguriert" —
  beim Erststart mit gesetzter Variable läuft die App direkt (Dev-Szenario,
  entspricht bisherigem Verhalten).
- **IIS-Default:** `ServerAddressProvider.DefaultAddress` bleibt letzter
  Fallback der Adressauflösung, zählt aber nicht mehr als „hinterlegt" —
  ohne hinterlegten Server erscheint der Einrichtungsdialog.
- **Zeroconf-Paket:** neue Abhängigkeit von `VideoPlayer.ApiClient`
  (transitiv ins Testprojekt — unproblematisch, netstandard-kompatibel).
- **Contract-Tests:** `SettingsStorageContractTests.SettingsPage_ContainsAllSettingControls`
  zählt 4 Switches/4 Entries — „Server wechseln" darf kein Switch/Entry sein
  (Link-Button); `VideoPlayerMauiContractTests` erwartet sechs FlyoutItems —
  `ServerSetupPage` wird nicht als FlyoutItem eingebunden.
- **Server ohne mDNS:** mDNS liefert praktisch keine Treffer, bis
  Server-Issue #223 gelöst ist; unbrauchbare UDP-Meldungen werden per
  Health-Check verworfen (Issue #224). Kein Anwender-sichtbarer Defekt der
  App — die manuelle Eingabe bleibt der zuverlässige Weg.

## Umsetzungsreihenfolge

1. **Storage: `ServerAddress`, `KnownServer`, `KnownServersService`**
   - Voraussetzungen: `BackupDatabase`/`KnownServerEntry`/`BackupStateEntry`,
     `ISettingsStore`, `AppProfile` (vorhanden).
   - Normalisierungs-Helfer, Domänenrecord, Service inkl. Legacy-Migration.

2. **ApiClient: Discovery**
   - Voraussetzungen: `Zeroconf`-Paketreferenz (in diesem Schritt ergänzen),
     `ServerAddress` aus Schritt 1.
   - `DiscoveryProtocol`, `DiscoverySource`, `DiscoveredServer`,
     `IServerDiscovery`, `UdpServerDiscovery`, `MdnsServerDiscovery`,
     `ServerDiscoveryService`.

3. **Mvu: Model, Messages, Effekte, Update**
   - Voraussetzungen: Schritte 1–2 (Typen `KnownServer`, `DiscoveredServer`),
     `VideoPlayer.Mvu` referenziert `VideoPlayer.ApiClient` neu hinzu
     (`DiscoveredServer` im Model) — Projektreferenz ergänzen.
   - `AppPage.ServerSetup`, Model-Erweiterung, Messages/Effekte,
     Update-Verzweigungen.

4. **Maui: Provider/Probe umstellen, Services verdrahten**
   - Voraussetzungen: Schritte 1–3.
   - `IServerAddressProvider`/`ServerAddressProvider` (async, DB+Legacy+Env+Default),
     `IServerHealthProbe`-Overload, `ShellAppEffectHandler`-Erweiterung,
     `AppShell`-Route, `MauiProgram`-Registrierungen.

5. **Maui: `ServerSetupPage` + „Server wechseln"**
   - Voraussetzungen: Schritt 4.
   - Neue Seite (XAML + Code-Behind), `SettingsPage`-Eintrag.

6. **Tests**
   - Voraussetzungen: Schritte 1–5.
   - Neue Testklassen (siehe unten) + Anpassung der betroffenen Tests.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `ServerAddressTests` | neu | Normalisierung: Schema-Ergänzung, Trailing-Slash, Kleinschreibung des Hosts, Ablehnung ungültiger Eingaben |
| `KnownServersServiceTests` | neu | `SetActiveServerAsync` legt `known_servers`-Eintrag + `LastConnectedServer` an; Upsert dedupliziert nach Adresse; `GetAllAsync` sortiert nach `LastUsedAtUtc` absteigend; `GetLastConnectedAddressAsync` liefert `null` bei leerer DB; Legacy-Migration übernimmt `ServerAddress`-Präferenz und entfernt den Schlüssel; Profil-Isolation über `AppStorageLayout`-Unterverzeichnis |
| `DiscoveryProtocolTests` | neu | `TryParseResponse` akzeptiert `VIDEOWEBPLAYER_SERVER:<addr>`, verwirft Fremd-Datagramme und leere Adressen; Konstanten (`UdpPort`, `QueryText`, `MdnsServiceType`) entsprechen dem Protokoll |
| `UdpServerDiscoveryTests` | neu | Echter Socket-Roundtrip: Loopback-Responder auf freiem Port antwortet auf `VIDEOWEBPLAYER_DISCOVERY` mit `VIDEOWEBPLAYER_SERVER:…` → Discovery liefert die Adresse; Fremd-Datagramme werden ignoriert; `localhost`-Meldung eines entfernten Responders wird zur Responder-Adresse normalisiert |
| `ServerDiscoveryServiceTests` | neu | Merge zweier Fake-Probes, Dedupe nach normalisierter Adresse, Fehler einer Probe beeinflusst die andere nicht |
| `AppUpdateTests_ServerSetup` | neu | `ServersLoadedMessage` ohne Server → `ServerSetup`-Seite + `NavigateToPageEffect`, kein Health-Check; mit Server → `CheckServerHealthEffect`; `ServerDiscoveryRequested`/`Completed` setzen `IsDiscoveryRunning`/`DiscoveredServers`; `ServerAddressSubmitted` ungültig → `ServerSetupError` ohne Effekt, gültig → `CheckCandidateServerEffect`; `ServerCheckCompleted(false)` → `ServerSetupError`; `ServerCheckCompleted(true)` → `PersistActiveServerEffect` + `NavigateToPageEffect(Home)` + `ActiveServerAddress`; Wechsel analog; `LoadServersRequested` → `LoadServersEffect` |
| `ServerSetupContractTests` | neu | `ServerSetupPage.xaml` enthält die Bedienelemente mit `AutomationId`s (`KnownServersList`, `DiscoverButton`, `DiscoveredServersList`, `ServerAddressEntry`, `CheckAddressButton`, `SetupStatusLabel`, `SetupErrorLabel`); Code-Behind dispatcht die neuen Messages über `MvuHost`; `SettingsPage.xaml` enthält `SwitchServerLink` mit Text „Server wechseln"; `AppShell.xaml.cs` registriert Route `serversetup`; `MauiProgram` registriert `KnownServersService`, `ServerDiscoveryService`, `ServerSetupPage`; `ShellAppEffectHandler` behandelt alle neuen Effekte; `AppShell.xaml` weiterhin genau sechs FlyoutItems ohne `serversetup` |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppUpdateTests_Startup.Update_StartupRequested_SetsCheckingAndEmitsHealthCheck` | Effektliste ändert sich (`LoadServersEffect` statt `CheckServerHealthEffect`) |
| `AppUpdateTests_Startup.Update_HealthCheckCompleted_NavigatesToHome` | unverändert gültig, ggf. Model-Voraussetzung um `ActiveServerAddress` ergänzen |
| `MvuHostTests.DispatchAsync_UpdatesModelAndExecutesEffects` | erwartete Effektliste des Starts ändert sich |
| `SettingsStorageContractTests.ServerAddressProvider_UsesSettingsStore` | Provider nutzt nun `KnownServersService` (+ `ISettingsStore` nur noch für die Legacy-Migration) — Assertions anpassen |
| `SettingsStorageContractTests.EffectHandler_HandlesAllStorageEffects` | um die neuen Effekte erweitern |

### E2E-Tests (primärer Funktionsnachweis)

Es existiert kein UI-Automatisierungs-Framework im Repository (Nachweis:
`inventory/tests.md`); echte E2E-Tests auf Gerätebene sind nicht ausführbar.
Die Benutzerflüsse werden daher durch die etablierte Ersatz-Nachweiskette
abgedeckt (wie in Schritt 3 dokumentiert und akzeptiert): MVU-Update-Tests
für den exakten Fluss (Message → Model → Effekt), Contract-Tests auf die
gebaute Oberfläche (Controls, Verdrahtung, Routen, DI) sowie
Windows-Build + manuell geführter Live-Nachweis der Discovery.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Erststart ohne Server → Einrichtungsdialog | `AppUpdateTests_ServerSetup` (`ServersLoaded` ohne Adresse → `ServerSetup` + Navigate-Effekt) + `ServerSetupContractTests` (Seite existiert, Route, Root-Swap in `ShellAppEffectHandler`) | AC 1 | Benutzerfluss — kein UI-Framework vorhanden, Nachweis über MVU+Contract-Kette |
| Pflicht | Discovery liefert Auswahl | `AppUpdateTests_ServerSetup` (`DiscoveryCompleted` → `DiscoveredServers`) + `UdpServerDiscoveryTests` (echter Socket-Roundtrip) | AC 2 | wie oben |
| Pflicht | Erfolgreiche Einrichtung → künftig direkter Start | `AppUpdateTests_ServerSetup` (`CheckCompleted(true)` → persist + Home) + `KnownServersServiceTests` (Roundtrip: gesetzt → `GetLastConnectedAddressAsync`) + `AppUpdateTests_ServerSetup` (`ServersLoaded` mit Adresse → `CheckServerHealthEffect`) | AC 3 | wie oben |
| Pflicht | „Server wechseln" listet, fügt hinzu, wechselt ohne Einstellungsverlust | `ServerSetupContractTests` (`SwitchServerLink` auf `SettingsPage`, bekannte Liste auf `ServerSetupPage`) + `AppUpdateTests_ServerSetup` (`ServerSwitchRequested` → Prüfung → persist) + `AppSettingsServiceTests` (Bestand: Settings unabhängig persistiert) | AC 4 | wie oben |
| Pflicht | Fehlerpfad: ungültige Adresse / nicht erreichbarer Server wird nicht gespeichert | `AppUpdateTests_ServerSetup` (`Submitted` ungültig → Fehler ohne Effekt; `CheckCompleted(false)` → `ServerSetupError`, kein Persist-Effekt) | AC 2/4 (Nebenbedingung „erfolgreich geprüft") | wie oben |

## Offene Punkte

Keine.
