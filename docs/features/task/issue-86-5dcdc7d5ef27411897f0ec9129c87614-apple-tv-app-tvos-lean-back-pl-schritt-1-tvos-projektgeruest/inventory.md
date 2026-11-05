# Bestandsaufnahme: tvOS-Projektgerüst und frühe Build-Validierung über macOS-CI

Bestandsaufnahme der Solution-, Projekt- und CI-Landschaft bezogen auf die Anforderung,
ein tvOS-App-Grundgerüst (`net10.0-tvos`) mit macOS-CI-Build-Validierung zu schaffen.

## Zusammenfassung

- Es existiert **kein** tvOS-Projekt, keine tvOS-Workload und kein tvOS-CI-Pfad.
  Die Solution `VideoPlayer.App.sln` enthält acht Projekte, davon vier
  plattformneutrale `net10.0`-Bibliotheken, die das tvOS-Projekt referenzieren soll
  (`VideoPlayer.Mvu`, `VideoPlayer.Storage`, `VideoPlayer.ApiClient`,
  `msTools.DeveloperApi`) — alle für ein `net10.0-tvos`-Konsumprojekt kompatibel.
- **Kritischer Befund:** `pr-checks.yml` führt `dotnet restore` auf Windows über die
  gesamte Solution aus. Ein tvOS-Projekt mit festem `net10.0-tvos`-TFM bricht dort —
  die tvOS-Workload ist unter Windows nicht installierbar. Das in
  `VideoPlayer.Maui.csproj` etablierte Muster (konditionale TFM-Liste über
  Include-Schalter/OS-Bedingung, „Reporter-Muster") ist auf das neue Projekt zu
  übertragen.
- **iOS-Pfad als Vorlage:** `build-ios` in `main-merge.yml`/`staging-merge.yml`
  dokumentiert exakt das für tvOS benötigte Muster: `global.json`-SDK-Band-Pin,
  Workload-Rollback per `printf`-JSON, `dotnet workload install …
  --skip-manifest-update`, Simulator-Build mit `-p:EnableCodeSigning=false`.
- **Workload-Pinning verifiziert:** `microsoft.net.sdk.tvos.manifest-10.0.100` existiert
  auf nuget.org in `26.5.10318` (deckungsgleich mit dem `main-merge`-iOS-Rollback);
  ein `27.1`-Pendant existiert nicht → `macos-latest` + `26.5.10318`.
- **Paketkompatibilität verifiziert:** `SQLitePCLRaw.lib.e_sqlite3` 3.53.3 nennt
  `net10.0-tvos` unter den kompatiblen TFMs; `sqlite-net-pcl`/`bundle_green` sind
  `netstandard`-kompatibel; `Zeroconf` 3.7.16 ist rein verwaltet und kompilierbar
  (Laufzeitrisiko Multicast-Entitlement auf echten Geräten dokumentiert, Ausweich:
  UDP-only-/Broadcast-Discovery).
- **Test-Ausgangszustand:** `VideoPlayer.Maui.Tests` 1440/1440 grün (eigener Lauf);
  `VideoPlayer.E2E.Tests` 71 bestanden / 3 nachgewiesene preexisting Fehlschläge
  (Referenzlauf der Projekt-Bestandsaufnahme, identischer Code-Stand) — siehe
  [tests.md](inventory/tests.md).

## Details

- [CI-Landschaft / iOS-Pfad als Vorlage](inventory/ci.md)
- [Projekte, Solution und Paketkompatibilität](inventory/projects.md)
- [Tests / Ausgangszustand](inventory/tests.md)
