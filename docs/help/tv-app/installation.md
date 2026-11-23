← [Zurück zur Übersicht](index.md)

# Apple-TV-App (tvOS) — Installation, CI-Build und Kompatibilität

## Voraussetzungen

- macOS mit Xcode 26.x (Release-Xcode; das gepinnte tvOS-SDK-Pack `26.5.x`
  verlangt eine Xcode-26-Generation)
- .NET SDK Band `10.0.1xx` (`global.json`-Pin im CI-Job:
  `{"sdk":{"version":"10.0.100","rollForward":"latestPatch","allowPrerelease":false}}`)
- tvOS-Workload, gepinnt auf Manifest `26.5.10318`:

  ```bash
  printf '{"microsoft.net.sdk.tvos": "26.5.10318"}' > workload-rollback.json
  dotnet workload update --from-rollback-file workload-rollback.json
  dotnet workload install tvos --skip-manifest-update
  ```

## Projekt `VideoPlayer.TvOS`

- TFM: `net10.0-tvos`, `OutputType=Exe`, natives UIKit-Gerüst
  (`Program` → `UIApplication.Main` → `AppDelegate` → `MainViewController`)
- Referenziert ausschließlich die plattformneutralen `net10.0`-Bibliotheken
  `VideoPlayer.Mvu`, `VideoPlayer.Storage`, `VideoPlayer.ApiClient` und
  `msTools.DeveloperApi` — keine MAUI-Abhängigkeit (MAUI unterstützt tvOS
  nicht).
- `SupportedOSPlatformVersion` `15.0`; `RuntimeIdentifiers`
  `tvossimulator-x64`, `tvossimulator-arm64`, `tvos-arm64`.
- Bundle-ID `de.martinstromberg.videos.tv` ist eine Arbeitsannahme — die
  endgültige Bundle-ID wird im Apple-Developer-Account eingerichtet
  (manueller Vorlauf für den späteren Deployment-Pfad).

### `IncludeTvOsTarget`-Schalter (Windows-Kompatibilität)

Die tvOS-Workload ist unter Windows nicht installierbar. Damit
`dotnet restore`/`dotnet build`/`dotnet test` auf die gesamte
`VideoPlayer.App.sln` unter Windows weiter funktioniert (lokale Entwicklung
und `verify`-Job der PR-Checks), ist das Ziel-Framework konditionalisiert —
gleiches Schalter-Muster wie `IncludeIosTarget` u. a. in
`VideoPlayer.Maui.csproj`:

- `IncludeTvOsTarget=true` (Default: nur unter macOS) → `net10.0-tvos`
- sonst → degradierte leere `net10.0`-Bibliothek
  (`EnableDefaultCompileItems=false`, `OutputType=Library`)

Explizit aktivierbar via `-p:IncludeTvOsTarget=true`.

## CI-Job `build-tvos` (`.github/workflows/pr-checks.yml`)

Baut das tvOS-Projekt bei jedem PR nach `main`/`staging` auf `macos-latest`
für den Simulator — ohne Codesignierung (`-p:EnableCodeSigning=false`, der
Runner hat keine Zertifikate; Simulator-Builds werden ad-hoc signiert):

1. `actions/setup-dotnet` (`10.0.x`)
2. `global.json`-Band-Pin `10.0.100`/`latestPatch` — ohne den Pin würde das
   Rollback ins `10.0.400`-Manifest auflösen, das nicht auf nuget.org liegt
   (Muster `build-ios` in `main-merge.yml`).
3. Workload-Rollback `{"microsoft.net.sdk.tvos": "26.5.10318"}` +
   `dotnet workload install tvos --skip-manifest-update`.
4. `dotnet build VideoPlayer.TvOS -c Release -f net10.0-tvos
   -r tvossimulator-arm64 -p:EnableCodeSigning=false`

### Manifest-Pinning

