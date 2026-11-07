# Detail: Fortschritts-Pfad Download → Anzeige (verifiziert)

## Datenfluss pro Fortschritts-Tick

1. `DownloadService.ReportProgressThrottledAsync`
   (DownloadService.cs:704-724) — Manifest-Update, dann
   `RaiseItemChanged(ToItem(entry))` (Zeile 723; Throttle
   `ProgressInterval` 250 ms, Zeile 104).
2. `ShellAppEffectHandler.OnDownloadItemChanged`
   (ShellAppEffectHandler.cs:747-748) — dispatcht
   `DownloadItemChangedMessage(e.Item)` in den `MvuHost`.
3. `AppUpdate.UpdateDownloadItemChanged` (AppUpdate.cs:1758-1769) —
   `Downloads = model.Downloads with { Items = UpsertItem(...) }`:
   neue `DownloadsPageState`-Instanz + neue `List<DownloadListItem>`
   (Kopie, ein Eintrag ersetzt oder angehängt).
4. `MvuHost.DispatchAsync` (MvuHost.cs:61-62) — `PropertyChanged(Model)`
   + `ModelChanged`.
5. `DownloadsPage`:
   - Binding `ItemsSource="{Binding Model.Downloads.Items}"` erhält eine
     **neue Listeninstanz** → CollectionView führt einen kompletten
     Reset durch (alle Zellen verworfen/neu erstellt).
   - `OnModelChanged` → `UpdateUi` aktualisiert Quota-/Status-Anzeige.

Ergebnis: 4×/Sekunde ein Voll-Reset der Liste → sichtbares Flackern.

## Identität und Reihenfolge

`UpsertItem` (AppUpdate.cs:1818-1835): Ersatz, wenn `EntryId` gleich
oder `MediaId`+`MediaType` gleich; sonst Anhängen. Reihenfolge =
Manifest-Auftragsreihenfolge (stabil; kein Umsortieren im MVU-Pfad).

## Weitere Quellen von Listenänderungen (Struktur)

- `DownloadsLoadedMessage` → komplette Liste neu (AppUpdate.cs:1682-1697).
- `DownloadEnqueueCompletedMessage` → Upsert (AppUpdate.cs:1734-1756).
- `DownloadDeletedMessage` → Entfernen nach `MediaId`+`MediaType`
  (AppUpdate.cs:1771-1785).

Strukturänderungen sind selten (Benutzeraktionen, Laden) — nur sie
dürfen CollectionChanged-Ereignisse auslösen.
