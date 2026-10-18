# Logik

## `DownloadService`
Datei: `VideoPlayer.Mvu/DownloadService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `EnqueueAsync` | `public` | Reiht einen Download ein (Dedup, Quota-Prüfung) |
| `CancelAsync` | `public` | Bricht wartende/laufende Downloads ab → `Cancelled` |
| `DeleteAsync` | `public` | Löscht einen Eintrag inkl. Datei, meldet `ItemRemoved` |
| `DeleteIfCompletedAsync` | `public` | Wie `DeleteAsync`, nur für `Completed` (Auto-Löschen gesehener Episoden) |
| `DeleteAllAsync` | `public` | Bricht alle Transfers ab, löscht alle Dateien + Manifest-Zeilen, meldet `ItemRemoved` je Eintrag; Rückgabe: Anzahl |
| `PurgeOrphansAsync` | `public` | Entfernt Dateien ohne Manifest-Eintrag; Rückgabe: freigegebene Bytes |
| `ReconcileAsync` | `public` | Abgleich Ablageort ↔ Manifest (Teildateien, Restdaten, fehlende Dateien → `Failed`) |
| `ResumeInterruptedAsync` | `public` | Plant wartende/verwaiste/nicht-Quota-fehlgeschlagene Aufträge erneut ein |
| `LoadItemsAsync` | `public` | Liest Manifest als `DownloadListItem`-Liste |
| `GetUsedBytes` | `public` | Belegter Speicher des Video-Ablageorts |

Publizierte Events: `ItemChanged` (`DownloadItemEventArgs`), `ItemRemoved` (`DownloadRemovedEventArgs`).
Interne Hilfsmittel: `manifestGate` (Serialisiert Manifest-Schreibzugriffe), `runningTransfers` (aktive Transfers, `Cancel()`-fähig), `TryDeleteFile`, `RaiseItemRemoved`, `RaiseItemChanged`, `FindEntryAsync`, `FilePathFor`, `ToItem`.

**Befund:** `DeleteAllAsync` arbeitet ungefiltert — eine Variante für `Failed`/`Cancelled` fehlt und ist direkt aus dem vorhandenen Muster ableitbar.

## `AppUpdate`
Datei: `VideoPlayer.Mvu/AppUpdate.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `UpdateDownloadsDeleteAllRequested` | `private` | `DeleteAllDownloadsEffect`, wenn `HasItems` oder `OrphanedBytes > 0` |
| `UpdateDownloadsPurgeOrphansRequested` | `private` | `PurgeOrphanedDownloadsEffect`, wenn `OrphanedBytes > 0` |
| `UpdateDownloadDeleteRequested` | `private` | `DeleteDownloadEffect` für einen Eintrag |
| `UpdateDownloadDeleted` | `private` | Entfernt Eintrag aus `Downloads.Items` |

Dispatch-Tabelle (Zeile ~298): `DownloadsDeleteAllRequestedMessage` → `UpdateDownloadsDeleteAllRequested`, `DownloadsPurgeOrphansRequestedMessage` → `UpdateDownloadsPurgeOrphansRequested`.

## `ShellAppEffectHandler`
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| Effekt-Switch (`HandleEffectAsync`) | — | `case DeleteAllDownloadsEffect` → `DeleteAllDownloadsAsync`, `case PurgeOrphanedDownloadsEffect` → `PurgeOrphanedDownloadsAsync` |
| `DeleteAllDownloadsAsync` | `private` | `downloads.DeleteAllAsync` + `LoadDownloadsAsync` (Liste + Belegung neu) |
| `PurgeOrphanedDownloadsAsync` | `private` | `downloads.PurgeOrphansAsync` + `LoadDownloadsAsync` |
| `DeleteDownloadAsync` | `private` | `downloads.DeleteAsync` je Eintrag |
| `LoadDownloadsAsync` | `private` | Manifest laden + `ReconcileAsync`, dispatcht `DownloadsLoadedMessage` |

Abonniert: `downloads.ItemChanged` → `DownloadItemChangedMessage`, `downloads.ItemRemoved` → `DownloadDeletedMessage`.

## `DownloadsPage` (Code-Behind)
Datei: `VideoPlayer.Maui/Pages/DownloadsPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `UpdateUi` | `private` | Setzt `DeleteAllButton.IsVisible = model.Downloads.HasItems \|\| showOrphans` |
| `OnDeleteAllClicked` | `private` | `DisplayAlertAsync`-Rückfrage → `DownloadsDeleteAllRequestedMessage` |
| `OnPurgeOrphansClicked` | `private` | `DisplayAlertAsync`-Rückfrage → `DownloadsPurgeOrphansRequestedMessage` |

XAML: `DeleteAllButton` („Alle löschen", `GhostButton` + `VpDanger`, `Grid.Row="0"`, `HorizontalOptions="End"`), `PurgeOrphansButton` („Speicher bereinigen", `SecondaryButton`).
