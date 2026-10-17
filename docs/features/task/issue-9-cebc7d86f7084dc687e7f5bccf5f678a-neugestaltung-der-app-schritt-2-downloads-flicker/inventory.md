# Bestandsaufnahme — Downloads-Übersicht ohne Flackern (Schritt 2)

Repo: `VideoPlayer.App.sln`. Basisbranch (Diff-Basis):
`task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`.

Detaildokumente: `inventory/flicker-pfad.md` (verifizierter Update-Pfad
Fortschritt → UI).

## 1. Verifizierte Flicker-Ursache

- `VideoPlayer.Maui/Pages/DownloadsPage.xaml:58-60`: `CollectionView`
  `DownloadsCollection` mit `ItemsSource="{Binding Model.Downloads.Items}"`
  (BindingContext = `MvuHost`).
- `VideoPlayer.Mvu/MvuHost.cs:61-62`: Jeder Dispatch feuert
  `PropertyChanged(nameof(Model))` + `ModelChanged` — auch bei
  Fortschritts-Ticks.
- `VideoPlayer.Mvu/AppUpdate.cs:1758-1769`
  (`UpdateDownloadItemChanged`): `UpsertItem` (AppUpdate.cs:1818-1835)
  kopiert die Liste per `ToList()` und ersetzt den einen Eintrag →
  **jede** Fortschrittsmeldung erzeugt eine neue `Items`-Instanz →
  Binding schreibt neue `ItemsSource` → CollectionView-Reset → alle
  Zellen werden verworfen und neu aufgebaut = Flackern der ganzen Seite.
- `DownloadsPage.xaml.cs:38-51`: `OnModelChanged` → `UpdateUi` setzt
  zusätzlich Quota-/Fehler-/Leer-Anzeige neu (unschädlich, gleiche
  Werte).

## 2. Vorhandener granularer Pfad (bereits implementiert)

- `DownloadService.ReportProgressThrottledAsync`
  (DownloadService.cs:704-724): Fortschritt gedrosselt auf
  `ProgressInterval = 250 ms` (Zeile 104), Manifest-Update +
  `RaiseItemChanged(ToItem(entry))`.
- `ShellAppEffectHandler.cs:63-64,747-748`: `downloads.ItemChanged` →
  `DownloadItemChangedMessage(item)` in den MVU-Zyklus;
  `ItemRemoved` → `DownloadDeletedMessage`.
- `AppMessage.cs:522`: `DownloadItemChangedMessage(DownloadListItem Item)`.
- Identität eines Eintrags (UpsertItem, AppUpdate.cs:1822-1832):
  `EntryId`-Gleichheit **oder** `MediaId`+`MediaType`-Gleichheit;
  Sortierung bleibt Manifest-Reihenfolge, neue Einträge werden angehängt.

## 3. Anzeige-Record

`DownloadListItem` (VideoPlayer.Mvu/DownloadListItem.cs) — `sealed
record` mit `init`-Properties (EntryId, MediaType, MediaId, Title,
State, DownloadedBytes, TotalBytes, Error) und abgeleiteten
Anzeigewerten: `Progress` (0–1), `ProgressPercentText`, `StateText`,
`ShowProgress`, `HasError`, `ErrorText`, `CanCancel`, `CanRetry`,
`MediaTypeText`. Record-Gleichheit deckt alle Anzeige-Eingaben ab —
ein geänderter Fortschritt erzeugt einen ungleichen Record.

## 4. Seite und Bindings (DownloadsPage)

- XAML-Gerüst: Quota-Label, Offline-Banner, LoadingIndicator
  (`{Binding Model.Downloads.IsLoading}` auf `MvuHost`), ErrorPanel,
  EmptyLabel, `CollectionView` mit `DataTemplate x:DataType=
  mvu:DownloadListItem` — Karte mit Titel, Typ/Status/%-Labels,
  `ProgressBar {Binding Progress}`, Fehler-Label, Buttons Abbrechen /
  Erneut versuchen / Löschen (Sichtbarkeit `CanCancel`/`CanRetry`).
- Code-Behind: Klick-Handler lesen `BindingContext` als
  `DownloadListItem` und dispatchen die Request-Messages
  (`DownloadsPage.xaml.cs:53-75`).
- Design-Tokens (Schritt 1): Vp*-Ressourcen — keine Legacy-Hexfarben.

## 5. Tests und Konventionen

- `VideoWebPlayer.Maui.Tests` referenziert nur `VideoPlayer.Mvu`,
  `ApiClient`, `Storage`, `Client` — **nicht** `VideoPlayer.Maui`.
  Testbarer Code muss daher im Mvu-Projekt liegen; die MAUI-Schicht
  wird über Contract-Tests (Quelltext-Pins) abgesichert.
- `DownloadsContractTests.cs:24` pinnt `{Binding Model.Downloads.Items}`
  im XAML — muss bei ItemsSource-Umstellung angepasst werden.
- `AppUpdateTests_Downloads` (496 Zeilen): alle Download-Übergänge —
  dürfen unverändert grün bleiben (MVU-Seite wird nicht angefasst).
- Hooks: XML-Doku deutsch, CS1591 = Fehler, kein
  `NotImplementedException`, Enum-Coverage (kein neuer Enum nötig).
- Keine neuen Tabellen/Spalten → keine EF-/Backup-Pflichten.

## 6. Bewertung der Lösungsoptionen

| Option | Bewertung |
|--------|-----------|
| `ItemsSource` nur bei Strukturänderung neu setzen | Vermeidet Reset, aber Eintrags-Ersatz in derselben `IReadOnlyList` ohne `INotifyCollectionChanged` wird vom CollectionView nicht bemerkt → unzureichend allein. |
| `DownloadListItem` als INPC-Klasse im Modell | Verletzt die Unveränderlichkeits-Konvention des MVU-Modells (`with`-Updates, `Assert.Same`-Tests). |
| Stabile `ObservableCollection` + INPC-Eintrags-ViewModel am Seitenrand, in-place abgeglichen | Kleinster sauberer Eingriff: MVU bleibt unverändert, Fortschritts-Tick meldet nur `PropertyChanged` am betroffenen Eintrag, CollectionView wird nicht zurückgesetzt. Logik in `VideoPlayer.Mvu` → per xUnit testbar. **Gewählt.** |

## Offene Fragen

- Keine.
