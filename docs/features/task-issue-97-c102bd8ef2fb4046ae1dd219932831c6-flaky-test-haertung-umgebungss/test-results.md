# Test-Ergebnisse

Stand: 2026-11-22, Branch `task/issue-97-c102bd8ef2fb4046ae1dd219932831c6-flaky-test-haertung-umgebungss`.
Alle Läufe lokal auf Windows (interaktive Session, SessionId 1, `[Environment]::UserInteractive = True`), .NET SDK 11.0.100, Ziel-Framework net10.0.

## Ergebnis

**Status:** Keine Fehler

## Lane-Verifikation (CI-Struktur real ausgeführt)

| # | Lauf | Befehl | Ergebnis | Dauer | TRX/Beleg |
|---|------|--------|----------|-------|-----------|
| 1 | Blockierende `verify`-Lane | `dotnet test VideoPlayer.Maui.Tests -c Release --filter "Category!=OsInterface"` | **1223/1223 bestanden** | ~1,5 s | `TestResults/verify/*.trx` |
| 2 | Best-Effort-Lane `test-os-interface` | `dotnet test VideoPlayer.Maui.Tests -c Release --filter "Category=OsInterface"` | **263/263 bestanden** | ~56 s | `TestResults/os-main/*.trx` |
| 3 | DEBUG-gegatete DevApi-Tests | `dotnet test VideoPlayer.Maui.Tests -c Debug --filter "Category=OsInterface&FullyQualifiedName~DevApi"` | **10/10 bestanden** (6 `DevApiHttpServerTests` + 3 `DevApiServerTests_Metadata` + 1 `DevApiMetadataTests`) | ~1,1 s | Konsolenlog |
| 4 | Lint (CI-Äquivalent) | `dotnet format whitespace --verify-no-changes --no-restore` | **Clean, Exit 0** (nur Workspace-Warnungen) | — | — |
| 5 | Gesamtsuite + Coverage | `dotnet test VideoPlayer.Maui.Tests -c Release --collect:"XPlat Code Coverage"` | **1486/1486 bestanden** (1223 + 263 = 1486, keine Überlappung) | ~49 s | `TestResults/coverage/*/coverage.cobertura.xml` |

Die Lane-Summen decken sich exakt mit der Gesamtsuite: 1223 + 263 = 1486 — die Filterung trennt den Bestand vollständig und ohne Lücke.

## Skript-Verifikation (synthetische TRX, `Set-StrictMode -Version Latest`)

`scripts/Retry-FlakyTests.ps1` und `scripts/Analyze-FlakyTestResults.ps1` (beide binden `scripts/FlakyTestTrx.ps1` per Dot-Sourcing ein) wurden gegen synthetische TRX-Dateien und — für den Re-Run-Pfad — gegen echte OsInterface-Tests getestet. **15/15 Checks bestanden** + 1 Zusatzcheck:

| Check | Ergebnis |
|-------|----------|
| Retry: fehlendes `-TrxDirectory` → Exit 1 (Skript-Missbrauch) | Bestanden |
| Retry: Verzeichnis ohne TRX → Exit 0, „kein Re-Run" | Bestanden |
| Retry: TRX mit nur `Passed`/`NotExecuted` → Exit 0, „Keine fehlgeschlagenen Tests" | Bestanden |
| Retry: 1 Fehlschlag (Outcome `Failed`) → echter `dotnet test`-Re-Run, Re-Run-TRX erzeugt, Test `outcome="Passed"` | Bestanden |
| Retry: mehrere Fehlschläge (`Failed`+`Error`+`Timeout`) → „Re-Run von 3 fehlgeschlagenen Tests"; `NotExecuted` löst **keinen** Re-Run aus | Bestanden |
| Analyze: 7 synthetische Läufe (os-main/os-rerun) → exakt 2 Flakes erkannt, Tabelle `\| Suite.A.FlakyTest \| 2 \|` | Bestanden |
| Analyze: Re-Run-Outcome `Failed` → kein Flake; fehlendes os-rerun-Verzeichnis → kein Flake; `NotExecuted` im Re-Run → kein Flake | Bestanden |
| Analyze: leeres `os-main`-Verzeichnis → kein Fehler/Absturz, Lauf wird ausgewertet | Bestanden |
| Analyze: `::warning::Quarantaene-Kandidat` ab Schwellwert 2 ausgegeben | Bestanden |
| Analyze: ohne `-ArtifactDirectory` und ohne `GH_TOKEN` → Exit 0, „keine Auswertung" | Bestanden (Zusatzcheck) |

