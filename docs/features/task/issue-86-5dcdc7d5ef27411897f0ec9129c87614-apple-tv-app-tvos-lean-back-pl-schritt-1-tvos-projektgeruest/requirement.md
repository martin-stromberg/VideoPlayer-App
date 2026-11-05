# Übersetzte Anforderung: tvOS-Projektgerüst und frühe Build-Validierung über macOS-CI

Quelle: Entwicklungsschritt 1 des Projektplans „Apple TV-App (tvOS) — Lean-back-Player via .NET for tvOS"
(`docs/projects/task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl/project-plan.md`).

## Fachliche Zusammenfassung

Es wird ein neues, eigenständiges App-Projekt für Apple TV (tvOS) angelegt, das als
technisches Fundament des geplanten Lean-back-Players dient. Das Projekt zielt auf
`net10.0-tvos`, referenziert ausschließlich die plattformneutralen `net10.0`-Bibliotheken
der Lösung (`VideoPlayer.Mvu`, `VideoPlayer.Storage`, `VideoPlayer.ApiClient`,
`msTools.DeveloperApi`) und startet als minimale, leere Programmhülle mit Fenster und
einfacher Startansicht — ohne fachliche Funktion. Da der Windows-Entwicklungsrechner
`net10.0-tvos` weder kompilieren noch ausführen kann, sichert ein neuer CI-Job auf einem
macOS-Runner die Kompilierbarkeit fortlaufend ab (Simulator-Build ohne Codesignierung);
die tvOS-Workload wird dabei analog zum bestehenden iOS-Pfad per Rollback-Datei gepinnt.
Die NuGet-Kompatibilität der kritischen Pakete (SQLite-Bindung, mDNS-Bibliothek) unter
`net10.0-tvos` ist früh zu prüfen und zu dokumentieren.

## Fachlicher Scope

- **Verarbeitungseinheit:** Das tvOS-App-Projekt als Build- und Auslieferungseinheit —
  neues Projekt `VideoPlayer.TvOS` (eigenständige .NET-for-tvOS-App, kein MAUI), ein
  CI-Build-Job für den tvOS-Simulator, eine Workload-Pinning-Datei und ein dokumentiertes
  Paket-Kompatibilitätsergebnis.
- **Auslösende Ereignisse:** Pull Request auf `main`/`staging` (CI-Job baut tvOS-Projekt);
  Entwickler öffnet die Solution (Projekt ist Teil der `VideoPlayer.App.sln`); tvOS-App
  wird auf dem Apple-TV-Simulator gestartet (zeigt leere Startansicht).
- **Erwartete Wirkung:** Der macOS-CI-Job kompiliert das tvOS-Projekt bei jedem PR-Lauf
  erfolgreich für `tvossimulator-*`; die Solution bleibt unter Windows weiterhin
  restore-/build-/testbar; das tvOS-Projekt erscheint als Solution-Mitglied; die
  Paket-Kompatibilitätsprüfung liegt als dauerhaftes Dokument vor.
- **Umgang mit fehlenden Objekten:** tvOS-Workload auf dem CI-Runner → wird im Job per
  gepinnter Rollback-Datei installiert (erzeugen). Signatur-Artefakte (Zertifikate,
  Provisioning-Profile, eigene Bundle-ID im Developer-Account) existieren noch nicht →
  kein Objekt des Codes, Simulator-Build läuft ad-hoc ohne Codesignierung; produktive
  Signatur ist explizit späterem Projektschritt vorbehalten. tvOS-Workload auf dem
  Windows-Entwicklungsrechner → kann nicht installiert werden; das Projekt muss so
  beschaffen sein, dass `dotnet restore`/`dotnet build`/`dotnet test` der Solution unter
  Windows weiter funktionieren (Fallback-Modus ohne tvOS-TFM).
- **Scope-Ausschlüsse:**
  1. Keine fachliche Funktion in der tvOS-App (keine Serveranbindung, kein UI-Inhalt
     außer Platzhalter-Startansicht).
  2. Keine Signierung/Paketierung/kein App-Store-Upload — Simulator-Build ohne
     Codesignierung genügt (produktiver Pfad ist Projektschritt 11).
  3. Keine tvOS-Testsuite und kein UI-Test-Framework für tvOS — der Kompilier-Job ist
     der Funktionsnachweis dieses Schritts.
  4. Keine Änderungen an den plattformneutralen Bibliotheken und kein Verdrahten
     tvOS-irrelevanter Modellanteile.
  5. Kein lokales tvOS-Deployment-Skript (kommt in Projektschritt 11).

## Betroffene Klassen und Komponenten