Die Pinning-Version `26.5.10318` ist deckungsgleich mit dem iOS-Rollback des
`main-merge.yml`-Pfads und gegen nuget.org verifiziert
(`microsoft.net.sdk.tvos.manifest-10.0.100`). Wechselt der Runner auf
Xcode 27, ist die Version auf ein `27.0.x`-Manifest anzuheben (Stand der
Prüfung: `27.0.10722`, `27.0.10742` verfügbar; ein `27.1`-Band existiert für
tvOS nicht).

## Paketkompatibilität unter `net10.0-tvos` (Prüfergebnis)

| Paket | Version | Ergebnis |
|-------|---------|----------|
| `SQLitePCLRaw.lib.e_sqlite3` | 3.53.3 | unter tvOS ohne natives Asset — das Paket liefert `runtimes/`-Ordner nur für iOS/Android/macOS/Windows/Linux, die tvOS-RID-Kette (`tvossimulator-arm64 → tvos → unix`) fällt **nicht** auf iOS zurück; Beitrag unter tvOS daher nur die verwaltete `netstandard2.0`-Assembly |
| `SQLitePCLRaw.bundle_green` | 2.1.11 | kompatibel — zieht für `net6.0-tvos` transitiv `SQLitePCLRaw.lib.e_sqlite3.tvos` + `SQLitePCLRaw.provider.internal` (siehe Sonderregelung unten) |
| `SQLitePCLRaw.lib.e_sqlite3.tvos` | 2.1.11 | **Linkfehler unter `tvossimulator-arm64`** — siehe Sonderregelung unten |
| `sqlite-net-pcl` | 1.11.285 | kompatibel (`netstandard2.0`); zieht `SQLitePCLRaw.provider.sqlite3` ≥ 3.0.3 transitiv |
| `SQLitePCLRaw.provider.sqlite3` | 3.0.3 | kompatibel (rein verwaltet, `DllImport("sqlite3")` → System-`libsqlite3.dylib`) |
| `SQLitePCLRaw.lib.e_sqlite3.android` | 2.1.13 | unproblematisch — Android-RID-Zusatz, unter tvOS ohne Wirkung |
| `Zeroconf` | 3.7.16 | kompilierbar (`netstandard`, rein verwaltete UDP/mDNS-Implementierung) — **Laufzeitrisiko:** mDNS-Multicast verlangt auf tvOS-Geräten das Entitlement `com.apple.developer.networking.multicast` sowie `NSLocalNetworkUsageDescription` + `NSBonjourServices` (`_http._tcp`); die `Info.plist` ist vorbereitet. **Ausweichvorschlag bei Inkompatibilität:** UDP-only-/Broadcast-Discovery bzw. direkte Servereingabe — `UdpServerDiscovery` in `VideoPlayer.ApiClient` ist der vorhandene Referenzpfad |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.10 | kompatibel (plattformneutral) |

### Sonderregelung SQLite unter tvOS (retaggte e_sqlite3-Archive)

`SQLitePCLRaw.lib.e_sqlite3.tvos` bettet `e_sqlite3.a` als Ressource in die
verwaltete Assembly ein. Die Analyse des fat archive (alle Versionen bis
einschließlich 2.1.14, Stand 2026-11) ergab nur zwei Slices:

| Slice | `LC_BUILD_VERSION`-Plattform |
|-------|------------------------------|
| `x86_64` | `tvossimulator` (8) |
| `arm64` | `tvos` (3) — **Gerät, kein Simulator** |

Eine `tvossimulator-arm64`-Slice existiert in keiner veröffentlichten
Version. Der `build-tvos`-Job (RID `tvossimulator-arm64`) schlug deshalb
beim nativen Linken fehl:

```
ld: building for 'tvOS-simulator', but linking in object file
  (.../linker-cache/e_sqlite3.a[arm64][2](sqlite3.o)) built for 'tvOS'
```

**Lösung (Plattform-Tag der iOS-Archive umschreiben):**

