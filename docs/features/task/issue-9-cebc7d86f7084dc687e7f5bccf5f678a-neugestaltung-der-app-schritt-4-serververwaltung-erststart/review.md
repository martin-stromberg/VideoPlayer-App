# Plan-Review

Geprüft: `plan.md` gegen die Implementierung (Selbstprüfung ohne Unteragenten —
dokumentierte Abweichung, es stehen keine Unteragenten zur Verfügung).
Alle Planelemente wurden im Code gesucht und vollständig gelesen.

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Projekt `VideoPlayer.Storage` (net10.0)

- [x] `ServerAddress` — `TryNormalize`/`Normalize`: Schema-Ergänzung
  (`http://`), Host-Kleinschreibung, Trailing-Slash; explizite Fremdschemata
  (`ftp://…`) werden abgelehnt.
- [x] `KnownServer` — Domänenrecord (`Address`, `DisplayName`,
  `LastUsedAtUtc`) + `NameOrAddress` für die Listenanzeige.
- [x] `KnownServersService` — `GetAllAsync` (LastUsedAtUtc absteigend),
  `GetLastConnectedAddressAsync`, `SetActiveServerAsync` (Upsert +
  `backup_state[LastConnectedServer]` + `LastUsedAtUtc`),
  `MigrateLegacyServerAddressAsync` (Übernahme des Schritt-3-Schlüssels
  `ServerAddress` aus `ISettingsStore`, profil-präfixiert, Schlüssel wird
  entfernt). Arbeitet ausschließlich auf `BackupDatabase`.

### Projekt `VideoPlayer.ApiClient`, Namespace `Discovery`

- [x] `DiscoveryProtocol` — `UdpPort = 5001`,
  `QueryText = "VIDEOWEBPLAYER_DISCOVERY"`,
  `ResponsePrefix = "VIDEOWEBPLAYER_SERVER:"`,
  `MdnsServiceType = "_http._tcp.local."`, `TryParseResponse`.
- [x] `DiscoverySource` (`Mdns`/`Udp`), `DiscoveredServer` (`Address`,
  `Source`, `Name`, `NameOrAddress`), `IServerDiscovery`.
