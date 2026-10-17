# Tests — Bestand und Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-17, lokale Zeit (Europe/Berlin, CEST)
- Branch und Commit-ID: `task/issue-51-5e323baba37842bf8094a24f94c9013b-download-optimieren`, `2a7bcf240876090db99d8d83a5b8cadc8d25f0f0`
- Uncommittete Änderungen im getesteten Stand: nur neue Planungsartefakte
  unter `docs/projects/task/issue-51-…/ ` (kein Produktivcode, keine Tests)
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.402,
  Testziel `net10.0`
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `VideoWebPlayer.Maui.Tests` (`dotnet test`, net10.0) — laut README
    der projektübliche Testbefehl
  - `VideoPlayer.E2E.Tests` (FlaUI, net10.0-windows) — siehe
    „Testlücken und Ausführungsprobleme"

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --nologo` | Repo-Wurzel | 0 | 1335 | 0 | 0 | [baseline-unittests.log](test-results/baseline-unittests.log) |

### Nachgewiesene bestehende Testfehler

Keine — der Baseline-Lauf meldet 0 fehlgeschlagene Tests.

### Testlücken und Ausführungsprobleme

- `VideoPlayer.E2E.Tests` wurde nicht ausgeführt: FlaUI-Tests gegen die
  gestartete Windows-App mit lokalem Mock-Server; für die iOS-Fragestellung
  nicht anwendbar (iOS-Plattform kann in dieser Windows-Umgebung weder
  gebaut noch ausgeführt werden). Es existiert kein Downloads-E2E-Test.
- iOS-spezifisches Verhalten (Hintergrund-Transfers) ist in dieser
  Umgebung nicht ausführbar — der Nachweis muss über plattformneutrale
  Tests und Prüfung des iOS-spezifischen Codes erfolgen.

## Testklassen (Download-Bereich)

### `DownloadServiceTests`
Datei: `VideoWebPlayer.Maui.Tests/DownloadServiceTests.cs` (27 Facts/Theories)

- `Enqueue_Completes_AndWritesFileAndManifest` — kompletter Transfer mit Manifest- und Dateischreiben
- `EnqueueAsync_TransferOutcome_Logged`, `EnqueueAsync_DedupNotice_LogsBelowInformation`, `EnqueueAsync_QuotaExceeded_LogsSingleWarning` — Protokollierung
- `Enqueue_WithSeriesContext_PersistsContextInManifest`
- `Enqueue_CompletedExisting_ReturnsAlreadyPresent`, `Enqueue_QueuedExisting_ReturnsAlreadyQueued`
- `Enqueue_TwoDownloads_RunInParallel`
- `Enqueue_QuotaExhausted_FailsWithQuotaError`, `Enqueue_QuotaExceededDuringTransfer_FailsAndRemovesPartial`
- `Cancel_RunningDownload_MarksCancelled`, `Cancel_QueuedDownload_MarksCancelled`
- `Enqueue_WifiOnlyWithoutWifi_StaysQueued`, `Resume_WifiOnly_WifiAvailable_StartsDownload`
- `Resume_OrphanedDownloadingEntry_RestartsDownload`, `Resume_CancelledEntry_StaysUntouched`, `Resume_QuotaFailedEntry_StaysUntouched`
- `Enqueue_ServerError_FailsWithHttpError`, `Enqueue_TransportError_FailsWithNetworkError` u. a. Fehlerpfade, Delete-/Dedup-Fälle

Hilfsmittel: injizierbarer `DownloadService`-Konstruktor (`httpClientFactory`,
`fileExists`, `quotaBytesOverride`), Mock-HTTP-Handler, Temp-Verzeichnisse.

### `DownloadListSyncTests`
Datei: `VideoWebPlayer.Maui.Tests/DownloadListSyncTests.cs` (10 Facts)

In-place-Updates, Insert/Move/Remove der gebundenen Liste.

### `DownloadsContractTests`
Datei: `VideoWebPlayer.Maui.Tests/DownloadsContractTests.cs` (9 Facts)

Anzeige-/Verhaltensverträge der `DownloadListItem`-Ableitungen.

### `AutoDownloadServiceTests`
Datei: `VideoWebPlayer.Maui.Tests/AutoDownloadServiceTests.cs` (16 Facts)

Vorlauf-Logik (Episoden-Auswahl, Drossel, WLAN-Bedingung, Playlist-Bezug).

### `AppUpdateTests_Downloads`
Datei: `VideoWebPlayer.Maui.Tests/AppUpdateTests_Downloads.cs` (31 Facts)

MVU-Verhalten der Download-Nachrichten/Effekte (Seiten-Laden, Enqueue-Notice,
ItemChanged-Upsert, Löschen, Quota-Text usw.).
