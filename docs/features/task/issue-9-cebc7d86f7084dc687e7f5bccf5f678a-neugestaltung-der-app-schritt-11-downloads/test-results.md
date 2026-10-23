# Testergebnisse

## Zusammenfassung

|| Lauf | Ergebnis |
||------|----------|
|| Baseline (vor der Umsetzung) | **510 Tests bestanden, 0 Fehler, 0 übersprungen** — Inventar-Lauf (`inventory/test-results/baseline-dotnet-test.log`) |
|| Finaler Testlauf | **580 Tests bestanden, 0 Fehler, 0 übersprungen** — `final-dotnet-test.log` |
|| Build `VideoPlayer.Maui` (net10.0-windows10.0.19041.0) | **0 Warnungen, 0 Fehler** |

## Ausgeführte Kommandos

```text
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
```

Ergebniszeile:

```text
Bestanden!   : Fehler: 0, erfolgreich: 580, übersprungen: 0, gesamt: 580
```

```text
dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj -f net10.0-windows10.0.19041.0
```

Ergebnis: `0 Warnung(en), 0 Fehler`.

## Neue Tests (70 gegenüber der Baseline)

### `DownloadServiceTests` (23 Tests)

- `Enqueue_Completes_AndWritesFileAndManifest` — Datei im
  no-backup-`VideosDirectory`, Manifest `completed`, Fortschritts-
  Ereignisse.
- `Enqueue_CompletedExisting_ReturnsAlreadyPresent` — Dedup ohne
  erneuten Request.
- `Enqueue_QueuedExisting_ReturnsAlreadyQueued` — wartender Auftrag
  nicht doppelt.
- `Enqueue_TwoDownloads_RunInParallel` — Gate-Nachweis zweier
  gleichzeitiger Transfers (`MaxParallelDownloads`).
- `Enqueue_QuotaExhausted_FailsWithQuotaError` — Vorab-Ablehnung mit
  Hinweis und `quota_exceeded`.
- `Enqueue_QuotaExceededDuringTransfer_FailsAndRemovesPartial` —
  Mid-Write-Überschreitung, Teildatei gelöscht.
- `Cancel_RunningDownload_MarksCancelled` — laufender Transfer
  → `cancelled`, Teildatei weg.
- `Cancel_QueuedDownload_MarksCancelled` — wartender Auftrag
  → `cancelled`.
- `Enqueue_WifiOnlyWithoutWifi_StaysQueued` — WLAN-Bedingung
  blockiert den Start ohne Request.
- `Resume_WifiOnly_WifiAvailable_StartsDownload` — WLAN-Rückkehr
  startet wartende Aufträge.
- `Resume_OrphanedDownloadingEntry_RestartsDownload` — App-Neustart-
  Simulation: `downloading` ohne Transfer → erneut → `completed`
  (Stream-URL + Token geprüft).
- `Resume_CancelledEntry_StaysUntouched` — Abgebrochene werden nicht
  wiederaufgenommen.
- `Resume_QuotaFailedEntry_StaysUntouched` — Quota-Fehlschläge bleiben.
- `Enqueue_ServerError_FailsWithHttpError` — `http_500` + `failed`.
- `Enqueue_TransportError_FailsWithNetworkError` — `network`.
- `Delete_Completed_RemovesFileAndEntry` — Datei + Zeile weg,
  `ItemRemoved`.
- `Delete_RunningDownload_RemovesEntry` — Abbruch + Entfernen.
- `DeleteIfCompleted_QueuedEntry_StaysUntouched` — nur `completed`.
- `Enqueue_FailedEntry_Retries` — fehlgeschlagener Auftrag → erneut.
- `Enqueue_ReportsProgressBytes` — `DownloadedBytes`/`TotalBytes`/
  `Progress`.
- `Enqueue_WithoutLogin_FailsNoConnection` — `no_connection`.
- `Enqueue_InvalidMediaType_Rejected` — Guard.
- `LoadItems_ReturnsManifestOrder` — Auftragsreihenfolge.

