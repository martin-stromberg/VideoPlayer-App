# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| `OsInterface`-Trait auf allen umgebungssensitiven Tests (`Maui.Tests` + `E2E.Tests`) | Schritte 1–2; vollständige 24-Klassen-Liste + `UdpServerDiscoveryTests` methodenweise + 19 E2E-Klassen/`E2ETestBase` — deckt sich 1:1 mit dem Audit (Grep-Verifikation: exakt die 25 Dateien mit `GetTempPath`/`Process.Start`/`UdpClient`/`TcpListener`/`Sockets`) | Meta-Test Regel 2 (`E2eTestClasses_CarryOsInterfaceTrait`); Schritt 8 lokale Filter-Verifikation mit dokumentierten Ergebniszahlen | Abgedeckt |
| Blockierende `verify`-Lane filtert `Category!=OsInterface` | `pr-checks.yml`: Test-Schritt um `--filter "Category!=OsInterface"` erweitert (Z. 105) | Szenario 1 (`verify` grün, nur nicht-umgebungssensitive Tests) + lokaler Lauf Schritt 8 | Abgedeckt |
| Neue Best-Effort-Lane `test-os-interface` (`continue-on-error: true`, `windows-latest`, großzügiges Timeout) | Neuer Job mit `continue-on-error: true`, `timeout-minutes: 30`, eigenen `permissions` (Z. 107) | Szenarien 1–2 | Abgedeckt |
| TRX-Logger, `--results-directory`, Artefakt-Upload in beiden Lanes | `verify`: `--logger "trx"` + `verify-trx`-Upload (`if: always()`); `test-os-interface`: `os-interface-trx` | Szenario 1 (Artefakt-Listing als Nachweis) | Abgedeckt |
| Genau ein automatisierter Re-Run fehlgeschlagener Tests, kein Blanket-Retry | `scripts/Retry-FlakyTests.ps1` (TRX-Parsing, FQN-Filter, kein Re-Run bei 0 Fehlschlägen); Ausschluss 3 | Szenario 2 (Flake → Re-Run grün, Job bleibt non-blocking) | Abgedeckt |
| TRX-basierte Flake-Statistik + definierter Schwellwert → Quarantäne-Issues | `scripts/Analyze-FlakyTestResults.ps1` (`-RunLimit 10`, `-FlakeThreshold 2`, Label `flaky-test`, Dedupe via `gh issue list`) | Szenario 3 (`::warning`, Summary, dedupliziertes Issue) | Abgedeckt |
| Poll-Deadlines konsistent gestaffelt (Host > Kind) | Gemeinsames 45-s-Deadline-Budget in `StartAsync` über `WaitForDevApiPortAsync`/`WaitForMainWindow`/`WaitForModelAsync` (Restbudget); `UiDriver` 15 s / `DevApiClient` 20 s bewusst unverändert — verifiziert am Code: 45 s Z. 80, 30-s-Eigenbudget Z. 403, 20 s `DevApiClient` Z. 110, 15 s `UiDriver` Z. 41 | Szenario 6 + indirekter Nachweis über gehärtete E2E-Kette | Abgedeckt (Hinweis: Shutdown-Wartezeiten `WaitForExit(10_000)`/5-s-Kill/5-s-Drain ohne dokumentiertes Prüf-Fazit — siehe Hinweise) |
| `DeleteProfileDirectory` mit Retry/Backoff + Protokollierung | Neuer `TempDirectoryCleanup.DeleteWithRetry` (3×, Backoff 200/500/1000 ms, `IOException`/`UnauthorizedAccessException`), `TryDelete` mit Log-Callback — verifiziert: Ist-`catch (Exception) {}` Z. 329–332 / Z. 425 | Szenario 6 (protokollierte Restfehler statt kommentarlos) | Abgedeckt |
| `Flaky`-Konvention nur mit `OsInterface`, per Meta-Test abgesichert | `[Trait("Flaky","true")]` orthogonaler Traitname (begründete Designentscheidung gegen `!=`-Mehrdeutigkeit); `OsInterfaceConventionTests` in `Maui.Tests` ohne Trait | `FlakyTrait_RequiresOsInterface` (quelltextbasiert via `Stubs/RepositoryRoot.Find()` — Datei verifiziert vorhanden) | Abgedeckt |
| Hilfeseite + `index.md`-Eintrag + `e2e-tests.md`-CI-Hinweis | `docs/help/stabilitaet/os-interface-tests.md` neu (Lane-Regeln, Trait-/Flaky-Konvention, `gh run rerun --job` nur OS-Job, Quarantäne, Debug-Gating-Hinweis); `index.md`/`e2e-tests.md` angepasst inkl. Korrektur der veralteten Zählung | Schritt 9 + Szenario 4 dokumentiert lokale Debug-Filtered-Runs | Abgedeckt |
| Nachweis ≥ 3 komplette grüne CI-Durchläufe | Schritt 10 (PR-Läufe beobachten, dokumentieren) | `gh pr checks`/`gh run view` als Nachweisweg | Abgedeckt |
| Scope-Ausschlüsse eingehalten (kein App-Laufzeitcode, kein Blanket-Retry, keine ConPTY-/Junction-Heuristiken) | Ausschlüsse 1–4 jeweils mit Begründung + Status `bestätigt`; Änderungsliste berührt nur Tests/Workflow/Skripte/Doku | Meta-Test + Szenarien; `#if`-Gating der DevApi-Tests als Ausschluss 2 dokumentiert (verifiziert: Zeile 1–3 aller drei Dateien = `#if DEBUG \|\| DEV_API`) | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

