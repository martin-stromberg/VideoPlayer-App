# Testergebnisse

**Status:** Keine Fehler

## Ausgeführte Verifikationen

| Prüfung | Ergebnis |
|---------|----------|
| Build aller geänderten Projekte (`VideoPlayer.Storage`, `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`, `VideoPlayer.Maui` Windows, `VideoWebPlayer.Maui.Tests`), Release | erfolgreich, 0 Fehler |
| `dotnet test VideoWebPlayer.Maui.Tests` (Release, mit Coverage-Sammlung) | 229 Tests: 229 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Code-Coverage (`--collect:"XPlat Code Coverage"`) | gemessen: gesamt 68,9 % Zeilen — `VideoPlayer.Storage` 99,6 %, `VideoPlayer.Mvu` 95,0 %, `VideoPlayer.ApiClient` 83,2 % (`VideoWebPlayer.Client` 7,3 %, Altbestand) |
| Windows-Build `VideoPlayer.Maui` (`net10.0-windows10.0.19041.0`, Release) | erfolgreich, 0 Warnungen, 0 Fehler (`windows-build.log`) |
| Windows-Smoke-Run | App gestartet, lief 15 s stabil (Erststart-Einrichtungsdialog, kein Server hinterlegt), wurde sauber beendet |
| `dotnet format whitespace --verify-no-changes` (geänderte Dateien) | Exit-Code 0 |
| iOS/MacCatalyst-/Android-Build | lokal auf Windows nicht ausgeführt — plattformneutraler Code (`VideoPlayer.Storage`, `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`) ist vollständig net10.0-getestet; MAUI-spezifische Verdrahtung über Contract-Tests |
| Discovery-Verifikation am lokalen IIS „VideoPlayer" | UDP-Antwort prinzipiell vorhanden, aber Adressmeldung ohne `/videoplayer`-Kontext bzw. `localhost` → Issue [#224](https://github.com/martin-stromberg/VideoPlayer/issues/224); mDNS `_http._tcp.local.` ohne Announce → Issue [#223](https://github.com/martin-stromberg/VideoPlayer/issues/223). Kein App-Workaround (Vorgabe); Details: `inventory/discovery-verification.md`, Prüfskript `inventory/discovery-check.ps1` |

Befehl (Arbeitsverzeichnis: Repository-Wurzel, Exit-Code 0):

```
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"
```

Protokoll: `final-dotnet-test.log` (Coverage-Anlage: `coverage.cobertura.xml`
unter `VideoWebPlayer.Maui.Tests/TestResults/`).

## Baseline vs. Ergebnis

- Baseline (vor Implementierung): 160 Tests, 160 bestanden.
- Nach Implementierung: 229 Tests, 229 bestanden (69 neue Tests).

## Neue Tests (Auswahl)

- `ServerAddressTests` — Normalisierung (Schema-Ergänzung, Host-
  Kleinschreibung, Trailing-Slash) und Ablehnung ungültiger Eingaben
  (inkl. Fremdschemata wie `ftp://`).
- `KnownServersServiceTests` — reale SQLite-Ablage: Upsert ohne Duplikat,
  `LastUsedAtUtc`-Sortierung, `LastConnectedServer` in `backup_state`,
  Legacy-Migration des Schritt-3-Schlüssels `ServerAddress` (Übernahme +
  Schlüssel-Entfernung, ungültiger Wert wird verworfen), Ablage in der
  Backup-Datenbank (Cache-DB enthält die Tabellen nicht).
- `DiscoveryProtocolTests` — Protokoll-Konstanten und Antwort-Parsing
  (Präfix-Prüfung, Leer-/Fremd-Datagramme).
- `UdpServerDiscoveryTests` — echter UDP-Loopback-Roundtrip mit Responder,
  Ignorieren von Fremd-Datagrammen, Timeout ohne Antwort,
  `localhost`→Responder-Normalisierung.
- `ServerDiscoveryServiceTests` — Merge über Probes, Dedupe nach Adresse,
  Fehlerisolation (eine defekte Probe beeinflusst andere nicht).
