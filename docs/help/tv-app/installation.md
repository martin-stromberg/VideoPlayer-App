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
| `SQLitePCLRaw.lib.e_sqlite3` | 3.53.3 | kompatibel — natives tvOS-Asset vorhanden (`net6.0-tvos` bis `net10.0-tvos` in den kompatiblen TFMs gelistet) |
| `SQLitePCLRaw.bundle_green` | 2.1.11 | kompatibel (`netstandard2.0`-Bundle, wählt `e_sqlite3`) |
| `sqlite-net-pcl` | 1.11.285 | kompatibel (`netstandard2.0`) |
| `SQLitePCLRaw.lib.e_sqlite3.android` | 2.1.13 | unproblematisch — Android-RID-Zusatz, unter tvOS ohne Wirkung |
| `Zeroconf` | 3.7.16 | kompilierbar (`netstandard`, rein verwaltete UDP/mDNS-Implementierung) — **Laufzeitrisiko:** mDNS-Multicast verlangt auf tvOS-Geräten das Entitlement `com.apple.developer.networking.multicast` sowie `NSLocalNetworkUsageDescription` + `NSBonjourServices` (`_http._tcp`); die `Info.plist` ist vorbereitet. **Ausweichvorschlag bei Inkompatibilität:** UDP-only-/Broadcast-Discovery bzw. direkte Servereingabe — `UdpServerDiscovery` in `VideoPlayer.ApiClient` ist der vorhandene Referenzpfad |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.10 | kompatibel (plattformneutral) |

Kein Paket ist inkompatibel; kein Ersatz erforderlich. Die einzige
Einschränkung ist das dokumentierte Multicast-Entitlement-Risiko von
`Zeroconf` auf echten Geräten (Simulator meist unbetroffen).

## Überprüfung

- Windows (degradierter Modus): `dotnet build VideoPlayer.App.sln` —
  das Projekt baut als leere `net10.0`-Bibliothek.
- macOS: `dotnet build VideoPlayer.TvOS/VideoPlayer.TvOS.csproj
  -f net10.0-tvos -r tvossimulator-arm64 -p:EnableCodeSigning=false`
  erzeugt `VideoPlayer.TvOS.app` für den Simulator.