### `AutoDownloadServiceTests` (8 Tests)

- `Run_MovieCandidate_EnqueuesDirectly` — Film ohne Detail-Roundtrip,
  Stream-Request mit Token.
- `Run_EpisodeCandidate_EnqueuesUpcomingEpisodes` — Serienkontext,
  `PreloadEpisodeCount` Folge-Episoden, Titel „Serie – Episode".
- `Run_Disabled_DoesNothing` — Schalter aus.
- `Run_WifiOnlyWithoutWifi_DoesNothing` — WLAN-only ohne WLAN.
- `Run_NotLoggedIn_DoesNothing` — ohne Client.
- `Run_ExistingManifestEntry_Deduplicates` — keine Doppelaufträge.
- `Run_Throttled_UnlessForced` — 60-s-Throttle, `force` umgeht.
- `Run_EpisodeWithoutSeason_SkipsCandidate` — fehlender Serienkontext
  → übersprungen.

### `AppUpdateTests_Downloads` (31 Tests)

- Seitenfluss `DownloadsPageOpened` → `LoadDownloadsEffect` →
  `DownloadsLoaded`; Lade-/Fehlerzustände.
- `DownloadRequestedMessage`: Guard (ungültige Id/Typen),
  Offline-Hinweis auf der Detailseite, Effekt bei `Connected`.
- `DownloadEnqueueCompleted`: Item-Upsert + `DownloadNotice` nur bei
  passender Detail-Identität.
- `DownloadItemChanged`/`DownloadDeleted`: Upsert/Entfernen in
  `Downloads.Items`.
- Cancel/Retry/Delete-Guards (`CanCancel`/`CanRetry`/`CanDelete`).
- `EnterConnected` und Online-Rückkehr → `ResumeDownloadsEffect`.
- Sync-Abschluss/Cache-Änderung/Settings-Einschalten →
  `RunAutoDownloadsEffect` (nur `Connected` + Schalter).
- Watched-Übergang/Ende → `DeleteWatchedDownloadEffect` (nur Episode
  + Schalter).

### `DownloadsContractTests` (8 Tests)

- `DownloadsPage`: Listen-Bindung, Status-/Fortschritts-Anzeigen,
  Quota/Offline/Lade-/Fehler-/Leerzustand, Aktionen, Dispatches.
- `MediaDetailsPage`: Download-Schalter + Dispatches.
- `MauiProgram`: DI-Registrierung beider Dienste.
- `ShellAppEffectHandler`: Effekt-Verdrahtung + Event-Subscriptions.
- `ConnectivityNetworkStatusMonitor`: WLAN-Profil-Erkennung.
- `AppStorageLayout`: Downloads im no-backup-Bereich.

### Angepasste Bestandstests

- `AppUpdateTests_Connection`: `EnterConnected` erzeugt jetzt vier
  Effekte (+ `ResumeDownloadsEffect` vor der Navigation).
- `MvuHostTests`: Effektfolge um `ResumeDownloadsEffect` erweitert.
- `ProgressReportServiceTests.FakeNetworkMonitor`: `IsWifi` ergänzt.
- `StubHttpMessageHandler`: threadsichere Queue/Requests +
  `EnqueueAsyncResponder` (Token-durchgereicht) für Abbruch-Tests.

## Hinweise

- UI-E2E-Automatisierung existiert im Repository nicht
  (dokumentierte Einschränkung); die Benutzerflüsse sind über
  MVU-Übergangstests, Service-Tests mit echter SQLite-Ablage und
  XAML-Contract-Tests abgesichert (siehe `plan.md`,
  „Testnachweise der Benutzerflüsse").
- `IsWifi` auf Ethernet: durch die Schnittstellen-Dokumentation und
  die Fakes der Service-Tests abgesichert; plattformspezifisches
  Verhalten (`ConnectionProfiles`) ist am App-Rand nicht
  unit-testbar — dokumentierte Einschränkung.
