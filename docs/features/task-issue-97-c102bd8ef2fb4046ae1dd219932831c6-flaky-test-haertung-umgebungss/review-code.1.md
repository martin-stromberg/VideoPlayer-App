# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### Analyze-FlakyTestResults.ps1

- **Fehlerbehandlung** — Native `gh`-Aufrufe prüfen `$LASTEXITCODE` nicht
  (unter pwsh ist `$PSNativeCommandUseErrorActionPreference` standardmäßig
  aus; `$ErrorActionPreference = "Stop"` greift bei nativen Kommandos
  nicht). Konkret:
  - `gh issue create` (Z. 216) gefolgt von `Write-Host
    "Quarantaene-Issue angelegt"` (Z. 217) meldet Erfolg auch bei
    Fehlschlag (Read-Only-`GITHUB_TOKEN` bei Fork-PRs, API-/Netzfehler).
  - `gh label list` / `gh issue list` (Z. 193, 201) und `gh run list`
    (Z. 127): Bei Fehlschlag ist stdout leer → `ConvertFrom-Json` liefert
    nichts → `$existing`/`$duplicates`/`$runs` werden leer ausgewertet —
    ein defektes `gh` oder fehlende Rechte degradieren die Auswertung
    still zu „0 Läufe" bzw. lösen einen Issue-Create-Versuch ohne
    vorhandenes Label aus (der dann ebenfalls ungeprüft scheitert).

  Empfehlung: Nach jedem `gh`-Aufruf `$LASTEXITCODE` prüfen und
  Fehlschläge als `::warning` mit Kontext protokollieren; die Meldung
  „Quarantaene-Issue angelegt" nur bei Exit 0 ausgeben. `2>$null` bei
  `gh label create`/`gh run download` ist als Race-/Fehlartefakt-Dämpfung
  akzeptabel, sollte aber ebenfalls eine Warnung auslösen statt
  kommentarlos zu schlucken.

- **Doppelter Code** — Die TRX-Auflösung (TestDefinitions →
  `className.name`-FQN, Results → Outcome, `testName`-Regex-Fallback)
  ist nahezu identisch in `Retry-FlakyTests.ps1` (Z. 54–84) und in
  `Get-TestOutcomes` (Z. 43–71) dupliziert; ein Schema- oder
  Parsing-Fix müsste an zwei Stellen gepflegt werden.

  Empfehlung: Gemeinsames Helper-Skript (z. B.
  `scripts/lib/Trx.ps1`) per Dot-Sourcing einbinden oder zumindest
  einen gegenseitigen Kommentarverweis setzen.

### OsInterfaceConventionTests.cs

- **Testqualität** — Die Konventionsprüfung arbeitet auf
  Dateigranularität (`source.Contains`, Z. 55–56 und 80) und kann das
  Trait nicht der Klasse bzw. Methode zuordnen:
  - `FlakyTrait_RequiresOsInterface`: `[Trait("Flaky", …)]` auf Methode
    A plus `[Trait("Category", "OsInterface")]` auf Methode B derselben
    Datei lässt den Check grün werden, obwohl A unmarkiert in der
    blockierenden Lane läuft — konkretes Risiko bei methodenweiser
    Vergabe wie in `UdpServerDiscoveryTests`.
  - `E2eTestClasses_CarryOsInterfaceTrait`: Ein methodenlokales Trait
    oder ein Trait-Literal in einem Kommentar erfüllt den Check, ohne
    dass die Klasse markiert ist.

  Empfehlung: Deklarationsbezogen prüfen — z. B. zeilenweise, dass das
  OsInterface-Trait unmittelbar über der `class`-Deklaration steht, und
  für die Flaky-Regel pro `[Fact]`/`[Theory]`-Attributblock die
  zugehörige Methoden-/Klassenzeile auswerten.

