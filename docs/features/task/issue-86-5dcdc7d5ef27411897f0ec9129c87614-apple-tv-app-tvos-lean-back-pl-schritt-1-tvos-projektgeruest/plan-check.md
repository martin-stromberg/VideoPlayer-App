# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Projekt ist Teil der Lösung und referenziert ausschließlich die plattformneutralen Projekte | Umsetzungsschritte 2 + 4 (csproj mit genau vier `ProjectReference`s; `dotnet sln add`) | `TvOsProject_IsPartOfSolution`, `TvOsProject_ReferencesOnlyPlatformNeutralLibraries` | Abgedeckt |
| Neuer CI-Job baut das tvOS-Projekt auf macOS erfolgreich (Simulator, ohne Codesignierung) | Umsetzungsschritt 6 (`build-tvos` auf `macos-latest`, `tvossimulator-arm64`, `EnableCodeSigning=false`) | `PrChecksWorkflow_ContainsTvOsSimulatorJob` (Job-Vorhandensein/Inhalt); tatsächlicher Build-Nachweis über den CI-Lauf selbst (Windows-seitig nicht ausführbar — begründete Einschränkung) | Abgedeckt |
| Kompatibilitätsprüfung der kritischen Pakete dokumentiert, bei Inkompatibilität mit Ausweichvorschlag | Umsetzungsschritt 1 + Task 8; Ergebnis in `inventory/projects.md`, Dauer-Doku `docs/help/` | Dokumentationsartefakt (Task 12); Zeroconf-Risiko mit UDP-only-Ausweich benannt | Abgedeckt |
| Workload-Pinning analog zum iOS-Pfad | Umsetzungsschritt 6: `global.json`-Band-Pin + `printf`-Rollback-JSON + `workload update --from-rollback-file` + `install tvos --skip-manifest-update`; Version `26.5.10318` gegen nuget.org verifiziert | `PrChecksWorkflow_ContainsTvOsSimulatorJob` prüft Rollback-Vorhandensein | Abgedeckt |
| Bestehende Projekte und Tests bleiben unverändert lauffähig | Designentscheidung `IncludeTvOsTarget` (degradiert zu leerer `net10.0`-Bibliothek unter Windows); Umsetzungsschritt 5 (Windows-Validierung); keine Änderung an bestehenden Quelldateien | Windows-Validierung `dotnet restore`/`test` + `TvOsProject_TargetsTvOsConditionally`; Baseline-Testlauf in `inventory/tests.md` | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| tvOS-App startet und zeigt leere Startansicht | Kein E2E-Test geplant | Nicht erforderlich mit Begründung: keine tvOS-UI-Testinfrastruktur, `net10.0-tvos` auf dem Entwicklungsrechner nicht ausführbar; Funktionsnachweis liegt im macOS-CI-Kompilier-Job (Akzeptanzszenario 1+3). Die Anforderung verlangt ausdrücklich nur eine leere Programmhülle ohne Benutzerfluss. |

## Hinweise

- Die vier Contract-Tests sichern die unter Windows prüfbaren Akzeptanzkriterien
  (Solution-Mitgliedschaft, Referenzmenge, TFM-Schalter, CI-Job-Konfiguration)
  nach dem bestehenden `*ContractTests`-Muster der Suite.
- Restrisiko außerhalb der Planabdeckung: Der reale macOS-Build kann erst im
  CI-Lauf validiert werden; das Pinning ist gegen nuget.org verifiziert
  (`microsoft.net.sdk.tvos.manifest-10.0.100` `26.5.10318`).