Die `e_sqlite3.a` aus `SQLitePCLRaw.lib.e_sqlite3` 3.53.3
(`runtimes/ios-arm64` bzw. `runtimes/iossimulator-*`) unterscheidet sich
von einem tvOS-Build nur im Plattform-Tag der Mach-O-Objekte —
Architektur, ABI und Funktionsumfang sind identisch (Simulator- und
Geräte-Plattformen teilen das SDK-Featureset; SQLite ist reines
POSIX-C). Das Skript `scripts/Retag-Esqlite3-For-Tvos.py` schreibt das
Plattformfeld um (`LC_BUILD_VERSION` 7→8 bzw. ältere
`LC_VERSION_MIN_IPHONEOS`-Kommandos 0x25→0x2f/`LC_VERSION_MIN_TVOS`) und
erzeugt die eingecheckten Archive unter `VideoPlayer.TvOS/NativeLibs/`:

| Datei | Quelle | Slices |
|-------|--------|--------|
| `e_sqlite3.tvossimulator.a` | `runtimes/iossimulator-arm64` (fat) | `x86_64` + `arm64`, je `tvossimulator` |
| `e_sqlite3.tvos-arm64.a` | `runtimes/ios-arm64` (thin) | `arm64`, `tvos` |

- `VideoPlayer.TvOS.csproj` unterdrückt das transitive
  `SQLitePCLRaw.lib.e_sqlite3.tvos` per Direktreferenz mit
  `ExcludeAssets="all"` (nur unter `IncludeTvOsTarget=true`) und linkt
  stattdessen die retaggten Archive als `NativeReference`
  (`Kind=Static`, `ForceLoad`, `SmartLink=false`), RID-konditioniert
  (`tvos-arm64` vs. Simulator-RIDs).
- Der verwaltete Pfad bleibt unverändert der upstream-vorgesehene:
  `Batteries_V2.Init()` → `SQLite3Provider_internal` →
  `DllImport("__Internal")` löst die `sqlite3_*`-Symbole aus der
  statisch gelinkten `e_sqlite3.a` — auf Gerät **und** Simulator. Eine
  Änderung an `SqliteBootstrap` oder den übrigen Plattformen ist nicht
  erforderlich.

#### Verworfene Alternative: System-libsqlite3

Ein Provider-Wechsel auf `SQLitePCLRaw.provider.sqlite3`
(System-`libsqlite3.dylib`) funktioniert auf tvOS **nicht** ohne
Weiteres: die tvOS-Bibliothek exportiert — anders als iOS/macOS —
`sqlite3_load_extension`/`sqlite3_enable_load_extension` nicht
(`SQLITE_OMIT_LOAD_EXTENSION`), die ILC-Wrapper der
`SQLitePCLRaw.provider.*`-Assemblies referenzieren die Symbole aber
statisch (`Undefined symbols for architecture arm64`). `-U`-Linkerflags
als Ausweg wurden geprüft und verworfen — der retaggte e_sqlite3-Pfad
ist funktionsgleich mit den übrigen Plattformen und ohne Symbollücken.

## Signierter Release-Pfad

Die Paketierung für App Store Connect (signierter `tvos-arm64`-Build,
`.ipa` + dSYM-Archiv, TestFlight-Upload per iTMSTransporter) liegt in
der Composite-Action `.github/actions/package-tvos` und ist in die
Release-Workflows `staging-merge.yml`/`main-merge.yml` eingebunden —
hinter der Gate-Variable `TVOS_SIGNING_ENABLED`, damit die Pipeline
ohne eingerichtete Signierung grün bleibt. Für manuelle Builds dient
`scripts/tvOS-Deployment.ps1`. Details:
[deployment.md](deployment.md).

## Überprüfung

- Windows (degradierter Modus): `dotnet build VideoPlayer.App.sln` —
  das Projekt baut als leere `net10.0`-Bibliothek.
- macOS: `dotnet build VideoPlayer.TvOS/VideoPlayer.TvOS.csproj
  -f net10.0-tvos -r tvossimulator-arm64 -p:EnableCodeSigning=false`
  erzeugt `VideoPlayer.TvOS.app` für den Simulator.
