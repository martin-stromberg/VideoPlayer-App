# Umsetzungsplan: tvOS-Projektgerüst und frühe Build-Validierung über macOS-CI

## Übersicht

Es wird ein neues, eigenständiges tvOS-App-Projekt `VideoPlayer.TvOS` (`net10.0-tvos`)
als leere Programmhülle (Fenster + einfache Startansicht) angelegt, in die
`VideoPlayer.App.sln` aufgenommen und über einen neuen macOS-CI-Job `build-tvos` in
`pr-checks.yml` als Simulator-Build ohne Codesignierung fortlaufend kompilierbar
gehalten. Die tvOS-Workload wird analog zum iOS-Pfad per Rollback-Datei gepinnt; die
Paketkompatibilität unter `net10.0-tvos` ist geprüft und wird dauerhaft dokumentiert.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | tvOS-App-Projekt `VideoPlayer.TvOS` als Build-/Solution-Einheit inkl. CI-Build-Job, Workload-Pinning und Kompatibilitätsdokumentation |
| Auslösende Ereignisse | PR auf `main`/`staging` (CI baut tvOS-Simulator-Build); Solution-Öffnung/`dotnet restore`/`dotnet test` unter Windows (muss weiterhin funktionieren); App-Start im tvOS-Simulator (zeigt leere Startansicht) |
| Erwartete Wirkung | CI-Job `build-tvos` ist auf dem macOS-Runner grün; Windows-Solution-Workflows bleiben unverändert lauffähig; leere Startansicht im Simulator sichtbar |
| Umgang mit fehlenden Objekten | tvOS-Workload auf dem CI-Runner → wird im Job per Rollback-Pinning installiert (erzeugen). Signatur-Artefakte → nicht vorhanden und nicht nötig; Simulator-Build läuft mit `EnableCodeSigning=false` ad-hoc (verwerfen/ausklammern). tvOS-Workload auf Windows-Entwicklungsrechner → nicht installierbar; Projekt degradiert per `IncludeTvOsTarget`-Schalter zu leerer `net10.0`-Bibliothek (ausklammern, übersteuerbar) |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine fachliche Funktion / keine Verdrahtung der referenzierten Bibliotheken | Explizite Anforderung: „minimale, leere Programmhülle … ohne fachliche Funktion" | bestätigt (aus Projektplan-Schrittbeschreibung, 2026-11-05) |
| 2 | Keine Signierung, Paketierung oder App-Store-Upload | Signatur-Artefakte liegen im Apple-Developer-Account noch nicht vor (manueller Vorlauf); produktiver Pfad ist Projektschritt 11 | bestätigt (Projektplan, 2026-11-05) |
| 3 | Keine tvOS-Testsuite / kein tvOS-E2E-Framework | Unter Windows nicht ausführbar; Kompilierbarkeit über den macOS-CI-Job ist der Funktionsnachweis dieses Schritts | bestätigt (aus Anforderung ableitbar, 2026-11-05) |
| 4 | Kein lokales tvOS-Deployment-Skript | Gehört zum Deployment-Pfad (Projektschritt 11) | bestätigt (Projektplan, 2026-11-05) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|------------------|------------|
| Projektname / Root-Namespace | `VideoPlayer.TvOS` | Deckt sich mit dem Ziel-Moniker `net10.0-tvos`; folgt der `VideoPlayer.*`-Namenskonvention der Solution |
| TFM-Konditionalisierung | `IncludeTvOsTarget`-Schalter (Default `true` nur unter macOS, übersteuerbar via `-p:IncludeTvOsTarget=true`); bei `false` degradiert das Projekt zu leerer `net10.0`-Bibliothek (`EnableDefaultCompileItems=false`, `OutputType=Library`) | `dotnet restore`/`build`/`test` auf die gesamte Solution läuft unter Windows (lokal + `pr-checks.yml` `verify`-Job); festes `net10.0-tvos`-TFM würde mit `NETSDK1147` brechen. Direkte Übertragung des „Reporter-Musters" aus `VideoPlayer.Maui.csproj` |
| UI-Technologie | tvOS-natives UIKit (`UIApplication`, `UIApplicationDelegate`, `UIWindow`, `UIViewController`, `UILabel`) | MAUI unterstützt tvOS nicht (Projektplan-Designentscheidung); für eine leere Hülle ist programmatisches UIKit minimal — kein Storyboard, kein SwiftUI nötig |
| CI-Runner / Manifestversion | `macos-latest` + Rollback `{"microsoft.net.sdk.tvos": "26.5.10318"}` | Deckungsgleich mit dem `main-merge.yml`-iOS-Rollback (`26.5.10318` = Xcode-26.x-Pack, `macos-latest` trägt Release-Xcode 26.x); ein tvOS-Band-`27.1`-Pendant zum `xcode-27`-Pfad existiert auf nuget.org nicht |
| Workflow-Platzierung | Neuer Job `build-tvos` in `pr-checks.yml` | „Kompilierbarkeit fortlaufend sichern" = bei jedem PR prüfen; Release-Workflows bleiben unverändert (Packaging ist Projektschritt 11) |
| Bundle-ID | `de.martinstromberg.videos.tv` (Arbeitsannahme, dokumentiert) | Finale Bundle-ID ist manueller Vorlauf im Apple-Developer-Account (Projektplan); für den Simulator-Build genügt ein konsistenter Platzhalter nach dem bestehenden `de.martinstromberg.videos`-Schema |

