# Bestandsaufnahme: Scrollposition beim Nachladen der Quellen-Übersicht

Analysiert wurde der Anzeige- und Nachladepfad der Quellen-Übersicht
(`SourceOverviewPage` ↔ `SourceBrowserState` ↔ `UpdateSourceItemsLoaded`)
bezogen auf die Anforderung, die Scrollposition beim Anhängen einer Folgeseite
zu erhalten.

## Zusammenfassung

- **Fehlerbild bestätigt:** `ItemsCollection.ItemsSource` ist per XAML-Binding an
  `Model.SourceBrowser.Items` gekoppelt. `UpdateSourceItemsLoaded` erzeugt beim
  Anhängen (`Append = true`) per `Concat().ToList()` jedes Mal eine **neue**
  Listeninstanz (`AppUpdate.cs` ~Z. 1629–1631). Da `MvuHost` nach jedem Update
  `PropertyChanged(nameof(Model))` feuert, wird die `ItemsSource`-Referenz
  ersetzt → die `CollectionView` baut die Ansicht neu auf und springt an den
  Listenanfang.
- **Präzedenzmuster vorhanden:** Die `DownloadsPage` löst exakt dasselbe Problem
  bereits — eine stabile, seiten-eigene `ObservableCollection<DownloadItemViewModel>`
  wird einmal als `ItemsSource` gesetzt und über den Helper `DownloadListSync`
  (im `VideoPlayer.Mvu`-Projekt → unit-testbar) granular mit der unveränderlichen
  Modell-Liste abgeglichen. Nur Strukturänderungen erzeugen
  `CollectionChanged`-Ereignisse.
- **Eintragsmodell:** `SourceMediaItem` ist ein unveränderliches `record` mit
  stabiler `Id` — direkt bindbar, kein ViewModel-Wrapper nötig (das XAML-
  `DataTemplate` deklariert bereits `x:DataType="mvu:SourceMediaItem"`).
- **Nachladepfad:** `OnRemainingItemsThresholdReached` →
  `SourceItemsNextPageRequestedMessage` → `UpdateSourceItemsNextPageRequested`
  → `LoadSourceItemsEffect(Append: true)` → `SourceItemsLoadedMessage` →
  `UpdateSourceItemsLoaded`. Derselbe Pfad gilt für ungefilterte, Such- und
  Genre-Treffer (Filterparameter stecken im Effekt).
- **Reset-Pfad:** `ReloadBrowserItems` setzt `Items = []` und lädt ab Seite 0
  (Genre-Wechsel, neue Suche, Retry bei leerer Liste); `SourceBrowserOpened`
  (neue Quelle) tut dasselbe. Dort ist der Listenersatz ausdrücklich gewünscht.
- **Achtung Seiteneffekt `PlaylistDetailsPage`:** Sie nutzt denselben
  `RemainingItemsThreshold`-Mechanismus mit `Items`-Binding — gleiche
  Fehleranfälligkeit, aber **nicht** Teil dieser Anforderung (nur vermerkt).
- **Altbestand `VideoWebPlayer.Maui`:** eigene, ViewModel-basierte App-Variante
  mit `ObservableCollection` + `Items.Add` — bereits inkrementell, nicht betroffen.

Test-Ausgangszustand: **1320/1320 Unit-Tests grün** (`VideoWebPlayer.Maui.Tests`,
Exit-Code 0, ~30 s) — kein bekannter Fehlschlag. Nachweis:
[inventory/tests.md](inventory/tests.md) +
[inventory/test-results/baseline-unittests.log](inventory/test-results/baseline-unittests.log).
E2E-Tests (`VideoPlayer.E2E.Tests`) bauen und starten die echte Windows-App —
Ausführbarkeit in dieser Umgebung wird im Testschritt verifiziert.

## Details

- [Datenmodell](inventory/models.md) — `SourceBrowserState`, `SourceMediaItem`, `LoadSourceItemsEffect`, `SourceItemsLoadedMessage`
- [Logik](inventory/logic.md) — `AppUpdate`-Handler, `MvuHost`, `DownloadListSync` (Muster)
- [UI](inventory/ui.md) — `SourceOverviewPage`, `DownloadsPage` (Muster), Altbestand
- [Tests](inventory/tests.md) — Test-Ausgangszustand, Testklassen und E2E-Infrastruktur