—

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Anwenderseitige UI-Flüsse | Keine — die Anforderung betrifft ausschließlich Test-Infrastruktur, CI-Jobs, Skripte und Doku; kein App-Laufzeitcode, kein UI-Flow. Plan begründet das ausdrücklich (Abschnitt „E2E-Tests (primärer Funktionsnachweis)") | Nicht erforderlich mit Begründung |
| CI-Fluss (PR → beide Lanes → Artefakte/Quarantäne) | „Fluss" ist der CI-Lauf selbst: Akzeptanzszenarien 1–3 mit konkretem Nachweis (`gh pr checks`, `gh run view`, Artefakt-Listing, Step-Summary) + Schritt 10 (3 grüne Durchläufe) | Abgedeckt |
| Bestehende E2E-Suite (lokal) | Kein inhaltlicher Umbau nötig; einmaliger lokaler Volllauf als Härtungsnachweis eingeplant (`dotnet test VideoPlayer.E2E.Tests -c Debug`, interaktive Session) | Abgedeckt |

## Fehlende oder unvollständige Planbestandteile

—

## Hinweise

- **Shutdown-Deadline-Prüfung dokumentieren:** Die Anforderung nennt unter „Konkret prüfen" explizit auch `WaitForExit(10_000)` beim Shutdown. Der Plan trifft dazu (sowie zur 5-s-Kill-Wartezeit `TryKillAndWait` und dem 5-s-stdout-Drain in `WriteProcessDiagnosticsCore`) keine Festlegung. Da die Shutdown-Phasen sequentielle Fallbacks ohne Host/Kind-Verschachtelung sind, dürfte das Prüfergebnis „unverändert belassen" lauten — es sollte im Umsetzungs-PR explizit festgehalten werden. Ebenso könnte ein Satz zu den Testdatei-Übersteuerungen (bis 120 s) die Staffelungskonvention abrunden (Override-Budgets sind bewusst testlokal und größer als die 15-/20-s-Helfer-Defaults).
- **`TestResults/` nicht in `.gitignore`:** Nur `*.trx` ist ignoriert (Zeile 67); lokale Läufe mit `--results-directory TestResults/…` können weitere Dateien (z. B. Attachments) als Untracked hinterlassen. Optional `.gitignore` um `TestResults/` ergänzen.
- **Schritt-8-Erwartungszahlen:** Die Debug-Erwartung ist bewusst weich formuliert (`DevApiMetadataTests` zählt „ggf." dazu) — da der Plan `DevApiMetadataTests` klassenweise markiert, sollte die tatsächliche Zahl (5 + 3 + N) beim Verifikationslauf konkret dokumentiert werden.
- **Verifikationsbefund dieses Checks:** `dotnet test --filter "Category=OsInterface"` wurde auf dem bestehenden Release-Build ausgeführt — Filter-Schlüssel wird vom xUnit-v3/VSTest-Adapter korrekt ausgewertet (0 Treffer, Exit 0). `WaitForMainWindow` wird im `AppInstance`-Konstruktor (Z. 45) aufgerufen, nicht direkt in `StartAsync`; die Restbudget-Weitergabe muss daher durch den Konstruktor gereicht werden — Implementierungsdetail, kein Planwiderspruch.