Neu anzulegen:
- Projekt `VideoPlayer.TvOS` (`VideoPlayer.TvOS/VideoPlayer.TvOS.csproj`) — `net10.0-tvos`,
  `OutputType=Exe`, nur `ProjectReference` auf die vier `net10.0`-Bibliotheken.
- `Info.plist` (Bundle-Identifier `de.martinstromberg.videos.tv` — Arbeitsannahme,
  finale Bundle-ID ist manueller Vorlauf im Apple-Developer-Account).
- `Program.cs` (Einstiegspunkt `UIApplication.Main`).
- `AppDelegate.cs` (`UIApplicationDelegate` mit `UIWindow` und einfachem
  `UIViewController` als Startansicht).
- CI-Job `build-tvos` in `.github/workflows/pr-checks.yml` (macOS-Runner,
  Workload-Rollback `{"microsoft.net.sdk.tvos": "26.5.10318"}`, Simulator-Build ohne
  Codesignierung).
- Solution-Eintrag in `VideoPlayer.App.sln`.
- Dauerhafte Kompatibilitäts-/Einrichtungsdokumentation unter `docs/help/`.

Bestehende Projekte bleiben unverändert; `VideoPlayer.App.sln` wird nur um den neuen
Projekteintrag erweitert.

## Implementierungsansatz

- .NET-for-tvOS-App nach nativem Xamarin/.NET-Muster (`UIApplication.Main`,
  `UIApplicationDelegate`, `UIWindow`, `UIViewController`, `UILabel`) — kein MAUI,
  kein `UseMaui`.
- Da die tvOS-Workload unter Windows nicht installierbar ist, wird das Ziel-Framework
  des tvOS-Projekts nach dem im `VideoPlayer.Maui.csproj` bewährten Schalter-Muster
  konditionalisiert: `net10.0-tvos` nur unter macOS (und über eine explizite Property
  übersteuerbar), sonst degradiert das Projekt zu einer leeren `net10.0`-Bibliothek,
  damit Solution-Restore/-Build/-Tests unter Windows und in der Windows-PR-CI
  unverändert funktionieren.
- CI-Job spiegelt den iOS-Build-Job aus `main-merge.yml`/`staging-merge.yml`:
  `setup-dotnet` → `global.json`-SDK-Band-Pin (`10.0.100`, `rollForward: latestPatch`)
  → Workload-Rollback aus `printf`-JSON-Datei → `dotnet workload install tvos
  --skip-manifest-update` → `dotnet build -r tvossimulator-arm64
  -p:EnableCodeSigning=false`.
- Runner: `macos-latest` (Release-Xcode 26.x) mit tvOS-Manifest `26.5.10318`
  — analog dem `main-merge.yml`-Rollback `{"microsoft.net.sdk.ios": "26.5.10318"}`.

## Konfiguration

- Workload-Version-Pinning: Rollback-JSON inline im Workflow (Muster des iOS-Pfads:
  `printf '{...}' > "$RUNNER_TEMP/workload-rollback.json"`).
- `global.json` wird wie im iOS-Job zur Laufzeit im Job-Wurzelverzeichnis erzeugt —
  keine dauerhafte `global.json` im Repo (würde die Windows-Entwicklung beeinflussen).

## Akzeptanzkriterien (aus der Anforderung)

1. Das tvOS-Projekt ist Teil der `VideoPlayer.App.sln` und referenziert ausschließlich
   die plattformneutralen Projekte (`VideoPlayer.Mvu`, `VideoPlayer.Storage`,
   `VideoPlayer.ApiClient`, `msTools.DeveloperApi`).
2. Der neue CI-Job baut das tvOS-Projekt auf macOS erfolgreich (Simulator-Build ohne
   Codesignierung).
3. Die Kompatibilitätsprüfung der kritischen Pakete (insbesondere SQLite-Bindung
   `SQLitePCLRaw.*`/`sqlite-net-pcl` und mDNS-Bibliothek `Zeroconf`) ist dokumentiert;
   bei Inkompatibilität mit benanntem Ausweichvorschlag (z. B. UDP-only-Discovery bzw.
   Ersatzbindung).
4. Das Workload-Pinning ist analog zum iOS-Pfad hinterlegt.
5. Die bestehenden Projekte und ihre Tests bleiben unverändert lauffähig.

## Offene Fragen

Keine — die fachlichen Vorgaben sind im Projektplan bereits geklärt (kein neuer
Server-Vertrag, Deployment-Gate-Ansatz, Bundle-ID als manueller Vorlauf). Die konkrete
tvOS-Manifestversion (`26.5.10318`) ist gegen nuget.org verifiziert
(`microsoft.net.sdk.tvos.manifest-10.0.100`); falls der Runner eine andere
Xcode-Generation trägt, ist die Pinning-Zahl analog anzupassen — dies ist im Plan als
Risiko vermerkt.
