# Tests — Bestand und Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-17, lokale Zeit (Europe/Berlin, CEST)
- Branch und Commit-ID des Laufs: `task/issue-51-…-download-optimieren`
  (Basisbranch), `2a7bcf240876090db99d8d83a5b8cadc8d25f0f0` — identischer
  Codebestand wie der Schritt-Branch (nur Docs-Commits dazwischen)
- Uncommittete Änderungen im getesteten Stand: nur neue Artefakte unter
  `docs/projects/…` (kein Produktivcode)
- Testumgebung: Windows, .NET SDK 10.0.402, Ziel `net10.0`

### Testläufe

| Lauf | Befehl | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --nologo` | Repo-Wurzel | 0 | 1335 | 0 | 0 | `docs/projects/task/issue-51-…/inventory/test-results/baseline-unittests.log` |

### Nachgewiesene bestehende Testfehler

Keine — 0 Fehlschläge im Baseline-Lauf.

### Testlücken und Ausführungsprobleme

- `VideoPlayer.E2E.Tests` (FlaUI, Windows) nicht ausgeführt — kein
  Downloads-E2E-Test vorhanden; iOS-Verhalten in dieser Umgebung nicht
  ausführbar.

## Testklassen (betroffen)

- `DownloadServiceTests` (27 Tests): Enqueue/Dedup/Quota/Cancel/Resume/
  Fehlerpfade; injizierbarer Service-Konstruktor, Mock-HTTP, Temp-Verzeichnis.
- `DownloadListSyncTests` (10), `DownloadsContractTests` (9),
  `AutoDownloadServiceTests` (16), `AppUpdateTests_Downloads` (31).

## Hilfsmethoden

- `DownloadService`-Testkonstruktor: `httpClientFactory`, `fileExists`,
  `quotaBytesOverride` — für Abgleichstests ggf. um Dateisystem-Abstraktion
  erweitern oder reales Temp-Verzeichnis nutzen (bestehende Tests schreiben
  echte Dateien in Temp-Pfade).