## Programmabläufe

### App-Start im tvOS-Simulator

1. `Program.Main` ruft `UIApplication.Main(args, null, typeof(AppDelegate))`.
2. tvOS instanziiert `AppDelegate` und ruft `FinishedLaunching`.
3. `AppDelegate` erzeugt `UIWindow` über `UIScreen.MainScreen.Bounds`, setzt
   `RootViewController` auf einen einfachen `UIViewController` mit dunklem Hintergrund
   und zentriertem `UILabel` („Heimkino TV") und ruft `MakeKeyAndVisible`.
4. Sichtbare Wirkung: leere Startansicht (Titel-Schriftzug auf dunklem Grund).

Beteiligte Klassen: `Program`, `AppDelegate`, `MainViewController` (eigene
`UIViewController`-Ableitung für die Startansicht).

### CI-Build-Validierung (`build-tvos` in `pr-checks.yml`)

1. `actions/checkout`, `actions/setup-dotnet` (`10.0.x`).
2. `global.json`-Band-Pin (`10.0.100`, `rollForward: latestPatch`) — verhindert
   Auflösung ins `10.0.400`-Manifest (Muster aus `main-merge.yml`).
3. tvOS-Workload-Rollback: `printf '{"microsoft.net.sdk.tvos": "26.5.10318"}' >
   "$RUNNER_TEMP/workload-rollback.json"` → `dotnet workload update
   --from-rollback-file` → `dotnet workload install tvos --skip-manifest-update`.
4. `dotnet build VideoPlayer.TvOS/VideoPlayer.TvOS.csproj -c Release -f net10.0-tvos
   -r tvossimulator-arm64 -p:EnableCodeSigning=false`.
5. Sichtbare Wirkung: grüner Job; das Artefakt (`.app`) wird in diesem Schritt noch
   nicht hochgeladen (kein Release-Kontext).

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `VideoPlayer.TvOS.Program` | Klasse (Einstiegspunkt, `static void Main`) | `UIApplication.Main`-Start, Muster `VideoPlayer.Maui/Platforms/iOS/Program.cs` |
| `VideoPlayer.TvOS.AppDelegate` | `UIApplicationDelegate`-Ableitung (`[Register("AppDelegate")]`) | Fenster anlegen und Startansicht aktivieren |
| `VideoPlayer.TvOS.MainViewController` | `UIViewController`-Ableitung | Leere Startansicht: dunkler Hintergrund + zentrierter Titel |

## Neue Projekt-/Konfigurationsdateien

| Datei | Zweck |
|-------|-------|
| `VideoPlayer.TvOS/VideoPlayer.TvOS.csproj` | tvOS-App-Projekt, konditionales TFM, nur `ProjectReference` auf die vier `net10.0`-Bibliotheken |
| `VideoPlayer.TvOS/Info.plist` | App-Metadaten; `UIDeviceFamily=3` (tv); `NSLocalNetworkUsageDescription` + `NSBonjourServices` (`_http._tcp`) vorsorglich für die spätere mDNS-Server-Discovery (`MdnsServerDiscovery` nutzt `_http._tcp.local.`) |
| `VideoPlayer.TvOS/Program.cs`, `AppDelegate.cs`, `MainViewController.cs` | Minimales UIKit-Gerüst |

## Änderungen an bestehenden Dateien

### `VideoPlayer.App.sln`

- Neuer `Project`-Eintrag `VideoPlayer.TvOS` mit neuer GUID; `Debug|Any CPU`- und
  `Release|Any CPU`-Konfigurationseinträge; `NestedProjects` entfällt (flache Liste,
  keine Solution-Ordner vorhanden).

### `.github/workflows/pr-checks.yml`

- Neuer Job `build-tvos` (siehe Programmablauf oben). Bestehender Job `verify` bleibt
  unverändert.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `IncludeTvOsTarget` (MSBuild-Property, `VideoPlayer.TvOS.csproj`) | bool | `true` unter macOS, sonst `false` | Schaltet das `net10.0-tvos`-TFM zu bzw. degradiert zu leerer `net10.0`-Bibliothek (Windows-Restore/-Build-Kompatibilität) |
| tvOS-Workload-Rollback (Workflow-Variable via `printf`-JSON) | string | `26.5.10318` | Gepinnte `microsoft.net.sdk.tvos`-Manifestversion (iOS-Pfad-Analogon) |

## Seiteneffekte und Risiken

- **Solution-Restore unter Windows:** ohne konditionales TFM brächen lokale Builds und
  der `verify`-Job — durch `IncludeTvOsTarget`-Schalter adressiert; muss in der
  Implementierung durch echten `dotnet restore`/`dotnet build`/`dotnet test` unter
  Windows verifiziert werden.
- **Manifest-/Xcode-Drift auf dem Runner:** läuft `macos-latest` auf Xcode 27 hoch,
  kann das `26.5`-Pack unpassend werden; die Pinning-Zahl ist dann analog anzuheben
  (verfügbare Versionen: `microsoft.net.sdk.tvos.manifest-10.0.100`, Stand: `26.5.10318`,
  `27.0.10722`, `27.0.10742`). Kommentar im Workflow nennt die Anpassungsstelle.
- **`dotnet format whitespace --verify-no-changes`:** neue Dateien müssen dem
  Whitespace-/Formatierungsstandard entsprechen (4er-Einrückung, Block-Namespace mit
  geschweiften Klammern — Stil aus `VideoPlayer.Maui/Platforms/iOS/`).
- **`WarningsAsErrors` `CS1591` + `GenerateDocumentationFile`:** Projekt hält die
  Konvention; alle public/protected Member der neuen Klassen erhalten XML-Dok-Kommentare.
- **`NSBonjourServices`/`NSLocalNetworkUsageDescription`:** vorsorglich für die spätere
  Discovery enthalten; für den Simulator-Build harmlos.

## Umsetzungsreihenfolge

1. **Paketkompatibilität dokumentieren (Dauer-Artefakt)**
   - Voraussetzungen: keine (Ergebnisse liegen in `inventory/projects.md` vor).
   - Beschreibung: Dauerhaftes Kompatibilitätsdokument anlegen — Inhalt wird in
     Schritt 9 in `docs/help/` eingeordnet; die Prüfergebnisse selbst stehen bereits
     in `inventory/projects.md` (SQLite-Stack kompatibel; Zeroconf kompilierbar mit
     dokumentiertem Multicast-Entitlement-Risiko + UDP-only-Ausweich).

2. **Projekt `VideoPlayer.TvOS` anlegen (csproj)**
   - Voraussetzungen: keine.
   - Beschreibung: `VideoPlayer.TvOS/VideoPlayer.TvOS.csproj` mit
     `IncludeTvOsTarget`-Schalter, `net10.0-tvos`-TFM unter macOS, App-Metadaten
     (`ApplicationId`, `ApplicationDisplayVersion` `0.1`, `ApplicationVersion` `1`,
     `SupportedOSPlatformVersion` `15.0`, `RuntimeIdentifiers`), Konventionen
     (`ImplicitUsings`, `Nullable`, `GenerateDocumentationFile`,
     `WarningsAsErrors CS1591`, LICENSE-Verweis) und `ProjectReference` auf
     `VideoPlayer.Mvu`, `VideoPlayer.Storage`, `VideoPlayer.ApiClient`,
     `msTools.DeveloperApi`.

3. **UIKit-Gerüst anlegen (`Program.cs`, `AppDelegate.cs`, `MainViewController.cs`,
   `Info.plist`)**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: Einstiegspunkt nach dem Muster
     `VideoPlayer.Maui/Platforms/iOS/Program.cs`; `AppDelegate` mit `Window`-Override;
     `MainViewController` mit dunklem Hintergrund und zentriertem `UILabel`
     „Heimkino TV"; XML-Dok-Kommentare wegen `CS1591`.

4. **Solution-Eintrag**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: `dotnet sln VideoPlayer.App.sln add
     VideoPlayer.TvOS/VideoPlayer.TvOS.csproj` (edits nur die `.sln`, keine
     Workload nötig); GUID- und Konfigurationseinträge prüfen.

5. **Windows-Validierung der Solution**
   - Voraussetzungen: Schritte 2–4.
   - Beschreibung: `dotnet restore` (gesamte Solution), `dotnet build VideoPlayer.App.sln`
     bzw. `dotnet test VideoPlayer.Maui.Tests` — muss ohne tvOS-Workload
     fehlerfrei durchlaufen.

6. **CI-Job `build-tvos` in `pr-checks.yml`**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: Job nach dem `build-ios`-Muster (SDK-Band-Pin, Rollback
     `26.5.10318`, `workload install tvos`, Simulator-Build
     `tvossimulator-arm64`, `EnableCodeSigning=false`), Kommentar zur
     Versionsanpassung.

7. **YAML-Syntax-Validierung des Workflows**
   - Voraussetzungen: Schritt 6.
   - Beschreibung: `pr-checks.yml` parsen (Python/PowerShell-YAML oder
     `dotnet`-unabhängiger Linter); Struktur des neuen Jobs gegen `build-ios` spiegeln.

## Tests

### Neue Tests

Keine neuen Unit-/Integrationstests — die leere Programmhülle enthält keine
testbare plattformneutrale Logik; `net10.0-tvos` ist auf dem Entwicklungsrechner
nicht kompilier-/ausführbar. Funktionsnachweis ist der macOS-CI-Build-Job
(Kompilierbarkeit) plus Windows-Validierung der Solution (Schritt 5 oben).

Zusätzlich sinnvoll und unter Windows möglich (Contract-Test-Muster, analog
`VideoPlayerMauiContractTests`/`IosDeploymentResponseFileTests`):

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `TvOsProject_IsPartOfSolution` | `TvOsProjectContractTests` (neu, `VideoPlayer.Maui.Tests`) | `VideoPlayer.App.sln` enthält `VideoPlayer.TvOS\VideoPlayer.TvOS.csproj` |
| `TvOsProject_ReferencesOnlyPlatformNeutralLibraries` | `TvOsProjectContractTests` | csproj enthält genau die vier erlaubten `ProjectReference`-Pfade und kein `PackageReference`/`UseMaui` |
| `TvOsProject_TargetsTvOsConditionally` | `TvOsProjectContractTests` | csproj enthält `net10.0-tvos` nur hinter dem `IncludeTvOsTarget`-Schalter und einen Nicht-macOS-Fallback |
| `PrChecksWorkflow_ContainsTvOsSimulatorJob` | `TvOsProjectContractTests` | `pr-checks.yml` enthält Job `build-tvos` mit `macos`-Runner, `microsoft.net.sdk.tvos`-Rollback und `tvossimulator-arm64` + `EnableCodeSigning=false` |
| `RepositoryRoot` (bereits vorhanden) | Stubs | Liefert Repo-Pfad für die Contract-Tests |

### Betroffene bestehende Tests

Keine — es werden keine bestehenden Quelldateien geändert (außer `.sln`-Eintrag und
`pr-checks.yml`); bestehende Contract-Tests bleiben unberührt.

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests. Begründung: Die Anforderung liefert bewusst eine leere
Programmhülle ohne Benutzerfluss; auf Windows ist tvOS nicht lauffähig und es
existiert keine tvOS-UI-Testinfrastruktur. Der über die Anforderung geforderte
Funktionsnachweis (Simulator-Build auf macOS) liegt in der CI — nicht in einem
ausführbaren E2E-Test. Der reale Abnahmepunkt „App startet und zeigt die
Startansicht" ist als CI-/Simulator-Nachweis dokumentiert (siehe
Akzeptanzszenarien, Nachweis „macOS-CI-Job"), ein Windows-seitiger E2E-Ersatz ist
technisch nicht möglich.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: CI-Kompilierbarkeit | Pull Request auf `main`/`staging` | tvOS-Projekt in Solution; Runner ohne tvOS-Workload | `build-tvos`: SDK-Pin → Rollback-Workload-Install → `dotnet build -r tvossimulator-arm64` | Grüner Job; `VideoPlayer.TvOS.app` für Simulator kompiliert | macOS-CI-Job (läuft bei PR auf Remote-Runner; unter Windows nicht reproduzierbar) |
| 2 | Windows-Entwickler-Solution | `dotnet restore`/`build`/`test` auf `VideoPlayer.App.sln` unter Windows ohne tvOS-Workload | tvOS-Projekt ist Solution-Mitglied | Restore/Build/Test der gesamten Solution | Unverändert erfolgreich — tvOS-Projekt degradiert zu leerer `net10.0`-Bibliothek | `test-results.md` + Contract-Tests |
| 3 | tvOS-App-Start | Start im Apple-TV-Simulator | App-Paket aus CI-/macOS-Build | Simulator bootet die App | Dunkle Startansicht mit Titel „Heimkino TV" | Nachweis im CI-Build-Artefakt; manuelle Simulator-Abnahme außerhalb dieses Schritts (kein macOS-Zugriff lokal) |
| 4 | Paket-Inkompatibilität | Restore/Build des tvOS-Projekts | Paket wäre unter tvOS nicht auflösbar | Kompatibilitätsprüfung der kritischen Pakete | Dokumentiertes Ergebnis inkl. Ausweichvorschlag | `inventory/projects.md` + Dauer-Doku `docs/help/` |

## Offene Punkte

Keine.

Hinweis (kein offener Punkt, sondern verankertes Risiko): Sollte `macos-latest`
zwischenzeitlich auf Xcode 27 gewechselt sein und das `26.5`-Pack verwerfen, ist die
Rollback-Version auf `27.0.10742` anzuheben — Stelle und Begründung stehen als
Kommentar im Workflow.
