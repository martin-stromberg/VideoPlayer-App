# Logik — MVU-Update und Hilfsklassen

## `AppUpdate` (partial, statisch)

Datei: `VideoPlayer.Mvu/AppUpdate.cs` (~4237 Zeilen)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update` | public static | Zentrale Update-Funktion; dispatchend per `switch` auf `AppMessage` (~Z. 202: `SourceItemsLoadedMessage → UpdateSourceItemsLoaded`) |
| `UpdateSourceItemsLoaded` | private static | **~Z. 1595–1648**: Fehler-Append behält Liste; veraltete Anhänge (`Page != NextPage`) werden verworfen; **`Items` = `browser.Items.Concat(message.Items).ToList()` bei Append — neue Listeninstanz (Ursache des Sprungs)**; sonst direkte Ersetzung |
| `ReloadBrowserItems` | private static | ~Z. 1654–1676: setzt `Items = []`, `NextPage = 0`, `IsLoading = true` + `LoadSourceItemsEffect(Append: false)` — gewollter Reset (Genre-Wechsel, neue Suche, Retry) |
| `UpdateSourceGenreSelected` | private static | Genre-Chip → `ReloadBrowserItems` |
| `UpdateSourceSearchSubmitted` | private static | Suchtext (getrimmt) geändert → `ReloadBrowserItems`; identischer Text = no-op |
| `UpdateSourceItemsNextPageRequested` | private static | ~Z. 1708–1724: Guard `!IsActive \|\| !HasMore \|\| IsLoading \|\| IsLoadingMore`; setzt `IsLoadingMore = true` + `LoadSourceItemsEffect(Append: true)` mit aktivem `SearchText`/`GenreId` |
| `UpdateSourceItemsRetryRequested` | private static | Retry: bei Bestand → Folgeseite erneut, sonst `ReloadBrowserItems` |
| `UpdateSourceSelected` / `UpdateSourceBrowserOpened` | private static | Browser öffnen: `NewBrowser` + Genres- und Seite-0-Ladung (`Append: false`); Rückkehr auf gleiche Quelle = no-op (Zustand bleibt) |
| `NewBrowser` | private static | Frischer `SourceBrowserState` pro Quelle |

Publizierte Effekte: `LoadSourceItemsEffect`, `LoadSourceGenresEffect`,
`NavigateToSourceOverviewEffect`, `NavigateToMediaDetailsEffect`.

## `MvuHost`

Datei: `VideoPlayer.Mvu/MvuHost.cs`

| Methode/Ereignis | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `DispatchAsync` | public | `Update` anwenden → `Model` ersetzen → `PropertyChanged(nameof(Model))` + `ModelChanged` → Effekte ausführen (serialisiert per `SemaphoreSlim`) |
| `Model` | public get | Aktuelles unveränderliches `AppModel` — **jede Modellablösung lässt alle Bindings neu auswerten, damit wechselt auch die `ItemsSource`-Referenz** |
| `ModelChanged` | event | Seiten abonnieren es und rufen `UpdateUi` auf dem UI-Thread |

## `DownloadListSync` — bestehendes Präzedenzmuster

Datei: `VideoPlayer.Mvu/DownloadListSync.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Sync` | public static | Gleicht `ObservableCollection<DownloadItemViewModel>` (stabile ItemsSource der Seite) granular mit `IReadOnlyList<DownloadListItem>` ab: entfallene entfernen, bestehende in-place aktualisieren, neue einfügen, verschobene `Move`n — kein `CollectionChanged` bei Inhalt-Updates → kein Listenreset |
| `IsSameDownload` | private static | Identitätsregel: gleiche `EntryId` oder `MediaId`+`MediaType` |

Relevanz: exakt dasselbe Problem wurde dort bereits gelöst — stabile gebundene
Liste + granularer Abgleich im Mvu-Projekt (unit-testbar, `DownloadListSyncTests`).

## App-seitiger Effekt-Handler (Kontext)

`LoadSourceItemsEffect` wird in `VideoPlayer.Maui` behandelt
(`IAppEffectHandler`-Implementierung): ruft `api/items?mediaSourceId=…&page=…&size=…[&search=…][&genreId=…]`
und dispatcht `SourceItemsLoadedMessage` — unverändert relevant.
