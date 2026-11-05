# Abnahmeprüfung – Entwicklungsschritt 1

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Hinweise

Prüfnachweise (gegen `git diff task/issue-86-...-apple-tv-app-tvos-lean-back-pl...HEAD` sowie Quelllektüre):

- **Projekt & TFM:** `VideoPlayer.TvOS/VideoPlayer.TvOS.csproj` zielt auf `net10.0-tvos` (Zeile 13), konditioniert über den Schalter `IncludeTvOsTarget` (Default `$([MSBuild]::IsOSPlatform('osx'))`, Zeile 12). `OutputType=Exe`, `ApplicationId=de.martinstromberg.videos.tv`, `SupportedOSPlatformVersion=15.0`, RIDs `tvossimulator-x64;tvossimulator-arm64;tvos-arm64`.
- **Referenzen:** ausschließlich die vier plattformneutralen `net10.0`-Bibliotheken `msTools.DeveloperApi`, `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`, `VideoPlayer.Storage` (csproj Zeilen 47–50). Alle vier Ziel-csproj sind `net10.0` ohne MAUI-Bezug verifiziert; das tvOS-Projekt selbst enthält keine `PackageReference` und kein `UseMaui`.
- **Programmhülle:** `Program.cs` (`UIApplication.Main(args, null, typeof(AppDelegate))`), `AppDelegate.cs` (erzeugt `UIWindow` mit `MainViewController` als `RootViewController`, `MakeKeyAndVisible`), `MainViewController.cs` (schwarzer Hintergrund, zentrierter Titel „Heimkino TV", keine fachliche Funktion), `Info.plist` (`UIDeviceFamily=3`, `NSLocalNetworkUsageDescription` + `NSBonjourServices` `_http._tcp` passend zu `DiscoveryProtocol.MdnsServiceType`, ATS-Lockerung für LAN).
- **Lösung:** `VideoPlayer.App.sln` enthält das Projekt mit eindeutiger GUID `{AFA373E3-…}` inkl. aller Debug/Release-Konfigurationen (Any CPU/x64/x86).
- **CI-Job:** `build-tvos` in `.github/workflows/pr-checks.yml` (Zeilen 50–86) auf `macos-latest`: `setup-dotnet 10.0.x` → `global.json`-Band-Pin `10.0.100`/`latestPatch` (gleiches Muster wie `build-ios` in `main-merge.yml`, Zeilen 81–84) → Workload-Rollback-Datei `{"microsoft.net.sdk.tvos": "26.5.10318"}` + `dotnet workload update --from-rollback-file` + `dotnet workload install tvos --skip-manifest-update` — deckungsgleiche Version und analoges Vorgehen zum iOS-Rollback (`microsoft.net.sdk.ios: 26.5.10318`, main-merge.yml Zeilen 92–96) → Simulator-Build `dotnet build … -f net10.0-tvos -r tvossimulator-arm64 -p:EnableCodeSigning=false` (keine Signierung; auf dem macOS-Runner greift der OS-Default des Schalters, das TFM ist daher aktiv). YAML-Syntax und Job-Plausibilität sind korrekt.
- **Paketkompatibilität:** dokumentiert in `docs/help/tv-app/installation.md` (Abschnitt „Paketkompatibilität unter net10.0-tvos", Zeilen 72–85): `SQLitePCLRaw.lib.e_sqlite3` 3.53.3, `bundle_green` 2.1.11, `sqlite-net-pcl` 1.11.285 kompatibel; `Zeroconf` 3.7.16 kompilierbar mit dokumentiertem Multicast-Entitlement-Laufzeitrisiko und benanntem Ausweichvorschlag (UDP-only-Discovery — `UdpServerDiscovery` existiert in `VideoPlayer.ApiClient/Discovery/UdpServerDiscovery.cs`). Keine Inkompatibilität festgestellt.
- **Unveränderte Lauffähigkeit:** auf diesem Windows-Rechner verifiziert — `dotnet restore VideoPlayer.App.sln` erfolgreich (nur vorbestehende NU1903-Warnung zu SignalR, unabhängig vom Diff); `dotnet build VideoPlayer.TvOS.csproj -c Release` baut degradiert als leere `net10.0`-Bibliothek (0 Fehler, `EnableDefaultCompileItems=false`), der `IncludeTvOsTarget`-Schalter bricht den Windows-Build also nicht; die 6 neuen `TvOsProjectContractTests` bestehen (6/6 grün); `dotnet format whitespace --verify-no-changes` auf der Solution ohne Befund. Testbestand laut `inventory/test-results/maui-tests-dotnet-test.log`: 1440 + 6 neue = 1446 Tests.
- **Absicherung:** Contract-Tests `VideoPlayer.Maui.Tests/TvOsProjectContractTests.cs` prüfen Solution-Mitgliedschaft, exakte Referenzmenge, TFM-Schalter, Einstieg/Fenster und die CI-Job-Konfiguration fortlaufend.

Einschränkungen (keine Abweichungen):

- Der reale macOS-CI-Lauf von `build-tvos` kann auf dem Windows-Entwicklungsrechner nicht stattfinden und steht erst nach Push an; die Konfiguration selbst ist syntaktisch und inhaltlich plausibel und am bewährten iOS-Pfad ausgerichtet.
- Die Bundle-ID `de.martinstromberg.videos.tv` ist dokumentierte Arbeitsannahme; die endgültige ID wird im Apple-Developer-Account eingerichtet (manuelles Vorlauf-Thema des Deployment-Schritts).
- Kleinigkeit ohne Anforderungsbezug: `docs/features/…-tasks.md` nennt `inventory/projects.md` als Ablage des Kompatibilitäts-Ergebnisses — die Datei existiert nicht; die verbindliche Dokumentation liegt fachlich korrekt in `docs/help/tv-app/installation.md`.