Smoke-Artefakte: `%TEMP%\flaky-smoke-b40cc4dbe7b344dd90ef73b126b98487` (Harness: `%TEMP%\flaky-smoke.ps1`).

## E2E-Suite (Härtungsnachweis `AppInstance`-Deadline-Budget + `TempDirectoryCleanup`)

Interaktive Desktop-Session verfügbar (`query session`: `console`, Benutzer Martin, ID 1, Aktiv; `UserInteractive: True`, `SessionId: 1`).

| Lauf | Befehl | Ergebnis | Dauer |
|------|--------|----------|-------|
| Volllauf `VideoPlayer.E2E.Tests` (74 Fälle, FlaUI/UIA3, echte `VideoPlayer.Maui.exe` pro Test) | `dotnet test VideoPlayer.E2E.Tests -c Debug` | **74/74 bestanden, 0 Flakes** | 7,7 min |

Beleg: `TestResults/e2e-run.log`, `TestResults/e2e/e2e-full.trx`. Die Suite baut die App über `AppFixture` selbst (Debug, net10.0-windows10.0.19041.0) und startet sie je Test als echten Prozess — Startup-Deadline-Budget (45 s über `WaitForDevApiPortAsync`/`WaitForMainWindow`/`WaitForModelAsync`) und Retry/Backoff-Profilcleanup (`TempDirectoryCleanup.DeleteWithRetry`) wurden in allen 74 Instanzen real durchlaufen, ohne sichtbare Cleanup-Restfehler oder Startup-Timeouts.

**Umgebungshinweis (kein Produktfehler):** Der erste Versuch schlug beim App-Build mit NETSDK1147 fehl — der lokale Workload-Satz (VS 18.11-Preview) enthält iOS/Android/MacCatalyst nur im net11-Band, nicht für net10.0. Der Lauf wurde mit den im `VideoPlayer.Maui.csproj` vorgesehenen Schaltern `IncludeIosTarget=false`, `IncludeAndroidTarget=false`, `IncludeMacCatalystTarget=false` als Umgebungsvariablen ausgeführt (MSBuild übernimmt Env-Vars als Properties; sie propagieren in den `dotnet build`-Aufruf der `AppFixture`). Keine Code- oder Maschinenänderung erforderlich.

## Reale End-to-End-Abnahme

Die Anforderung ist Test-Infrastruktur/CI-Härtung — kein App-UI-, kein Benutzerfluss. Die reale Abnahme besteht aus dem realen Ausführen der neuen Lane-Struktur und Skripte (s. oben).

| Szenario | Durchgeführt über | Ergebnis |
|----------|-------------------|----------|
| Akzeptanz 1 (Happy Path CI: `verify` gefiltert grün, `test-os-interface` separat) | Reale lokale Läufe der exakten CI-Befehle (Lane 1 + 2, inkl. `--logger trx` + `--results-directory`) | Bestanden (CI-Nachweis auf dem Runner ausstehend, s. unten) |
| Akzeptanz 2 (Flake → Einmal-Re-Run) | `Retry-FlakyTests.ps1` gegen synthetische Hauptlauf-TRX + echter `dotnet test`-Re-Run | Bestanden |
| Akzeptanz 3 (Flake-Statistik + Quarantäne-Hinweis) | `Analyze-FlakyTestResults.ps1` gegen 7 synthetische Artefakt-Läufe | Bestanden (Issue-Erzeugung via `gh` nur im CI-Modus prüfbar — ausstehend) |
| Akzeptanz 4 (lokaler gefilterter Debug-Lauf, DevApi-Gating) | Lane 3 | Bestanden (10/10) |
| Akzeptanz 5 (Konventions-Meta-Test) | `OsInterfaceConventionTests` in Lane 1 enthalten und grün | Bestanden |
| Akzeptanz 6 (E2E-Startup-Budget + Cleanup-Retry) | E2E-Volllauf 74/74 | Bestanden |
| Task 27: ≥3 grüne PR-Durchläufe (`gh pr checks`, Artefakte) | Erst nach Push/PR möglich | **Ausstehend — externer CI-Nachweis**, kein Testfehler |

