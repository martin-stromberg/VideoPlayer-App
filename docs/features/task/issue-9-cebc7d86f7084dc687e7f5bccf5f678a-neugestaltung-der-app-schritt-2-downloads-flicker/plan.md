# Umsetzungsplan: Downloads-Übersicht ohne Flackern (Schritt 2)

## Übersicht

Das Flackern entsteht, weil das Binding `ItemsSource="{Binding
Model.Downloads.Items}"` bei jedem Fortschritts-Tick eine neue
Listeninstanz erhält und der `CollectionView` dadurch alle Zellen
verwirft. Das MVU-Modell bleibt bewusst unveränderlich — der Fix liegt
am Seitenrand: Die `DownloadsPage` hält eine **stabile**
`ObservableCollection` von Eintrags-ViewModels als `ItemsSource` und
gleicht sie bei jeder Modelländerung in-place mit
`Model.Downloads.Items` ab. Ein Fortschritts-Tick löst dann nur noch
`PropertyChanged` am betroffenen Eintrag aus — kein CollectionView-Reset,
kein Neuzeichnen der übrigen Liste.

Die neuen Typen kommen in `VideoPlayer.Mvu` (plattformneutral), damit
sie vom Testprojekt `VideoWebPlayer.Maui.Tests` (referenziert nur Mvu/
ApiClient/Storage/Client) per xUnit geprüft werden können — gleiche
Konvention wie `DownloadListItem` mit seinen Anzeige-Properties.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|------------------|------------|
| Eintrags-ViewModel | `DownloadItemViewModel` (neu, `VideoPlayer.Mvu`, `sealed class`, `INotifyPropertyChanged`): kapselt das unveränderliche `DownloadListItem` (`Item`), delegiert alle gebundenen Anzeige-Properties; `Update(item)` ersetzt den Record und meldet `PropertyChanged` für die Anzeige-Properties — bei Record-Gleichheit keine Benachrichtigung | Trennt unveränderliches MVU-Modell vom gebundenen UI-Eintrag; Fortschritts-Tick aktualisiert nur den einen Eintrag; im Testprojekt instanziierbar |
| Listen-Abgleich | `DownloadListSync` (neu, `VideoPlayer.Mvu`, statisch): `Sync(ObservableCollection<DownloadItemViewModel>, IReadOnlyList<DownloadListItem>)` — (a) entfallene Einträge entfernen + bestehende in-place `Update`, (b) fehlende an Zielposition `Insert`, verschobene per `Move`; Identität wie `UpsertItem`: `EntryId` oder `MediaId`+`MediaType` | In-place-Updates erzeugen **kein** `CollectionChanged` → kein Reset; Strukturänderungen (Laden/Einreihen/Löschen) erzeugen nur die minimalen Add/Remove/Move-Events |
| ItemsSource | `DownloadsCollection.ItemsSource` wird **einmal** im Konstruktor auf die `ObservableCollection` gesetzt; XAML-Binding auf `Model.Downloads.Items` entfällt | Neue ItemsSource-Instanz pro Tick entfällt → kein Reset |
| DataTemplate | `x:DataType` des ItemTemplate auf `DownloadItemViewModel` (neues `xmlns:pages`); Property-Namen bleiben identisch (Delegation) | Kompilierte Bindings bleiben; XAML-Struktur unverändert |
| Klick-Handler | `BindingContext is DownloadItemViewModel v` → Dispatch mit `v.Item` | Request-Messages erwarten weiterhin `DownloadListItem` — MVU-Seite unverändert |
| MVU-Kern | **keine Änderung** an `AppUpdate`/`DownloadsPageState`/`DownloadListItem` | Kleinster Eingriff; alle `AppUpdateTests_Downloads` bleiben unverändert gültig |

## Programmabläufe

### Fortschritts-Tick (Hot Path)

1. `DownloadItemChangedMessage` → `AppUpdate` upsertet wie bisher →
   `ModelChanged`.
2. `DownloadsPage.UpdateUi`: `DownloadListSync.Sync(downloadItems,
   model.Downloads.Items)` — gleiche Identitätsfolge → nur
   `viewItem.Update(changed)`.
3. `Update` erkennt Record-Ungleichheit → `PropertyChanged` für
   `Progress`, `ProgressPercentText` u. a. → `ProgressBar` des einen
   Eintrags aktualisiert sich. Kein `CollectionChanged`, kein Reset.

### Strukturänderung (Laden, Einreihen, Löschen)

`Sync` entfernt entfallene VMs (`RemoveAt`), fügt neue an der
Zielposition ein (`Insert`) bzw. verschiebt (`Move`) — der
CollectionView aktualisiert nur die betroffenen Positionen.

### Aktionen (Abbrechen / Erneut versuchen / Löschen)

Handler lesen `DownloadItemViewModel.Item` und dispatchen die
unveränderten Request-Messages; Guards (`CanCancel`/`CanRetry`) bleiben
im `AppUpdate`.

## Betroffene Dateien

**Neu (`VideoPlayer.Mvu`):**
- `DownloadItemViewModel.cs` — INPC-Wrapper mit `Update`.
- `DownloadListSync.cs` — statischer Abgleich in die
  `ObservableCollection`.

**Geändert (`VideoPlayer.Maui`):**
- `Pages/DownloadsPage.xaml` — `ItemsSource`-Binding entfernt,
  `xmlns:pages` + `x:DataType="pages:DownloadItemViewModel"` im
  DataTemplate.
- `Pages/DownloadsPage.xaml.cs` — `ObservableCollection`-Feld,
  einmaliges `ItemsSource`-Setzen, `DownloadListSync.Sync` in
  `UpdateUi`, Handler auf `DownloadItemViewModel` umgestellt.

**Tests (`VideoWebPlayer.Maui.Tests`):**
- `DownloadListSyncTests.cs` (neu) — In-place-Update ohne
  CollectionChanged, PropertyChanged nur bei echter Änderung,
  Insert/Remove/Move, Identitätsregeln (EntryId vs. MediaId+MediaType).
- `DownloadsContractTests.cs` — Pin `{Binding Model.Downloads.Items}`
  ersetzen durch Pins der neuen Verdrahtung (`x:Name="DownloadsCollection"`,
  `DownloadItemViewModel`-DataType, `DownloadListSync.Sync` und
  `ItemsSource =` im Code-Behind).

## Testnachweise (verbindlich)

UI-E2E existiert nicht (Projektkonvention); Absicherung über xUnit +
Contract-Tests:

1. Fortschritts-Tick: `Sync` mit geändertem Fortschritt → dieselbe
   VM-Instanz bleibt, `PropertyChanged(Progress)` feuert, **kein**
   `CollectionChanged`.
2. Unveränderte Liste: `Sync` mit gleichen Records → keinerlei
   Benachrichtigungen.
3. Struktur: Insert am Ende, Remove entfallener Einträge, Move bei
   geänderter Reihenfolge — jeweils minimale CollectionChanged-Events.
4. Identität: Ersatz über `EntryId` und über `MediaId`+`MediaType`.
5. Contract: DownloadsPage-Verdrahtung (stabile ItemsSource, Sync-
   Aufruf, VM-DataType, Request-Dispatches).

## Offene Punkte

Keine.