- [x] `UdpServerDiscovery` — Broadcast `:5001`, injizierbarer interner
  Konstruktor (Ziel-Endpunkt + Zeitfenster) für den Loopback-Socket-Test;
  `localhost`-Meldung eines entfernten Responders → Responder-Adresse
  (protokollkonform, Verweis auf Server-Issue #224 — kein Workaround).
- [x] `MdnsServerDiscovery` — `ZeroconfResolver.ResolveAsync` auf
  `_http._tcp.local.` (Hinweis auf Server-Issue #223 im Kommentar).
- [x] `ServerDiscoveryService` — Composite (Probes parallel, Merge + Dedupe,
  Fehlerisolation je Probe), `CreateDefault()`.
- [x] `VideoPlayer.ApiClient.csproj` — `Zeroconf` 3.7.16 (Version wie Alt-App).

### Projekt `VideoPlayer.Mvu`

- [x] `AppPage.ServerSetup` (ohne Menü-Route, dokumentiert).
- [x] `AppModel`: `KnownServers`, `ActiveServerAddress`, `DiscoveredServers`,
  `IsDiscoveryRunning`, `IsServerCheckRunning`, `ServerSetupError`
  (+ `DiscoveryAttempted` für den „nichts gefunden"-Hinweis).
- [x] Messages: `LoadServersRequestedMessage`, `ServersLoadedMessage`,
  `ServerDiscoveryRequestedMessage`, `ServerDiscoveryCompletedMessage`,
  `ServerAddressSubmittedMessage`, `DiscoveredServerSelectedMessage`,
  `ServerSwitchRequestedMessage`, `ServerCheckCompletedMessage`.
- [x] Effekte: `LoadServersEffect`, `DiscoverServersEffect`,
  `CheckCandidateServerEffect`, `PersistActiveServerEffect`.
- [x] `AppUpdate`: Startup emittiert `[InitializeStorageEffect,
  LoadSettingsEffect, LoadServersEffect]`; `ServersLoadedMessage` verzweigt
  (aktiver Server → `CheckServerHealthEffect`; kein Server →
  `CurrentPage = ServerSetup` + `NavigateToPageEffect`); einheitlicher
  Prüfpfad für manuelle Eingabe, Discovery-Auswahl und Wechsel; nur
  `ServerCheckCompletedMessage(Success: true)` persistiert und navigiert —
  Misserfolg setzt `ServerSetupError`, nichts wird gespeichert.
- [x] `VideoPlayer.Mvu.csproj` — `ProjectReference` auf
  `VideoPlayer.ApiClient` ergänzt (Typ `DiscoveredServer` im Model).

### Projekt `VideoPlayer.Maui`

- [x] `IServerAddressProvider`/`ServerAddressProvider` — async und
  zweigeteilt (`GetConfiguredServerAddressAsync` → `null` ohne hinterlegten
  Server; `GetServerAddressAsync` → IIS-Default als Auflösungs-Fallback).
- [x] `IServerHealthProbe.CheckAsync(Uri, ct)`-Overload +
  `ServerHealthProbe`-Implementierung (eigener `HttpClient` je Kandidat,
  10 s Timeout, Fehler → `false`).
- [x] `ShellAppEffectHandler` — vier neue Effekt-Zweige;
  `NavigateAsync`-Sonderfall `ServerSetup` (Window-Root beim Erststart,
  `Shell.Current.GoToAsync("serversetup")` aus der Shell heraus).
- [x] `AppShell.xaml.cs` — `Routing.RegisterRoute("serversetup", …)`;
  `AppShell.xaml` unverändert bei sechs FlyoutItems.
- [x] `Pages/ServerSetupPage.xaml(.cs)` — aktiver Server, bekannte Server,
  Discovery-Schaltfläche + Ergebnisliste, manuelle Eingabe, Prüf-/
  Fehleranzeige; alle Bedienelemente mit `AutomationId`; ausschließlich
  MVU-Dispatch, kein `Guid`/interner Schlüssel als Nutzereingabe.
- [x] `SettingsPage` — dezenter Eintrag „Server wechseln" (`Button`,
  `AutomationId="ServerSwitchButton"`) → `NavigateToMessage(AppPage.ServerSetup)`;
  die übrigen Einstellungen werden nicht angefasst.
- [x] `MauiProgram` — `KnownServersService`, `ServerDiscoveryService`
  (`CreateDefault`), `Pages.ServerSetupPage` registriert.

### Tests

- [x] `ServerAddressTests`, `KnownServersServiceTests` (reale SQLite-Ablage:
  Upsert, Sortierung, LastConnected, Legacy-Migration, Ablageklassifikation),
  `DiscoveryProtocolTests`, `UdpServerDiscoveryTests` (echter
  Loopback-Socket-Roundtrip, Fremd-Datagramme, Timeout,
  Responder-Normalisierung), `ServerDiscoveryServiceTests` (Merge, Dedupe,
  Fehlerisolation), `AppUpdateTests_ServerManagement` (alle neuen
  Übergänge inkl. Einstellungs-Erhalt beim Wechsel),
  `ServerSetupContractTests` (Controls + AutomationIds, Route, DI,
  Effekt-Handler, Protokoll-Konstanten, Backup-Ablage).
- [x] Angepasst: `AppUpdateTests_Startup`, `AppUpdateTests_Settings`,
  `MvuHostTests`, `SettingsStorageContractTests`
  (`ServerAddressProvider_UsesKnownServersService`),
  `LocalDatabaseTests`/`KnownServersServiceTests` serialisiert über die neue
  `SqliteDatabase`-Collection (`ResetPool()` ist nicht parallelfest).

## Planabweichungen

- Control-/AutomationId-Namen weichen von den im Plan beispielhaft genannten
  ab (`AddressEntry` statt `ServerAddressEntry`, `ConnectButton` statt
  `CheckAddressButton`, `ServerSwitchButton` statt `SwitchServerLink` u. a.);
  der Contract-Test prüft die tatsächlichen Bezeichner — funktional
  identisch, konsistent zu den `x:Name`s.
- „Server wechseln" ist ein normaler `Button` statt eines
  linkgestylten Labels — dezent am Seitenende, ohne Gesten-Recognizer.
- `AppModel.DiscoveryAttempted` ergänzt: unterscheidet „Suche noch nicht
  gelaufen" von „Suche ohne Treffer" für den Hinweistext.
- `ServersLoadedMessage` löst den Health-Check nur bei laufendem Start
  (`IsStartupRunning`) aus — das Nachladen aus der geöffneten
  Serververwaltung aktualisiert nur das Model (sonst würde die Seite sofort
  wieder verlassen).

## Offene Aufgaben

—