- `AppUpdateTests_ServerManagement` — alle neuen Übergänge: Laden,
  Discovery-Start/-Abschluss, ungültige/gültige Adresseingabe,
  Discovery-/Wechsel-Auswahl, Prüferfolg (Persist + Home-Navigation,
  `ActiveServerAddress`, `Settings` unverändert) und -misserfolg
  (`ServerSetupError`, kein Persist-Effekt), Wechsel dedupliziert.
- `AppUpdateTests_Startup` — Start emittiert `LoadServersEffect`;
  `ServersLoadedMessage` verzweigt in Health-Check (Server vorhanden) bzw.
  Einrichtungsseite (kein Server); Nachladen außerhalb des Starts löst
  keinen Health-Check aus.
- `ServerSetupContractTests` — `ServerSetupPage` (Controls + AutomationIds,
  MVU-Dispatch-Verdrahtung, keine internen IDs als Eingabe), „Server
  wechseln" auf `SettingsPage`, Route `serversetup` ohne FlyoutItem,
  DI-Registrierungen, alle neuen Effekte im `ShellAppEffectHandler`,
  Protokoll-Konstanten + `Zeroconf`-Referenz, Speicherort Backup-Datenbank.
- Angepasst: `MvuHostTests` (fünf Start-Effekte inkl. `ServersLoaded`-
  Zwischenschritt), `AppUpdateTests_Settings`, `SettingsStorageContractTests`
  (`ServerAddressProvider_UsesKnownServersService`).

## E2E-/UI-Flow-Nachweis

Es existiert kein UI-Automatisierungs-Framework im Repository
(`inventory/tests.md`); die Pflicht-Benutzerflüsse sind über die etablierte
Ersatz-Nachweiskette (MVU-Update-Tests + Contract-Tests +
Persistenz-Roundtrips + Windows-Build/Smoke-Run) abgedeckt — gleiche
begründete Form wie in den Schritten 2 und 3:

- Erststart ohne Server → Einrichtungsdialog: `Update_ServersLoaded_
  WithoutServer_NavigatesToSetup` + `ServerSetupContractTests` +
  Windows-Smoke-Run (Setup-Seite als Window-Root).
- Discovery → Auswahl → Übernahme: `UdpServerDiscoveryTests` (echter
  Socket-Roundtrip) + `AppUpdateTests_ServerManagement`
  (`DiscoveryCompleted`/`DiscoveredServerSelected`/`CheckCompleted`).
- Erfolgreiche Einrichtung → künftig direkter Start:
  `ServerCheckCompleted_Success_PersistsAndNavigatesHome` +
  `KnownServersServiceTests`-Roundtrips + `ServersLoaded` mit Adresse →
  `CheckServerHealthEffect`.
- „Server wechseln" ohne Einstellungsverlust: `SettingsPage_Contains
  DiscreetServerSwitchEntry` + `Update_ServerCheckCompleted_Success_…`
  (`Settings` unverändert) + `KnownServersServiceTests` (schreibt nur
  `known_servers`/`backup_state`).
- Fehlerpfade: ungültige Adresse und nicht erreichbarer Server erzeugen
  keinen Persist-Effekt (`Update_AddressSubmitted_Invalid`,
  `Update_ServerCheckCompleted_Failure`).

## Einschränkungen

- mDNS-/UDP-Discovery liefern gegen die aktuelle lokale Serverinstallation
  keine verwertbaren Treffer (Server-Issues #223/#224); die App-Seite ist
  protokollkonform implementiert und getestet, die manuelle Eingabe bleibt
  der verlässliche Weg bis zur Server-Korrektur.
- `MdnsServerDiscovery` ist auf Testsystemen ohne announced Server nicht
  laufzeitabgedeckt (Zeroconf-Aufruf selbst ist nicht mockbar); Adapter ist
  bewusst dünn gehalten, die Aggregationslogik (`ServerDiscoveryService`)
  ist vollständig getestet.
