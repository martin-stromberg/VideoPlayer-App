# Code-Review

Geprüft werden alle neuen/geänderten Quelldateien gegenüber dem Basisbranch
`task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`
(Selbstprüfung ohne Unteragenten — dokumentierte Abweichung).

## Geänderte/neue Quelldateien

### `VideoPlayer.Storage`

- `ServerAddress.cs` — statische Normalisierung in die kanonische Form
  `scheme://host[:port][/pfad]/`; fehlendes Schema → `http://`, explizite
  Fremdschemata (`ftp://…`) werden abgelehnt, Host kleingeschrieben,
  Trailing-Slash erzwungen. Einzige Quelle der Adressvalidierung —
  kein duplizierter Parsing-Code in Update/Service.
- `KnownServer.cs` — schlanker Domänenrecord; `NameOrAddress` kapselt die
  Anzeige-Fallback-Regel an einer Stelle.
- `KnownServersService.cs` — Single Responsibility Serververwaltung auf
  `BackupDatabase`; Adressen werden an der Service-Grenze normalisiert
  (`Normalize` wirft bei ungültiger Eingabe); Upsert über den
  Unique-Index-Schlüssel `Address`; `MigrateLegacyServerAddressAsync` ist
  idempotent (kein Legacy-Wert → No-Op; ungültiger Wert → verworfen und
  Schlüssel entfernt) und setzt `LastConnectedServer` nur, wenn noch keiner
  existiert. Keine Kopplung an MAUI — vollständig aus dem Testprojekt
  laufzeitgetestet.

### `VideoPlayer.ApiClient` (`Discovery/`)

- `DiscoveryProtocol.cs` — alle Protokoll-Konstanten + `TryParseResponse`
  zentral; kein „Magic String" in den Probes.
- `DiscoverySource.cs`, `DiscoveredServer.cs`, `IServerDiscovery.cs` —
  kleine, klar benannte Verträge; `DiscoveredServer` ist ein record
  (Wertsemantik für Model/Tests).