## Sichtprüfung der Abnahme-Screenshots

**Status:** Nicht erforderlich — die Anforderung enthält keine UI-Änderung und keinen Benutzerfluss; es wurden keine Abnahme-Screenshots erzeugt. Die sichtbare Wirkung liegt ausschließlich in den CI-Lanes/Skriptausgaben (dokumentiert oben).

## Zusammenfassung

- Gesamt (VideoPlayer.Maui.Tests): 1486
- Bestanden: 1486
- Fehlgeschlagen: 0
- Übersprungen: 0
- Zusätzlich E2E: 74/74 bestanden
- Skript-Smoke-Checks: 15/15 + 1

## Testabdeckung

**Abdeckung:** 89,4 % (Zeilenabdeckung, XPlat Code Coverage / Cobertura über die Gesamtsuite `VideoPlayer.Maui.Tests`)

| Datei | Abdeckung |
|-------|-----------|
| VideoPlayer.Mvu/ServerAddressProvider.cs | 0,0 % |
| VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs | 0,0 % |
| VideoPlayer.Mvu/SignalRServerEventsConnection.cs | 0,0 % |
| VideoPlayer.Mvu/ServerHealthProbe.cs | 0,0 % |
| VideoPlayer.ApiClient/Models/MediaSource.cs | 61,5 % |
| VideoPlayer.Mvu/DownloadListItem.cs | 62,3 % |
| VideoPlayer.ApiClient/ClientGate.cs | 63,6 % |
| VideoPlayer.Mvu/PlaylistDetailsState.cs | 65,0 % |
| VideoPlayer.Mvu/ProfilePageState.cs | 70,0 % |
| VideoPlayer.Mvu/DownloadItemViewModel.cs | 72,0 % |
| VideoPlayer.Mvu/MediaTypeNames.cs | 72,2 % |
| VideoPlayer.Mvu/SourceItem.cs | 75,0 % |
| VideoPlayer.ApiClient/Discovery/DiscoveredServer.cs | 75,0 % |
| VideoPlayer.ApiClient/Discovery/ServerDiscoveryService.cs | 77,3 % |
| VideoPlayer.ApiClient/Models/MediaEntry.cs | 78,4 % |
| VideoPlayer.Mvu/ServerEventsService.cs | 79,8 % |

## Fehlende Tests

Quelle: `Coverage-Daten`

- `VideoPlayer.Mvu/ServerAddressProvider.cs` — 0 % Abdeckung
- `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` — 0 % Abdeckung (mDNS/OS-Netzwerk — umgebungssensitiv, nur E2E-nah abdeckbar)
- `VideoPlayer.Mvu/SignalRServerEventsConnection.cs` — 0 % Abdeckung (echte SignalR-Verbindung)
- `VideoPlayer.Mvu/ServerHealthProbe.cs` — 0 % Abdeckung

Einordnung: Die 0-%-Dateien sind umgebungs-/netzwerkgebundene Infrastruktur (mDNS, SignalR, Health-Probe) — kein Scope dieses Issues (Härtung der Test-Infrastruktur, keine neuen fachlichen Tests angefordert). Alle vom Issue neu geschaffenen Artefakte sind entweder selbst Testcode (`OsInterfaceConventionTests`, `TempDirectoryCleanup` in `VideoPlayer.E2E.Tests`) oder per Smoke-Test verifizierte PowerShell-Skripte.
