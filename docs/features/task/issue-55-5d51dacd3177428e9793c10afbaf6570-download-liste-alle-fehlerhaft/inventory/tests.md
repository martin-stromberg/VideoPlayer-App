# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt: 2026-10-18 12:23 UTC
- Branch und Commit-ID: `task/issue-55-5d51dacd3177428e9793c10afbaf6570-download-liste-alle-fehlerhaft`, `14ca0b3afd8b8d6ad1e8b3bf3545eb88aea2555c`
- Uncommittete Änderungen im getesteten Stand: nur Lifecycle-Artefakte unter `docs/features/task/issue-55-…/` (keine Codeänderungen)
- Testumgebung: Windows, .NET SDK 10.0.402, Ziel-Framework `net10.0`
- Ermittelte Testsuiten: `VideoWebPlayer.Maui.Tests` (xUnit; einzige in CI ausgeführte Suite — `pr-checks.yml` führt `dotnet test VideoWebPlayer.Maui.Tests` aus). `VideoPlayer.E2E.Tests` (FlaUI) wird in `pr-checks.yml` nicht ausgeführt und benötigt eine laufende App-Instanz.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release` | Repo-Stamm | 0 | 1326 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — der Ausgangslauf ist fehlerfrei.

### Testlücken und Ausführungsprobleme

- `VideoPlayer.E2E.Tests` nicht ausgeführt: erfordert eine deployte/laufende MAUI-App (FlaUI), wird auch in `pr-checks.yml` nicht ausgeführt.
- Build-Warnungen CS8618 in `VideoWebPlayer.Client` (Nullable-Kontext, bestehender Stand — nicht testrelevant).

## Testklassen

### `DownloadServiceTests`
Datei: `VideoWebPlayer.Maui.Tests/DownloadServiceTests.cs` — `[Collection("SqliteDatabase")]`, echte SQLite-Ablage + Temp-Verzeichnisse, `StubHttpMessageHandler`, `FakeNetworkMonitor`.

- `Delete_Completed_RemovesFileAndEntry` — Löschen entfernt Datei + Manifest-Zeile, `ItemRemoved` wird gemeldet.
- `Delete_RunningDownload_RemovesEntry` — laufender Download wird abgebrochen und entfernt.
- `DeleteIfCompleted_QueuedEntry_StaysUntouched` — gefiltertes Löschen lässt andere Zustände unberührt.
- `Resume_CancelledEntry_StaysUntouched` / `Resume_QuotaFailedEntry_StaysUntouched` — Zustandsfilter-Muster in der Wiederaufnahme.
- `Enqueue_FailedEntry_Retries` — `failed`-Einträge sind erneut einplanbar.
- `Enqueue_QuotaExceededDuringTransfer_FailsAndRemovesPartial` — `failed`-Eintrag ohne Datei-Reste.
- `LoadItems_ReturnsManifestOrder` — Manifest-Ablesereihenfolge.

### `AppUpdateTests_Downloads`
Datei: `VideoWebPlayer.Maui.Tests\AppUpdateTests_Downloads.cs` — MVU-Transitions (pure, ohne Infrastruktur).

- `DownloadsDeleteAll_WithItems_EmitsEffect` / `DownloadsDeleteAll_Empty_NoEffect` — Muster für Sammelaktion-Effekt + No-Op-Bedingung.
- `DownloadsPurgeOrphans_WithOrphans_EmitsEffect` / `DownloadsPurgeOrphans_None_NoEffect` — zweites Muster (Restdaten).
- `DownloadDeleteRequested_EmitsDeleteEffect` — Einzel-Löschung.
- `DownloadRetryRequested_Failed_EnqueuesAgain` — `Failed`/`Cancelled` ist die Retry-Menge.

### `DownloadsContractTests`
Datei: `VideoWebPlayer.Maui.Tests\DownloadsContractTests.cs` — Quelltext-Verträge (MAUI-Schicht nicht instanziierbar).

- `DownloadsPage_DispatchesMessages` — erwartete Messages im Code-Behind.
- `ShellAppEffectHandler_WiresDownloadEffects` — alle Download-Effekte im Switch verdrahtet.
- `DownloadsPage_HasCancelRetryDeleteActions` / `DownloadsPage_ShowsItems_WithStatusAndProgress` — XAML-Struktur.

### `DownloadServiceReconcileTests`, `DownloadListSyncTests`, `AutoDownloadServiceTests`
Angrenzende Suites (Abgleich, Listen-Sync, Auto-Downloads) — nicht direkt betroffen.

## Hilfsmethoden

### `DownloadServiceTests`
- `CreateService(quotaBytesOverride)` — Dienst über Temp-Layout + Stub-HTTP.
- `LoginAsync` — Authentifizierung gegen den Stub.
- `EnqueueStream(size)` — Stub-Antwort mit Bytestrom.
- `ReadEntryAsync(type, mediaId)` — Manifest-Zeile lesen.
- `WaitForStateAsync(read, state, timeoutMs)` — auf persistierten Zustand warten.
- Direkte `cache.Connection.InsertAsync(new DownloadManifestEntry { State = "failed" … })`-Vorbelegung (Muster der Resume-Tests) — geeignet zum Aufbau von `failed`/`cancelled`-Fixtures ohne Transfer.