- `UdpServerDiscovery.cs` — Senden + Zeitfenster-Empfangsschleife mit
  `CancellationToken`; Fremd-Datagramme und `SocketException`s einzelner
  Antworten werden übersprungen; der produktive Konstruktor bleibt schlank,
  der injizierbare interne Konstruktor macht den echten Socket-Roundtrip
  testbar, ohne Port 5001 zu belegen. Loopback-Meldung eines entfernten
  Responders → Responder-Adresse (dokumentierte, protokollkonforme Deutung;
  Verweis auf Server-Issue #224 — keine Protokolländerung).
- `MdnsServerDiscovery.cs` — dünner Zeroconf-Adapter; Fehler → leere
  Trefferliste (Discovery darf nie blockieren); Verweis auf Server-Issue
  #223.
- `ServerDiscoveryService.cs` — Composite mit `Task.WhenAll`,
  Fehlerisolation je Probe (`SafeDiscoverAsync`), Dedupe über
  `HashSet` (OrdinalIgnoreCase); injizierbare Probes für Tests.
- `VideoPlayer.ApiClient.csproj` — `Zeroconf` 3.7.16 (netstandard2.0,
  Versionsgleichheit mit der Alt-App).

### `VideoPlayer.Mvu`

- `AppPage.cs` — `ServerSetup` mit Kommentar (kein Menüeintrag;
  Window-Root beim Erststart, Shell-Route `serversetup` sonst).
- `AppModel.cs` — neue Serververwaltungs-Eigenschaften mit Defaults;
  Model bleibt ein reines record ohne ORM-/Plattformtypen
  (`DiscoveredServer` ist ApiClient-Domänentyp — Projektreferenz ergänzt).
- `AppMessage.cs` / `AppEffect.cs` — acht Messages / vier Effekte als
  `sealed record`s, konsistent zum Bestand.
- `AppUpdate.cs` — erschöpfende `switch`-Abdeckung; Fehlertexte als
  Konstanten (`InvalidAddressError`, `UnreachableServerError`) für Tests
  und einheitliche UI-Texte; Normalisierung ausschließlich über
  `ServerAddress.TryNormalize`; Erfolgszweig dedupliziert
  `KnownServers` nach Adresse (OrdinalIgnoreCase) und setzt den
  gewechselten Server an den Anfang; `Settings` bleiben beim Wechsel
  unangetastet (Test abgesichert). `ServersLoadedMessage` löst den
  Health-Check nur bei `IsStartupRunning` aus — Nachladen aus der
  geöffneten Serververwaltung navigieren nicht weg.
- `VideoPlayer.Mvu.csproj` — `ProjectReference` auf `VideoPlayer.ApiClient`.

### `VideoPlayer.Maui`

- `Services/IServerAddressProvider.cs` / `ServerAddressProvider.cs` —
  async, zweigeteilt („konfiguriert" vs. „effektiv"); IIS-Default bleibt
  Auflösungs-Fallback, zählt aber nicht als hinterlegt (Erststart-
  Erkennung); kein direkter `Preferences`-Zugriff.
- `Services/IServerHealthProbe.cs` / `ServerHealthProbe.cs` —
  `CheckAsync(Uri)`-Overload für Kandidaten; eigener `HttpClient` je Prüfung
  mit 10 s Timeout; alle Netzwerk-/URI-Fehler → `false` + Debug-Log.
- `Services/ShellAppEffectHandler.cs` — neue Effekt-Zweige mit jeweils
  lokaler Fehlerbehandlung (kein Effekt darf den MVU-Loop oder den Start
  blockieren; Rückdispatches auf dem UI-Thread); `NavigateAsync`-Sonderfall
  `ServerSetup` trennt Erststart (Window-Root) und Wechsel (geschobene
  Shell-Route) sauber.
- `AppShell.xaml.cs` — Route `serversetup` registriert; Flyout unverändert
  (Contract-Test sichert sechs Einträge ohne `serversetup`).
- `Pages/ServerSetupPage.xaml(.cs)` — vollständig modelgetrieben
  (`UpdateUi` aus `AppModel`), `ModelChanged`-Abo in `OnAppearing`/
  Abmeldung in `OnDisappearing`; Auswahl-Events setzen `SelectedItem`
  zurück (erneutes Tippen desselben Eintrags möglich); ausschließlich
  Dispatch — keine Persistenz-/Netzwerklogik in der View.
- `Pages/SettingsPage.xaml(.cs)` — dezenter „Server wechseln"-Button;
  `NavigateToMessage`-Dispatch analog der übrigen Navigation.
- `MauiProgram.cs` — `KnownServersService` (Singleton),
  `ServerDiscoveryService` (Singleton via `CreateDefault`),
  `ServerSetupPage` (Transient wie die übrigen Pages).

### Tests

- Neu: `ServerAddressTests`, `KnownServersServiceTests` (reale SQLite-DB),
  `DiscoveryProtocolTests`, `UdpServerDiscoveryTests` (Loopback-
  Socket-Roundtrip), `ServerDiscoveryServiceTests` (Fake-Probes),
  `AppUpdateTests_ServerManagement`, `ServerSetupContractTests`,
  `Stubs/SqliteDatabaseCollection`.
- Angepasst: `AppUpdateTests_Startup`, `AppUpdateTests_Settings`,
  `MvuHostTests`, `SettingsStorageContractTests`, `LocalDatabaseTests`
  (Collection-Serialisierung).

## Befunde

— (keine offenen Befunde)

### Behobene Befunde im Verlauf

- `ServerAddress.TryNormalize` akzeptierte `ftp://server/` (Schema wurde
  nach `http://`-Präfixierung als Host fehlinterpretiert) — durch Tests
  aufgefallen; Fremdschemata mit `://` werden nun vorab abgelehnt.
- `ServersLoadedMessage` mit aktiver Adresse hätte aus der geöffneten
  Serververwaltung („Server wechseln") einen Health-Check +
  Home-Navigation ausgelöst — Health-Check ist jetzt an
  `IsStartupRunning` gebunden (Test `…_AfterStartup_OnlyUpdatesModel`).
- `SQLiteAsyncConnection.ResetPool()` im `Dispose` zweier Datenbank-
  Testklassen raste parallel gegeneinander („out of memory"/„unopened
  database") — beide Klassen laufen jetzt in der Collection
  `SqliteDatabase` seriell.
- `FontSize="Title"`/`"Small"` erzeugten CS0612-Warnungen des XAML-
  SourceGenerators — auf numerische Größen umgestellt (Windows-Build
  jetzt warnungsfrei).
