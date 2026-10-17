# Logik

## `DownloadService`
Datei: `VideoPlayer.Mvu/DownloadService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadItemsAsync` | public | Manifest → `DownloadListItem`-Liste |
| `GetUsedBytes` | public | Summe aller Dateien im Video-Verzeichnis (inkl. Waisen) |
| `EnqueueAsync` | public | Einreihen/Dedup/Reaktivieren; `completed && !fileExists` → neu einreihen |
| `CancelAsync` | public | Abbruch wartender/laufender Aufträge; Teildatei wird entfernt |
| `DeleteAsync` | public | Transfer abbrechen + Datei + Manifest-Eintrag entfernen |
| `DeleteIfCompletedAsync` | public | Nur `completed` löschen (gesehene Episoden) |
| `ResumeInterruptedAsync` | public | `queued`/`downloading`/nicht-Quota-`failed` erneut einplanen |
| `PumpQueue`/`PumpQueueAsync` | private | Max. 2 parallele Transfers, WLAN-Gate, Vorab-Quota |
| `TransferAsync` | private | `HttpClient`-Stream → `FileStream` (`FileMode.Create`); Fehlerpfade löschen Teildatei via `TryDeleteFile` |
| `SetStateAsync`/`SetStateIfPresentAsync` | private | Zustand persistieren + `ItemChanged` |
| `FindEntryAsync` | private | Eintrag nach Typ+Id |
| `FilePathFor` | private | Dateiname `{typ}_{id}.mp4` |
| `TryDeleteFile` | private | Datei löschen (Fehler nur geloggt) |
| `GetUsedBytes`-Aufrufer | – | `EnqueueAsync` (Vorab-Quota), `PumpQueueAsync`, `LoadDownloadsAsync` (Anzeige) |

Injizierbar für Tests: `httpClientFactory`, `fileExists`, `quotaBytesOverride`.
Interner Zustand: `runningTransfers` (Dictionary Key→CTS), `manifestGate`,
`pumpGate`, `lastProgressEmit`.

Events: `ItemChanged`, `ItemRemoved`.

## `ShellAppEffectHandler` (Ausschnitt)
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

- `LoadDownloadsEffect` → `LoadDownloadsAsync`: `LoadItemsAsync` + `GetUsedBytes`
  + Quota → `DownloadsLoadedMessage`.
- `ResumeDownloadsEffect` → `ResumeDownloadsAsync` → `ResumeInterruptedAsync`.
- Ausgelöst bei App-Start/Verbindungsaufbau und Online-Wechsel (`AppUpdate`).

## `AppUpdate` (Ausschnitt)
Datei: `VideoPlayer.Mvu/AppUpdate.cs`

- `DownloadsPageOpenedMessage` → `UpdateDownloadsPageOpened` → `LoadDownloadsEffect`.
- `DownloadsLoadedMessage` → setzt `Downloads.Items`, `UsedBytes`, `QuotaGb`.
- `DownloadItemChangedMessage` → Upsert in `Downloads.Items`.

## `DownloadsPage`
Datei: `VideoPlayer.Maui/Pages/DownloadsPage.xaml(.cs)`

`QuotaLabel` (Belegung), `OfflineBanner`, `ErrorPanel`, `EmptyLabel`,
`CollectionView` mit Abbrechen/Erneut versuchen/Löschen je Eintrag.
Kein Bereich für Restdaten — die Seite kennt nur Manifest-Einträge.
