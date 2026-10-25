# Umsetzungsplan: Genreauswahl in Quellenansicht verhindert die Einsicht der Titel

## Übersicht

Die Genre-Leiste der `SourceOverviewPage` wird von einer horizontalen `CollectionView` auf das im Projekt bewährte `ScrollView` + `BindableLayout`-Muster des `MediaCarousel` umgebaut, sodass ihre Höhe wieder aus dem Chip-Inhalt gemessen wird statt die gesamte Resthöhe zu füllen (iOS-Fehlerbild: bildschirmhohe Pillen, verdecktes Medien-Raster). Das Auswahl- und Filterverhalten bleibt fachlich unverändert. Ergänzend wird das E2E-Baum-DTO um `bounds` erweitert und ein E2E-Nachweis für die begrenzte Leistenhöhe bei sichtbarem Medien-Raster geschaffen.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Genre-Leiste `GenresCollection` | `ScrollView Orientation="Horizontal"` + `HorizontalStackLayout` mit `BindableLayout.ItemsSource` + `TapGestureRecognizer` je Chip | Projektweit bewährtes Muster (`MediaCarousel`); Höhe folgt dem Inhalt auf allen Plattformen — keine `HeightRequest`-Nagellösung, die bei dynamischer Schriftgröße clippt und den iOS-`CollectionView`-Messfehler nur umgeht statt beseitigt. Developer-API (`TrySelectItem`/`ItemsOf`) unterstützt BindableLayout-Container bereits, E2E-Steuerbarkeit bleibt erhalten. |
| Auswahl-Ereignis | `TapGestureRecognizer` `Tapped="OnGenreTapped"` am Chip-`Border` | Ersetzt `SelectionChanged` der `CollectionView`; identische Ereigniskette wie Fingertipp, wird auch von `DevApiVisualTree.TrySelectBindableLayoutItem` (`SendTapped`) ausgelöst. |
| Auswahlzustand | Weiterhin `IsSelected`-`DataTrigger` am `Chip`-Border | Fachliches Verhalten und Optik bleiben; Contract-Test `Styles_PinsPillShapes` pinnt `{StaticResource Chip}` auf der Seite. |

## Programmabläufe

### Genre-Auswahl (unverändertes Fachverhalten, neue UI-Kette)

1. `HorizontalStackLayout` rendert je `SourceGenreItem` ein `Border`-Element (`Chip`-Style) mit `TapGestureRecognizer`.
2. Fingertipp (oder `POST /invoke` mit `itemIndex` → `SendTapped`) löst `OnGenreTapped` aus; `sender.BindingContext` liefert das `SourceGenreItem`.
3. `OnGenreTapped` dispatcht `SourceGenreSelectedMessage(genre.Id)` an den `MvuHost`.
4. `UpdateSourceGenreSelected` setzt `SelectedGenreId`, mapped `IsSelected` neu → `BindableLayout` rendert die Chips mit aktualisiertem Auswahlzustand; `ReloadBrowserItems` lädt die Einträge ab Seite 0 mit `genreId`-Filter.

Beteiligte Klassen/Komponenten: `SourceOverviewPage` (XAML + Code-Behind), `MvuHost`, `AppUpdate`, `SourceBrowserState`, `SourceGenreItem`, `DevApiVisualTree`.

### Sichtbarkeit der Leiste (unverändert)