- **Fehlende/inkorrekte Dokumentation** — Der `<summary>`-Kommentar zu
  `E2eTestClasses_CarryOsInterfaceTrait` (Z. 66–71) behauptet eine
  Ausnahme „außer `E2ETestBase.cs`": Die Datei matcht das Muster
  `*Tests.cs` gar nicht (endet nicht auf „Tests.cs") und trägt das
  Trait zudem selbst — die Ausnahme existiert weder im Code noch wäre
  sie sachlich richtig.

  Empfehlung: Kommentar auf „alle `*Tests.cs`-Dateien unter
  `VideoPlayer.E2E.Tests/Tests`" korrigieren.

### AppInstance.cs (VideoPlayer.E2E.Tests)

- **Fehlermeldung ohne aussagekräftigen Kontext** — In `StartAsync`
  wird `WaitForModelAsync` mit `deadline - DateTime.UtcNow` (Z. 119)
  aufgerufen. Ist das Startup-Budget durch devapi-Poll und
  Fenster-Attach bereits aufgebraucht, ist das Restbudget ≤ 0:
  `WaitForModelAsync` pollt dann kein einziges Mal und wirft
  `TimeoutException` mit „Letzter Zustand: (kein Snapshot lesbar: )" —
  die Meldung suggeriert ein API-Problem, die eigentliche Ursache
  (erschöpftes Gesamtbudget) fehlt.

  Empfehlung: Restbudget auf ≥ 0 clampen und bei ≤ 0 direkt eine
  `TimeoutException` mit Budget-Kontext werfen (z. B. „Startup-Budget
  von 45 s durch devapi-Poll/Fenster-Attach erschöpft"), alternativ
  eine Mindest-Restzeit von z. B. 1–2 s einmalig pollen lassen.

### Retry-FlakyTests.ps1

- **Fehlerbehandlung** — Der TRX-Scan filtert nur
  `UnitTestResult[@outcome='Failed']` (Z. 75). TRX-Outcomes `Error` und
  `Timeout` (Adapter-/Testhost-Fehler, Fixture-/Dispose-Abbrüche —
  genau die Klasse von Flakes, die der Job einfangen soll) werden nicht
  in den Re-Run aufgenommen und fehlen damit auch in der
  Flake-Auswertung.

  Empfehlung: XPath auf `@outcome='Failed' or @outcome='Error' or
  @outcome='Timeout'` erweitern.

### TempDirectoryCleanup.cs

- **Inkonsistente Dokumentation** — Klassen-/Methodendoku nennt das
  Backoff „200/500/1000 ms" (Z. 7–8, 25–26); mit dem Standard
  `maxAttempts = 3` wird `Backoff[2]` (1000 ms) nie erreicht — die
  dritte Stufe greift erst bei `maxAttempts ≥ 4`.

  Empfehlung: Entweder die Doku auf die mit drei Versuchen wirksamen
  Stufen (200/500 ms) einschränken oder `maxAttempts`-Standard auf 4
  erhöhen.

## Geprüfte Dateien

Liste aller geprüften Dateien:

- `.github/workflows/pr-checks.yml`
- `VideoPlayer.E2E.Tests/Support/AppInstance.cs`
- `VideoPlayer.E2E.Tests/Support/TempDirectoryCleanup.cs` (neu)
- `VideoPlayer.E2E.Tests/Tests/E2ETestBase.cs` und alle 19 E2E-Testklassen
  (`BootstrapTests.cs`, `ContinueWatchingContextMenuTests.cs`,
  `ContinueWatchingPlaybackTests.cs`, `DownloadsTests.cs`,
  `LogProtocolTests.cs`, `LoginTests.cs`, `PairingTests.cs`,
  `PlaybackTests.cs`, `PlaylistManagementTests.cs`,
  `PlaylistPlaybackTests.cs`, `PlaylistProgressReportTests.cs`,
  `PlaylistsTests.cs`, `SeasonalGenresTests.cs`, `SeriesDetailsTests.cs`,
  `ServerSetupTests.cs`, `ServerSwitchTests.cs`, `SourceBrowsingTests.cs`,
  `SourcePagingTests.cs`, `SourceSearchTests.cs`)
- `VideoPlayer.Maui.Tests/OsInterfaceConventionTests.cs` (neu)
- `VideoPlayer.Maui.Tests/UdpServerDiscoveryTests.cs` sowie alle
  24 klassenmarkierten Testdateien (`AppLogServiceTests.cs`,
  `AppStorageLayoutTests.cs`, `AutoDownloadServiceTests.cs`,
  `ContinueWatchingMutationServiceTests.cs`, `DevApiHttpServerTests.cs`,
  `DevApiMetadataTests.cs`, `DevApiServerTests_Metadata.cs`,
  `DownloadServiceReconcileTests.cs`, `DownloadServiceTests.cs`,
  `DownloadServiceTests_Expiry.cs`, `FavoritesServiceTests.cs`,
  `FileLoggerTests.cs`, `HomeListServiceTests.cs`,
  `IosDeploymentResponseFileTests.cs`, `KnownServersServiceTests.cs`,
  `LocalDatabaseTests.cs`, `MediaDetailsServiceTests.cs`,
  `OfflineContinueWatchingServiceTests.cs`,
  `PendingProgressSyncServiceTests.cs`, `PlaybackSourceServiceTests.cs`,
  `ProgressReportServiceTests.cs`, `SeasonalGenreTests.cs`,
  `ServerSyncServiceTests.cs`, `UpcomingEpisodesServiceTests.cs`)
- `scripts/Retry-FlakyTests.ps1` (neu)
- `scripts/Analyze-FlakyTestResults.ps1` (neu)
- `docs/help/stabilitaet/os-interface-tests.md` (neu),
  `docs/help/e2e-tests.md`, `docs/help/index.md`
- Lifecycle-Artefakte unter `docs/features/task-issue-97-…/`
  (nur Konsistenzsichtung)

## Verifikation

- `dotnet build` beider Testprojekte (Debug): 0 Fehler, 0 Warnungen —
  Trait-Platzierung inkl. `#if DEBUG || DEV_API`-Gates kompilierbar;
  CS1591-XML-Doku-Pflicht eingehalten.
- `OsInterfaceConventionTests` (Debug): 2/2 grün.
- Smoke-Test `Retry-FlakyTests.ps1` gegen synthetische TRX: FQN-
  Auflösung und Filter-Komposition `(Category=OsInterface)&
  (FullyQualifiedName~…)` funktionieren, Exit-Code 0.
- Smoke-Test `Analyze-FlakyTestResults.ps1` (lokaler
  `-ArtifactDirectory`-Modus): Flake-Erkennung main-failed ∩
  rerun-passed funktioniert, Warnung + Summary korrekt.
- `pwsh`-Parser: beide Skripte fehlerfrei; YAML parst sauber,
  Job-Permissions (`contents:read`, `actions:read`, `issues:write`)
  und `continue-on-error` korrekt gesetzt.
- Artefakt-Layout konsistent: `upload-artifact` legt `os-main/` +
  `os-rerun/` an die Artefakt-Wurzel (LCA `TestResults`), deckt sich
  mit `Get-RunFlakes`-Erwartung.
