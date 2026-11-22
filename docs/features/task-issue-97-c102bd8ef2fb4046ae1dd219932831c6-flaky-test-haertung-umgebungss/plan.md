# Umsetzungsplan: Flaky-Test-Härtung der Test-Infrastruktur (Issue #97)

## Übersicht

Die Test-Infrastruktur wird nach der Blaupause `martin-stromberg/Softwareschmiede` (PR #432) gehärtet: Umgebungssensitive Tests in `VideoPlayer.Maui.Tests` und `VideoPlayer.E2E.Tests` werden per `[Trait("Category", "OsInterface")]` markiert, `.github/workflows/pr-checks.yml` erhält eine Filterung der blockierenden Lane plus eine neue, nicht blockierende Best-Effort-Lane `test-os-interface` mit TRX-Artefakten, automatisiertem Einmal-Re-Run und Flake-Auswertung. Ergänzend werden `AppInstance.DeleteProfileDirectory` um Retry/Backoff mit Protokollierung gehärtet, die Deadline-Staffelung im E2E-Startup konsolidiert und die Konvention (`Flaky` nur mit `OsInterface`) per Meta-Test abgesichert.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein CI-Testlauf von `VideoPlayer.Maui.Tests` (blockierende `verify`-Lane und neue `test-os-interface`-Lane) sowie die Markierungen/Infrastruktur von `VideoPlayer.E2E.Tests` (nur lokale Läufe). |
| Auslösende Ereignisse | `pull_request` auf `main`/`staging` (Job `verify` und `test-os-interface` in `pr-checks.yml`); lokale `dotnet test`-Aufrufe mit/ohne `--filter`. |
| Erwartete Wirkung | `verify` läuft nur nicht-umgebungssensitive Tests; `test-os-interface` läuft best-effort (`continue-on-error: true`), wiederholt markierte Fehlschläge genau einmal, lädt TRX-Artefakte hoch und wertet die Fehlschlag→Re-Run-grün-Rate aus (Quarantäne-Hinweis ab ≥ 2 in 10 Läufen). |
| Umgang mit fehlenden Objekten | `OsInterface`-Traits: **erzeugen** (gesamter Testbestand). Job `test-os-interface` inkl. TRX/`upload-artifact`: **erzeugen**. `scripts/Retry-FlakyTests.ps1`, `scripts/Analyze-FlakyTestResults.ps1`: **erzeugen**. `Support/TempDirectoryCleanup.cs`: **erzeugen**; `AppInstance.DeleteProfileDirectory` darauf umstellen. `docs/help/stabilitaet/os-interface-tests.md`: **erzeugen**. Vorhandene Härtung (`Kill(entireProcessTree: true)`, Best-Effort-`Dispose`, `process.json`-Diagnose, `AggregateException`-Aufräumphase): **wiederverwenden**. `Quarantäne`-Issue: **erzeugen** durch das Auswerteskript (Dedupe über offene Issues mit Label `flaky-test`). |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | `VideoPlayer.E2E.Tests` läuft **nicht** in CI; die Lane-Trennung gilt nur für `VideoPlayer.Maui.Tests`. | FlaUI/UIA3 benötigt eine interaktive Desktop-Session; gehostete `windows-latest`-Runner laufen nicht-interaktiv (Issue-Option b). Die E2E-Klassen erhalten trotzdem das `OsInterface`-Trait für lokale gefilterte Läufe. | bestätigt (SCM-Event-Vorgabe, autonom entschieden) |
| 2 | `#if DEBUG \|\| DEV_API`-gegatete OsInterface-Tests (`DevApiHttpServerTests`, `DevApiServerTests_Metadata`, `DevApiMetadataTests`) werden von der Release-CI-Lane `test-os-interface` nicht abgedeckt. | Beide CI-Lanes bauen Release (Parität zum `verify`-Build); die gegeteten Dateien kompilieren dort nicht mit. Sie werden per `Trait` markiert und sind lokal per `dotnet test -c Debug --filter "Category=OsInterface"` lauffähig; dokumentierter Hinweis in der Hilfeseite. | bestätigt (SCM-Event-Vorgabe) |
| 3 | Kein Blanket-Retry der Gesamtsuite, kein Auto-`Skip`/`Assert.Skip` für Flaky-Tests. | Nur ein automatisierter Re-Run der im Hauptlauf fehlgeschlagenen Tests innerhalb der Best-Effort-Lane; Quarantäne erfolgt als Issue-Prozess, nicht als Code-Ausblendung. | bestätigt (Requirement) |
| 4 | Keine Änderung an App-Laufzeitcode; keine ConPTY-/NTFS-Junction-Heuristiken; kein gemeinsamer Temp-Cleanup-Helfer für `Maui.Tests`. | Scope ist Test-Infrastruktur/CI/Doku; Vorlagen-Heuristiken kommen im Repo nicht vor (Bestandsaufnahme). Die Temp-Root-Deletes in `Maui.Tests` werden durch die `OsInterface`-Markierung in die Best-Effort-Lane verlagert — ein eigener Retry-Helfer dort ist nicht angefordert. | bestätigt (Requirement/Bestandsaufnahme) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| E2E-Suite in CI | **Lokal belassen** (Issue-Option b); nur `VideoPlayer.Maui.Tests` bekommt die Lane-Trennung. | Interaktive Desktop-Session auf gehosteten Runnern nicht verfügbar; Self-hosted-Runner ist separates Infrastrukturthema außerhalb des Issue-Scope. |
| `test-os-interface`-Build-Konfiguration | **Release**, identisch zum `verify`-Build (`--no-build` auf demselben Kompilat ist nicht nutzbar — eigener Job, eigener Build). | Parität der Kompilate; `#if`-Gating dokumentiert (s. Ausschluss 2). Debug-Lane würde zusätzliche DevApi-Listener-Tests abdecken, bricht aber die Release-Konsistenz und verlängert die Pipeline. |
| `Flaky`-Trait-Kodierung | Eigenes Trait-Paar `[Trait("Flaky", "true")]` **neben** `[Trait("Category", "OsInterface")]` — kein zweiter `Category`-Wert. | Mehrwertige `Category`-Traits machen die `--filter "Category!=OsInterface"`-Auswertung mehrdeutig (ein weiterer Category-Wert könnte die `!=`-Bedingung erfüllen und den Test in die blockierende Lane zurückholen). Orthogonaler Traitname ist filtertechnisch eindeutig. |
| E2E-Markierung | `[Trait("Category", "OsInterface")]` auf **jeder der 19 Testklassen** (plus `E2ETestBase` zur Dokumentation), nicht auf Vererbung/Assembly-Ebene verlassen. | Filterwirksamkeit von Basisklassen-/Assembly-Traits hängt von xUnit-v3-Interna der Trait-Aggregation ab; klassenweise Markierung ist garantiert filterbar. Der Meta-Test erzwingt die Markierung künftiger E2E-Klassen (Reflexion über Quelltexte, Muster der bestehenden Contract-Tests). |
| Markierung `UdpServerDiscoveryTests` | **Methodenebene** nur auf den 3 `DiscoverAsync_*`-Tests. | 5 der 8 Tests sind rein in-process (`ResolveAnnouncedAddress_*`); Klassenmarkierung würde deterministische Tests in die Best-Effort-Lane verlagern (Requirement: Klassenebene nur, wo die gesamte Fixture umgebungssensitiv ist). |
| Retry-Helfer | Neuer statischer Helfer `TempDirectoryCleanup` in `VideoPlayer.E2E.Tests/Support` statt Inline-Retry in `DeleteProfileDirectory`. | Blaupause-Analogon (`TempDirectoryCleanup`-Helfer); kapselt Versuchszahl, Backoff und Protokollierung an einer Stelle, wiederverwendbar für spätere Aufräumpfade. |
| Deadline-Staffelung | **Ein gemeinsames Startup-Deadline-Budget** in `AppInstance.StartAsync`: `WaitForDevApiPortAsync`, `WaitForMainWindow` und das erste `WaitForModelAsync` teilen sich das `startupTimeout`-Budget (Standard 45 s) statt eigener Vollbudgets. | Ist-Stand erlaubt bis zu 45+30+45 = 120 s (Kind-Budgets größer als Restbudget des Hosts) — genau der Vorlagen-Fehler „Host 30 s vs. Kind 90 s". Restbudget-Weitergabe macht Host > Kind strukturell unmöglich zu verletzen. `UiDriver` 15 s / `DevApiClient` 20 s bleiben (15 < 20 < 45 erfüllt die Konvention bereits). |
| Quarantäne-Schwellwert | **≥ 2 Flake-Fehlschläge** (Fehlschlag im Hauptlauf, grün im Re-Run) desselben Tests innerhalb der **letzten 10 ausgewerteten Läufe** → Report-Hinweis (`::warning` + Step-Summary) und `gh issue create` (Label `flaky-test`, Dedupe über offene Issues). | Issue-Vorschlag aus der Anforderung; 10 Läufe ≈ verfügbare Artefakt-Historie ohne Langzeit-Speicher. |
| Meta-Test-Platz | `OsInterfaceConventionTests` in `VideoPlayer.Maui.Tests`, **ohne** `OsInterface`-Trait (blockierende Lane), quelltextbasiert via `Stubs/RepositoryRoot.Find()`. | Entscheidung laut Vorgabe; folgt dem etablierten Contract-Test-Muster (31 Dateien lesen Repo-Quellen). |
| Doku-Ort | `docs/help/stabilitaet/os-interface-tests.md`, verlinkt in `docs/help/index.md` (Abschnitt „Infrastruktur"). | Entscheidung laut Vorgabe (analog Blaupause). |
| Artefakt-/Rechte-Modell | Job-eigene `permissions` auf `test-os-interface` (`contents: read`, `actions: read`, `issues: write`); Top-Level bleibt `contents: read`. | Artefakt-Download früherer Läufe (`gh run download`) braucht `actions: read`; Quarantäne-Issues brauchen `issues: write`. Least-Privilege per Job statt Rechteaufweitung des ganzen Workflows. |

## Programmabläufe

### CI-Lane-Trennung (PR-Check)

1. `pull_request` auf `main`/`staging` startet `pr-checks.yml`.
2. Job `verify` (unverändert blockierend): `Checkout` → `setup-dotnet` → Workload → `dotnet restore` → Build `VideoPlayer.Maui.Tests` (Release) → Build `VideoPlayer.Maui` → **Test**: `dotnet test ... --no-build -c Release --filter "Category!=OsInterface" --logger "trx" --results-directory TestResults/verify` → `actions/upload-artifact` (`if: always()`, Name `verify-trx`) → `Lint` → Branch-Guard.
3. Job `test-os-interface` (neu, parallel, `continue-on-error: true`, `timeout-minutes: 30`, `windows-latest`): `Checkout` → `setup-dotnet` → Workload → `dotnet restore` → Build Tests (Release) → **Test (Hauptlauf)**: `dotnet test ... -c Release --filter "Category=OsInterface" --logger "trx" --results-directory TestResults/os-main` mit `if: always()`-Nachfolgeschritten.
4. Schritt **Re-Run**: `pwsh scripts/Retry-FlakyTests.ps1 -TrxDirectory TestResults/os-main -Project VideoPlayer.Maui.Tests/... -Configuration Release -Filter "Category=OsInterface" -ResultsDirectory TestResults/os-rerun` — parst fehlgeschlagene Tests aus der Hauptlauf-TRX und wiederholt genau diese einmal (kein Re-Run bei 0 Fehlschlägen → Exit 0 mit Hinweis).
5. Schritt **Upload**: `actions/upload-artifact` (`if: always()`, Name `os-interface-trx`, beide Ergebnisverzeichnisse + E2E-/Prozesslogs falls vorhanden).
6. Schritt **Auswertung**: `pwsh scripts/Analyze-FlakyTestResults.ps1` mit `env: GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}` — lädt die `os-interface-trx`-Artefakte der letzten 10 `test-os-interface`-Läufe (`gh run list`/`gh run download`), berechnet pro Test die Flake-Rate, schreibt Step-Summary + `::warning` und erzeugt bei Schwellwert-Überschreitung deduplizierte Quarantäne-Issues (`gh issue create --label flaky-test`).

Beteiligte Komponenten: `.github/workflows/pr-checks.yml`, `scripts/Retry-FlakyTests.ps1`, `scripts/Analyze-FlakyTestResults.ps1`, `actions/upload-artifact`.

### Lokale gefilterte Läufe

1. Blockierende Teilmenge: `dotnet test VideoPlayer.Maui.Tests -c Release --filter "Category!=OsInterface"`.
2. OsInterface-Teilmenge (Release): `--filter "Category=OsInterface"`.
3. Debug-Gated OsInterface-Tests (nur lokal, da Release sie nicht kompiliert): `dotnet test VideoPlayer.Maui.Tests -c Debug --filter "Category=OsInterface&FullyQualifiedName~DevApi"`.
4. E2E lokal: `dotnet test VideoPlayer.E2E.Tests -c Debug --filter "Category=OsInterface"` (alle 74 Tests) bzw. ungefiltert wie bisher.

Beteiligte Komponenten: Trait-Markierungen beider Testprojekte, `docs/help/stabilitaet/os-interface-tests.md`.

### E2E-Startup mit gemeinsamem Deadline-Budget

1. `AppInstance.StartAsync` berechnet `deadline = DateTime.UtcNow + (startupTimeout ?? 45 s)` einmal.
2. `WaitForDevApiPortAsync(metadataPath, process, deadline, ct)` pollt `devapi.json` (200 ms Takt) bis `deadline`.
3. `WaitForMainWindow(automation, process, deadline)` erhält statt eigenem 30-s-Budget das **Restbudget** bis `deadline` (Attach + `GetMainWindow`-Schleife).
4. `instance.DevApi.WaitForModelAsync(_ => true, deadline - now, "Developer-API erreichbar", ct)` erhält ebenfalls das Restbudget statt des vollen `timeout`.
5. Bei Budgetüberschreitung bleibt die bestehende Fehlerbehandlung (`TryKillAndWait` + `WriteProcessDiagnostics` + Rethrow) unverändert.

Beteiligte Klassen: `AppInstance`, `DevApiClient` (unveränderte API, nur anderes Budget), `UiDriver` (unverändert).

### Profil-Cleanup mit Retry/Backoff

1. `AppInstance.Dispose(bool)` ruft wie bisher `DeleteProfileDirectory()`.
2. `DeleteProfileDirectory` iteriert die Kandidatenverzeichnisse (`%APPDATA%`, `%LOCALAPPDATA%`, `nobackup`, `Cache`) und ruft je Kandidat `TempDirectoryCleanup.DeleteWithRetry(dir, log: Console-Error-Writer)` auf.
3. `DeleteWithRetry` versucht `Directory.Delete(dir, recursive: true)` bis zu 3× mit Backoff 200/500/1000 ms; nur `IOException`/`UnauthorizedAccessException` gelten als retrywürdig.
4. Jeder fehlgeschlagene Versuch und der verbleibende Restfehler werden über den `log`-Callback protokolliert (Pfad, Versuch, Exception-Typ/-Message); Restfehler schlagen den Dispose nicht mehr still durch, sondern bleiben best-effort mit Protokollspur.
5. `TryDelete` (`devapi.json`) erhält denselben `log`-Callback (optionaler Parameter), protokolliert Restfehler ebenfalls statt kommentarlos.

Beteiligte Klassen: `AppInstance`, `TempDirectoryCleanup` (neu).

### Konventions-Meta-Test

1. `OsInterfaceConventionTests` (in `Maui.Tests`, kein Trait) löst den Repo-Wurzel über `Stubs/RepositoryRoot.Find()` auf.
2. Regel 1: Jede `.cs`-Datei beider Testprojekte, die ein `[Trait("Flaky"`-Attribut enthält, muss in derselben Datei auch `[Trait("Category", "OsInterface")]` enthalten (die eigene Meta-Test-Datei wird vom Scan ausgenommen, da sie die Suchmuster als String-Literale enthält).
3. Regel 2: Jede Testklassen-Datei unter `VideoPlayer.E2E.Tests/Tests/` (außer `E2ETestBase.cs`) enthält `[Trait("Category", "OsInterface")]` — erzwingt die Markierung neuer E2E-Klassen.
4. Verstöße werden als benannte Liste im Assert-Fehlertext ausgegeben.

Beteiligte Klassen: `OsInterfaceConventionTests` (neu), `Stubs/RepositoryRoot`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `TempDirectoryCleanup` | statische Helferklasse (`VideoPlayer.E2E.Tests/Support/TempDirectoryCleanup.cs`) | `DeleteWithRetry(path, maxAttempts = 3, Action<string>? log)`: rekursives Löschen mit Backoff (200/500/1000 ms) bei `IOException`/`UnauthorizedAccessException` und Protokollierung aller Versuche/Restfehler. |
| `OsInterfaceConventionTests` | Testklasse (`VideoPlayer.Maui.Tests/OsInterfaceConventionTests.cs`) | Quelltext-Contract-Test: `Flaky`-Trait nur zusammen mit `OsInterface`; jede E2E-Testklasse trägt `OsInterface`. Läuft ohne Trait in der blockierenden Lane. |
| `scripts/Retry-FlakyTests.ps1` | PowerShell-Skript | Liest fehlgeschlagene Tests aus einer Hauptlauf-TRX und führt genau einen gefilterten `dotnet test`-Re-Run aus (neue TRX im Zielverzeichnis); Exit 0 auch bei Testfehlschlägen im Re-Run (Ergebnis liegt in der TRX), Fehler nur bei Aufruf-/Infrastrukturproblemen. |
| `scripts/Analyze-FlakyTestResults.ps1` | PowerShell-Skript | Lädt die `os-interface-trx`-Artefakte der letzten 10 Läufe via `gh`, berechnet pro Test die Rate „Hauptlauf fehlgeschlagen, Re-Run bestanden", schreibt Step-Summary/`::warning` und erstellt ab ≥ 2 Flakes deduplizierte Quarantäne-Issues (Label `flaky-test`). Schwellwerte als Parameter (`-RunLimit 10`, `-FlakeThreshold 2`). |

## Änderungen an bestehenden Klassen

### `pr-checks.yml` (Workflow)

- **Geänderter Schritt `Test` (Job `verify`):** ergänzt `--filter "Category!=OsInterface"`, `--logger "trx"`, `--results-directory TestResults/verify`.
- **Neuer Schritt `Upload test results` (Job `verify`):** `actions/upload-artifact@v4`, `if: always()`, Name `verify-trx`.
- **Neuer Job `test-os-interface`:** `runs-on: windows-latest`, `continue-on-error: true`, `timeout-minutes: 30`, job-eigene `permissions` (`contents: read`, `actions: read`, `issues: write`); Schritte Checkout/Setup/Workload/Restore/Build (Release) → Hauptlauf `--filter "Category=OsInterface"` + TRX → `Retry-FlakyTests.ps1` (`if: always()`) → `upload-artifact` (`if: always()`) → `Analyze-FlakyTestResults.ps1` (`if: always()`, `GH_TOKEN`).

### Testklassen `VideoPlayer.Maui.Tests` (Markierungen)

- **Klassenebene `[Trait("Category", "OsInterface")]`** (Trait innerhalb des `#if`-Blocks bei gegeteten Dateien, `DevApiContractTests` prüft weiterhin Zeile 1): `DevApiHttpServerTests`, `DevApiServerTests_Metadata`, `DevApiMetadataTests`, `IosDeploymentResponseFileTests`, `LocalDatabaseTests`, `FileLoggerTests`, `AppLogServiceTests`, `AppStorageLayoutTests`, `AutoDownloadServiceTests`, `UpcomingEpisodesServiceTests`, `ServerSyncServiceTests`, `SeasonalGenreTests`, `ProgressReportServiceTests`, `PlaybackSourceServiceTests`, `PendingProgressSyncServiceTests`, `OfflineContinueWatchingServiceTests`, `MediaDetailsServiceTests`, `KnownServersServiceTests`, `HomeListServiceTests`, `FavoritesServiceTests`, `DownloadServiceTests`, `DownloadServiceTests_Expiry`, `DownloadServiceReconcileTests`, `ContinueWatchingMutationServiceTests`.
- **Methodenebene:** `UdpServerDiscoveryTests` — nur die drei `DiscoverAsync_*`-Tests.
- **Nicht markiert:** Contract-Tests, `ClientGate`-Collection, `StubHttpMessageHandler`-basierte Tests (Audit-Ergebnis: hermetisch bzw. Threading- statt Umgebungssensitivität).

### Testklassen `VideoPlayer.E2E.Tests` (Markierungen)

- **Klassenebene `[Trait("Category", "OsInterface")]`** auf allen 19 Testklassen (`BootstrapTests` … `SourceSearchTests`) und auf `E2ETestBase` (Dokumentation der Fixture-Sensitivität).

### `AppInstance` (`VideoPlayer.E2E.Tests/Support/AppInstance.cs`)

- **Geänderte Methode `StartAsync`:** berechnet einmal `deadline`; übergibt Restbudget an `WaitForDevApiPortAsync`, `WaitForMainWindow` und das initiale `WaitForModelAsync`.
- **Geänderte Methode `WaitForMainWindow`:** Parameter `TimeSpan`/`deadline` statt festem 30-s-Budget (Zeile 403); Schleifenende am übergebenen Restbudget.
- **Geänderte Methode `DeleteProfileDirectory`:** ruft je Kandidat `TempDirectoryCleanup.DeleteWithRetry` mit `Console.Error`-Log-Callback; ersetzt den stillen `catch (Exception) {}`.
- **Geänderte Methode `TryDelete`:** optionaler `Action<string>? log`-Parameter; Restfehler werden protokolliert statt kommentarlos geschluckt.

### `docs/help/index.md`, `docs/help/e2e-tests.md` (Doku)

- `index.md`: Eintrag „OsInterface-Tests und Flake-Quarantäne" unter „Infrastruktur" → `stabilitaet/os-interface-tests.md`.
- `e2e-tests.md`: Hinweis, dass die Suite umgebungssensitiv/`OsInterface`-markiert und bewusst nicht Teil der CI ist (interaktive Desktop-Session); Verweis auf die neue Hilfeseite; ggf. veraltete Zählung „19 Tests" auf 19 Klassen/74 Fälle korrigieren.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `Retry-FlakyTests.ps1 -TrxDirectory` | Verzeichnis muss existieren und ≥ 0 `.trx` enthalten; 0 Dateien oder 0 Fehlschläge → kein Re-Run, Exit 0 mit Hinweis | Fehlendes Verzeichnis → Exit 1 mit Meldung (Skript-Missbrauch) |
| `Analyze-FlakyTestResults.ps1` | `-RunLimit ≥ 1`, `-FlakeThreshold ≥ 1`; ohne `GH_TOKEN` nur lokale Auswertung (`-ArtifactDirectory`), kein Download/Issue | Parameter < 1 → `throw` per `ValidateRange` |
| Quarantäne-Issue | Dedupe: kein neues Issue, wenn bereits ein offenes Issue mit Label `flaky-test` und dem Testnamen im Titel existiert | `gh issue list --search` Treffer → Hinweis statt Duplikat |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `[Trait("Category", "OsInterface")]` | Test-Markierung | — | Lane-Zuordnung per `dotnet test --filter` |
| `[Trait("Flaky", "true")]` | Test-Markierung | — | Kennzeichnung bekannter Flakes (nur mit `OsInterface`, per Meta-Test erzwungen); aktuell 0 Anwendungen |
| Job `test-os-interface.permissions` | Workflow | `contents: read`, `actions: read`, `issues: write` | Artefakt-Download + Quarantäne-Issues, least privilege per Job |
| Artefaktname `verify-trx` / `os-interface-trx` | Workflow | — | TRX-Upload je Lane; `os-interface-trx` ist Quelle der Flake-Historie |
| `-RunLimit` / `-FlakeThreshold` | Skript-Parameter | 10 / 2 | Auswertefenster und Quarantäne-Schwellwert |

## Seiteneffekte und Risiken

- **Verringerte PR-blockierende Abdeckung:** ~24 markierte `Maui.Tests`-Klassen (u. a. alle Temp-Dateisystem- und `SqliteDatabase`-Collection-Tests) laufen künftig best-effort. Deterministisch auf sauberem Runner, aber Dateisystem-Regressionen blockieren keinen PR mehr — bewusste Zielbild-Entscheidung („selten, lokalisiert, selbstdiagnostizierend"), durch Quarantäne-Prozess abgefedert.
- **`DevApiContractTests` (Zeile-1-Regel):** Trait-Markierungen müssen **innerhalb** des `#if DEBUG || DEV_API`-Blocks stehen, da der Contract-Test `#if` als erste Zeile der DevApi-Dateien erzwingt.
- **`UdpServerDiscoveryTests` Methodenmarkierung:** die 5 `ResolveAnnouncedAddress_*`-Tests bleiben absichtlich in der blockierenden Lane; Meta-Test-Regel 2 darf die Klasse nicht als verletzt melden (nur `Flaky`-Regel + E2E-Klassen-Regel).
- **Artefakt-Volumen:** TRX-Uploads pro PR-Lauf; `Analyze-FlakyTestResults.ps1` ist auf `actions: read` + verfügbare Artefakt-Historie angewiesen — bei Artefakt-Retention < 10 Läufe wertet es automatisch nur die verfügbaren Läufe aus (dokumentiert).
- **Deadline-Konsolidierung `StartAsync`:** Gesamtbudget 45 s deckt nun devapi-Poll + Fenster-Attach + erstes Modell ab (bisher bis ~120 s möglich). Lokale Läufe auf sehr langsamen Maschinen könnten früher in den Timeout laufen; `startupTimeout` bleibt pro Aufruf übersteuerbar (`StartAppAsync`-Parameter).
- **`dotnet format whitespace` (verify-Lane):** neue/geänderte `.cs`-Dateien müssen format-clean sein.
- **E2E-Trait-Vererbung:** falls zukünftig jemand auf Basisklassen-Trait setzt, verhindert Meta-Test-Regel 2 den Verlust der Markierung.

## Umsetzungsreihenfolge

1. **Trait-Markierungen `VideoPlayer.Maui.Tests`**
   - Voraussetzungen: Keine (xunit.v3 3.2.2 vorhanden; `[Trait]` ohne Zusatzpaket).
   - Beschreibung: Klassen-/Methoden-Traits gemäß „Änderungen an bestehenden Klassen"; bei `#if`-gegeteten Dateien Trait innerhalb des Blocks.
2. **Trait-Markierungen `VideoPlayer.E2E.Tests`**
   - Voraussetzungen: Keine.
   - Beschreibung: `[Trait("Category", "OsInterface")]` auf alle 19 Testklassen + `E2ETestBase`.
3. **Meta-Test `OsInterfaceConventionTests`**
   - Voraussetzungen: Schritte 1–2 (sonst schlägt Regel 2 sofort fehl); `Stubs/RepositoryRoot` vorhanden.
   - Beschreibung: Neue Datei in `VideoPlayer.Maui.Tests`, keine Trait-Markierung; implementiert Regel 1 + 2.
4. **`TempDirectoryCleanup`-Helfer + `AppInstance`-Umbau**
   - Voraussetzungen: Keine (rein interner Support-Code).
   - Beschreibung: Neuer Helfer `Support/TempDirectoryCleanup.cs`; `DeleteProfileDirectory` und `TryDelete` auf Retry/Logging umstellen; `StartAsync`/`WaitForMainWindow` auf gemeinsames Deadline-Budget.
5. **Skript `scripts/Retry-FlakyTests.ps1`**
   - Voraussetzungen: Keine (pwsh auf Runnern vorhanden).
   - Beschreibung: TRX-Parsing (XDocument), Filter-Aufbau `FullyQualifiedName~a|…`, genau ein Re-Run-Aufruf.
6. **Skript `scripts/Analyze-FlakyTestResults.ps1`**
   - Voraussetzungen: Schritt 5 liefert Re-Run-TRX-Paare (Auswertelogik definiert Hauptlauf-/Re-Run-Zuordnung über Verzeichnisstruktur `os-main`/`os-rerun` im Artefakt).
   - Beschreibung: Artefakt-Download via `gh`, Flake-Rate, Summary/Warning, Quarantäne-Issues mit Dedupe.
7. **Workflow `pr-checks.yml` umbauen**
   - Voraussetzungen: Schritte 1–2 (Filter wirken), 5–6 (Skripte existieren), sonst schlägt der CI-Lauf fehl.
   - Beschreibung: `verify`-Test-Schritt erweitern (Filter/TRX/Upload), Job `test-os-interface` hinzufügen.
8. **Lokale Verifikation der Filterung**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: `dotnet test -c Release --filter "Category!=OsInterface"` (muss exakt die Baseline 1484 minus markierte Tests ergeben und grün sein), `--filter "Category=OsInterface"` (nur markierte), `-c Debug --filter "Category=OsInterface&FullyQualifiedName~DevApi"` (8 gegetete Tests: 5 `DevApiHttpServerTests` + 3 `DevApiServerTests_Metadata` — `DevApiMetadataTests` zählt ggf. zusätzlich je nach Markierung), Ergebniszahlen im PR dokumentieren.
9. **Doku**
   - Voraussetzungen: Schritte 7–8 (Workflow- und Filter-Stand beschreibbar).
   - Beschreibung: `docs/help/stabilitaet/os-interface-tests.md` neu (Lane-Regeln, Trait-/Flaky-Konvention, Re-Run-Regel inkl. `gh run rerun --job <id>` nur für den OS-Job, Quarantäne-Prozess mit Schwellwert, Hinweis auf Debug-gegetete Tests); `index.md`-Eintrag; `e2e-tests.md`-CI-Hinweis.
10. **CI-Nachweis**
    - Voraussetzungen: Schritte 1–9 gemergt auf den Task-Branch.
    - Beschreibung: PR-Läufe beobachten — `verify` grün ohne OsInterface-Tests, `test-os-interface` sichtbar als best-effort, Artefakte vorhanden; Zielbild laut Anforderung: 3 komplette grüne Durchläufe dokumentieren.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `FlakyTrait_RequiresOsInterface` | `OsInterfaceConventionTests` | Scannt `VideoPlayer.Maui.Tests/*.cs` + `VideoPlayer.E2E.Tests/**/*.cs` auf `[Trait("Flaky"` ohne `[Trait("Category", "OsInterface")]` in derselben Datei; listet Verstöße. |
| `E2eTestClasses_CarryOsInterfaceTrait` | `OsInterfaceConventionTests` | Jede `*Tests.cs` unter `VideoPlayer.E2E.Tests/Tests/` (außer `E2ETestBase.cs`) enthält das Trait — sichert die Markierung zukünftiger Klassen. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Alle ~24 markierten `Maui.Tests`-Klassen + 3 Methoden in `UdpServerDiscoveryTests` | Erhalten `[Trait("Category", "OsInterface")]` — inhaltlich unverändert, laufen künftig in `test-os-interface`. |
| Alle 19 E2E-Testklassen + `E2ETestBase` | Erhalten das `OsInterface`-Trait. |
| `DevApiContractTests` (Zeile-1-Regel) | Keine Anpassung nötig, aber Constraint für die Markierung: Trait muss innerhalb des `#if`-Blocks stehen — im Umsetzungsschritt zu beachten und per Lauf zu verifizieren. |

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich: Die Anforderung betrifft keinen anwenderseitigen Benutzerfluss der App — sie ändert ausschließlich Test-Infrastruktur, CI-Jobs, Skripte und Doku. Der tatsächliche „Fluss" ist der CI-Lauf selbst und wird über die Akzeptanzszenarien (grüne Lanes, Artefakte, Quarantäne-Report) nachgewiesen. Die **bestehende** E2E-Suite wird gehärtet (Deadline-Budget, Cleanup-Retry); ihr Funktionsnachweis bleibt der lokale Volllauf (`dotnet test VideoPlayer.E2E.Tests -c Debug`, ~20–25 min, interaktive Session) — im Umsetzungsschritt als einmaliger lokaler Nachweis einzuplanen, nicht als neuer Test.

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Alle E2E-Testklassen (indirekt) | Laufen über die gehärtete `AppInstance`-Startup-/Cleanup-Kette; kein inhaltlicher Umbau der Testfälle. |

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path CI | PR auf `staging` | Workflow ohne Trait-/Lane-Trennung; `OsInterface`-Kategorie existiert noch nicht | `verify` baut Release, führt `--filter "Category!=OsInterface"` aus; `test-os-interface` führt `--filter "Category=OsInterface"` aus, Re-Run-Schritt findet keine Fehlschläge | `verify` grün (nur nicht-umgebungssensitive Tests); `test-os-interface` grün; Artefakte `verify-trx`/`os-interface-trx` unter „Artifacts" des Runs | `gh pr checks` + `gh run view` / Artefakt-Listing |
| 2 | Flake in Best-Effort-Lane | OsInterface-Test fällt im Hauptlauf aus | Szenario 1 umgesetzt | Hauptlauf fehlgeschlagen → `Retry-FlakyTests.ps1` wiederholt nur die fehlgeschlagenen Tests → Re-Run grün | Job bleibt non-blocking; Step-Summary/`::warning` zeigt „1 Flake"; `gh run view` zeigt beide TRX im Artefakt; PR-Status unverändert grün | Re-Run-TRX + Step-Summary |
| 3 | Quarantäne-Hinweis | Derselbe Test flakt zum 2. Mal innerhalb von 10 Läufen | Flake-Historie in Artefakten vorhanden | `Analyze-FlakyTestResults.ps1` aggregiert die letzten 10 `os-interface-trx`-Artefakte | `::warning` + Summary-Tabelle nennen den Test als Quarantäne-Kandidat; dedupliziertes Issue mit Label `flaky-test` wird angelegt | Issue-Liste / Run-Summary |
| 4 | Lokaler gefilterter Lauf (Debug-Gating) | Entwickler führt `dotnet test -c Debug --filter "Category=OsInterface&FullyQualifiedName~DevApi"` aus | DevApi-Tests `#if`-gegetet, markiert | Filter-Ausführung | Genau die markierten DevApi-Listener-Tests laufen grün; in Release existieren sie nicht (dokumentiert) | Lokaler Testlauf |
| 5 | Konventionsverstoß | Entwickler setzt `[Trait("Flaky", "true")]` ohne `OsInterface` oder legt E2E-Testklasse ohne Trait an | Meta-Test vorhanden | `verify`-Lane läuft | `OsInterfaceConventionTests` schlägt fehl und nennt Datei/Klasse — Verstoß ist PR-blockierend | Fehlgeschlagener Meta-Test |
| 6 | Cleanup-Resilienz E2E (lokal) | E2E-Lauf beendet App mit gesperrter Profildatei (z. B. noch geöffneter SQLite-Handle) | Bestehender Cleanup schluckt Fehler still | `Dispose` → `DeleteProfileDirectory` → `TempDirectoryCleanup.DeleteWithRetry` | Bis zu 3 Löschversuche mit Backoff; Restfehler erscheinen protokolliert (Pfad, Versuch, Exception) statt kommentarlos; Testergebnis bleibt durch `AggregateException`-Aufräumphase entkoppelt | Lokaler E2E-Lauf, Konsolen-/Prozesslog |

## Offene Punkte

Keine — die vier Issue-Fragen sind entschieden und oben eingearbeitet (E2E bleibt lokal → Scope-Ausschluss 1 + Designentscheidung; Schwellwert ≥ 2/10 → Designentscheidung „Quarantäne-Schwellwert"; Meta-Test in `Maui.Tests` ohne Trait → Designentscheidung „Meta-Test-Platz"; Doku `docs/help/stabilitaet/os-interface-tests.md` → Designentscheidung „Doku-Ort").