`IsVisible="{Binding Model.SourceBrowser.ShowGenreFilter}"` bleibt am Leisten-Container; ohne reale Server-Genres (nur „Alle") wird keine Leiste gezeigt.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `DevApiBounds` | DTO (E2E-Testprojekt, `DevApiModels.cs`) | Spiegelt die serverseitig bereits gelieferten `bounds`-Daten (`X`, `Y`, `Width`, `Height`) des `GET /tree`-Elementbaums für Höhen-/Sichtbarkeits-Nachweise. |

## Änderungen an bestehenden Klassen

### `SourceOverviewPage` (XAML)

- `CollectionView` `GenresCollection` (Grid.Row 1) wird ersetzt durch:
  `ScrollView Orientation="Horizontal"` `HorizontalScrollBarVisibility="Never"` mit `AutomationId="GenresCollection"` und `IsVisible="{Binding Model.SourceBrowser.ShowGenreFilter}"`, enthaltend `HorizontalStackLayout Spacing="8"` mit `BindableLayout.ItemsSource="{Binding Model.SourceBrowser.Genres}"` und dem bisherigen Chip-`DataTemplate` (`Border` `Chip`-Style + `IsSelected`-Trigger + `Label`), ergänzt um `TapGestureRecognizer Tapped="OnGenreTapped"` am Chip-`Border`.

### `SourceOverviewPage` (Code-Behind)

- **Geänderte Methoden:** `OnGenreSelected(object?, SelectionChangedEventArgs)` wird ersetzt durch `OnGenreTapped(object?, TappedEventArgs)` — dispatcht `SourceGenreSelectedMessage` aus `((BindableObject)sender).BindingContext as SourceGenreItem`; das `SelectedItem = null`-Zurücksetzen entfällt (kein Selektionsmodell mehr nötig).

### `DevApiTreeNode` (DTO, `VideoPlayer.E2E.Tests/Support/DevApiModels.cs`)

- **Neue Eigenschaft:** `Bounds` (`DevApiBounds?`, `[JsonPropertyName("bounds")]`) — bildet die vorhandenen Serverdaten ab.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **E2E-Aufrufe `InvokeAsync("GenresCollection", index)`** (`SourceBrowsingTests`, `SourceSearchTests`): laufen künftig über `TrySelectBindableLayoutItem` statt `CollectionView.SelectedItem` — dieselbe Auswahlwirkung; Voraussetzung ist `TapGestureRecognizer` im Chip-Template und AutomationId am Container.
- **`items`-Ableitung im Elementbaum** bleibt erhalten (`ItemsOf` deckt BindableLayout-Container ab) — kein Verlust der Diagnosedaten.
- **Horizontales Überlaufen** vieler Genres: `ScrollView` scrollt (wie bisher die `CollectionView`); `HorizontalScrollBarVisibility="Never"` entspricht der bisherigen Optik.
- **Keine Änderung** an `ItemsCollection`, `SourceItemListSync`, MVU-Zustand oder API-Zugriffen.

## Umsetzungsreihenfolge

1. **E2E-DTO `DevApiBounds` + `bounds`-Feld ergänzen** (`VideoPlayer.E2E.Tests/Support/DevApiModels.cs`)
   - Voraussetzungen: Keine (Server liefert `bounds` bereits).
   - Beschreibung: `DevApiBounds`-Klasse (`X`/`Y`/`Width`/`Height`, JsonPropertyNames) anlegen, `Bounds`-Property an `DevApiTreeNode` ergänzen.
2. **Fehlschlagenden Nachweis-Test schreiben** (Red): Contract-Test `SourceOverviewPage_GenreBar_UsesBoundedHorizontalLayout` in `SourcesContractTests` — prüft, dass die Genre-Leiste im XAML als `ScrollView Orientation="Horizontal"` mit `BindableLayout.ItemsSource` umgesetzt ist (schlägt vor dem Umbau fehl) sowie weiterhin `{Binding Model.SourceBrowser.Genres}` und `{StaticResource Chip}`.
   - Voraussetzungen: Keine.
3. **Genre-Leiste umbauen** (`SourceOverviewPage.xaml` + `.xaml.cs`)
   - Voraussetzungen: Keine.
   - Beschreibung: `CollectionView` → `ScrollView`/`HorizontalStackLayout`/`BindableLayout` wie oben; `OnGenreSelected` → `OnGenreTapped`.
4. **E2E-Nachweis ergänzen** (`SourceBrowsingTests`): neuer Test `GenreBar_HeightBounded_ItemsGridVisible` — nach Laden der Quelle mit Genres: `GenresCollection`-`Bounds.Height` ≤ ~120 (einzeilige Chip-Leiste) und `ItemsCollection` sichtbar mit `Bounds.Height` ≥ ~200; Genre-Auswahl weiterhin funktionsfähig.
   - Voraussetzungen: Schritt 1 (`bounds`-DTO), Schritt 3 (Umbau).
5. **Build + Unit-Tests + E2E-Teilsuite laufen lassen** (`SourceBrowsingTests`, `SourceSearchTests`, `SourcePagingTests`) — Nachweis, dass Auswahl, Sichtbarkeit und Layout stimmen.
   - Voraussetzungen: Schritte 1–4.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `SourceOverviewPage_GenreBar_UsesBoundedHorizontalLayout` | `SourcesContractTests` | XAML nutzt `ScrollView Orientation="Horizontal"` + `BindableLayout.ItemsSource="{Binding Model.SourceBrowser.Genres}"` + `{StaticResource Chip}`; kein `CollectionView`-Element namens `GenresCollection` mehr (Red→Green-Nachweis des Umbaus) |
| `GenreBar_HeightBounded_ItemsGridVisible` | `SourceBrowsingTests` (E2E) | `GenresCollection.Bounds.Height` ≤ 120 und `ItemsCollection` sichtbar mit `Bounds.Height` ≥ 200 — Titelliste einsehbar, Leiste nicht bildschirmfüllend |
| `DevApiBounds` | `DevApiModels` (E2E-Support) | DTO-Feld `bounds` des Elementbaums |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Keine Anpassung nötig — `SourcesContractTests.SourceOverviewPage_HasGenreBar_Search_AndIncrementalGrid` bleibt gültig (`{Binding Model.SourceBrowser.Genres}` bleibt via `BindableLayout.ItemsSource` vorhanden); `SourceBrowsingTests`/`SourceSearchTests` bleiben kompatibel (`TrySelectBindableLayoutItem`).

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Quelle öffnen → Genre-Leiste begrenzt hoch, Medien-Raster sichtbar und bedienbar | `SourceBrowsingTests.GenreBar_HeightBounded_ItemsGridVisible` (neu) | Titel der Quelle sind einsehbar; Genre-Leiste füllt nicht die Seite | Layout-Fehler ist nur im gerenderten UI-Baum nachweisbar (Bounds), nicht im MVU-Modell |
| Pflicht | Genre-Chip wählen → gefilterter Reload; „Alle" → ungefiltert | `SourceBrowsingTests.GenreBar_VisibleAndFilters_ResetsOnAll` (bestehend, muss weiterlaufen) | Filme/Serien aufruf- und filterbar | Nachweis, dass der Umbau die Auswahl-Ereigniskette nicht bricht |
| Pflicht | Keine Server-Genres → keine Leiste | `SourceBrowsingTests.GenreBar_Hidden_WhenServerHasNoGenres` (bestehend) | Keine funktionslose Leiste | Regression der Sichtbarkeitsregel |
| Pflicht | Suche über der Genre-Leiste bleibt funktionsfähig | `SourceSearchTests` (bestehend, nutzt `GenresCollection`-Invoke) | Suchfluss unverändert | Regression der Seiteninteraktionen |

Welche bestehenden E2E-Tests müssen angepasst werden?

Keine — die Aufrufe `InvokeAsync("GenresCollection", …)` bleiben API-kompatibel.

## Offene Punkte

Keine.
