# Umsetzungsplan: Scrollposition beim Nachladen der Quellen-Übersicht

## Übersicht

Die Trefferliste der Quellen-Übersicht springt beim automatischen Nachladen
einer Folgeseite an den Anfang, weil das XAML-Binding
`ItemsSource="{Binding Model.SourceBrowser.Items}"` bei jedem Modell-Tick eine
neue Listeninstanz erhält. Umgesetzt wird das im Projekt bereits etablierte
Muster der `DownloadsPage`: eine stabile, seiten-eigene `ObservableCollection`
als `ItemsSource` plus granularer Sync-Helper (`SourceItemListSync`, analog
`DownloadListSync`) im `VideoPlayer.Mvu`-Projekt. Angehängte Folgeseiten erzeugen
nur `Add`-Benachrichtigungen — die `CollectionView` behält ihre Scrollposition.
Modell und Ladepfad (`AppUpdate`) bleiben unverändert; das Verhalten gilt damit
automatisch für ungefilterte, Such- und Genre-Treffer.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Gebundene Eintragsliste | Stabile `ObservableCollection<SourceMediaItem>` im Code-Behind + Sync-Helper `SourceItemListSync` im Mvu-Projekt | Präzedenz `DownloadListSync`/`DownloadsPage` für exakt dasselbe Problem; Modell bleibt unveränderlich; Helper liegt im testbaren Projekt |
| Eintragstyp der gebundenen Liste | `SourceMediaItem` direkt (kein ViewModel-Wrapper) | Einträge haben keinen veränderlichen Anzeigezustand (alle Properties `init`); XAML-Template nutzt bereits `x:DataType="mvu:SourceMediaItem"` |
| Abgleichsstrategie | Präfix-Abgleich über `SourceMediaItem.Id`: gemeinsames Präfix unverändert lassen, View-Ende danach entfernen, Modell-Ende danach anhängen | Folgeseiten verlängern ausschließlich das Ende (Append-Semantik von `UpdateSourceItemsLoaded`); Filterwechsel/Neuladen weicht bereits vorn ab → voller Neuaufbau = gewollter Rücksprung an den Anfang. Minimaler Code, keine Move-Logik nötig (Listen sind append-only) |
| Kein Ansatz: `Items` im Modell als `ObservableCollection` mutieren | verworfen | Verletzt die Unveränderlichkeit des MVU-Modells (alte Zustände teilten sich mutierte Listen); widerspricht der Record-/`with`-Semantik |
| E2E-Trigger und Nachweis | UIA-`ScrollPattern` auf dem ScrollViewer der `ItemsCollection`: bis ans Ende scrollen (löst `RemainingItemsThresholdReached` aus wie ein Nutzer), nach dem Append `VerticalScrollPercent > 0` prüfen | Echte Eingabekette statt interner Aufrufe; Listeneinträge fehlen unter Windows im UIA-Baum (dokumentierte Lücke) — die Scroll-Position lässt sich nur am ScrollViewer ablesen. Fallback: Mausrad-Scroll am Elementmittelpunkt |

## Programmabläufe

### Folgeseite nachladen (Scrollposition bleibt)

1. Nutzer scrollt ans Listenende → `OnRemainingItemsThresholdReached` dispatcht
   `SourceItemsNextPageRequestedMessage`.
2. `UpdateSourceItemsNextPageRequested` setzt `IsLoadingMore = true` und löst
   `LoadSourceItemsEffect(Append: true)` aus (unverändert).
3. `UpdateSourceItemsLoaded` liefert `Items` als neue Liste
   (alte + angehängte Einträge) — unverändert.
4. `MvuHost` feuert `ModelChanged` → `SourceOverviewPage.UpdateUi` ruft
   `SourceItemListSync.Sync(browserItems, model.SourceBrowser.Items)`.
5. Das gemeinsame Präfix (alle bisherigen Einträge) bleibt unverändert, die
   neuen Einträge werden per `Add` angehängt — kein `ItemsSource`-Wechsel,
   kein Reset der `CollectionView` → Scrollposition bleibt erhalten.

Beteiligte Klassen/Komponenten: `SourceOverviewPage`, `SourceItemListSync`,
`MvuHost`, `AppUpdate`, `SourceBrowserState`.

### Listenwechsel (Erstladen, Genre-Wechsel, neue Suche — Liste ab Anfang)

1. `ReloadBrowserItems` setzt `Items = []` → `Sync` leert die gebundene Liste
   (Präfix bricht ab / Modell leer).
2. Die neue erste Seite weicht im ersten Eintrag ab (oder die Liste war leer)
   → `Sync` ersetzt den Inhalt ab Position 0 → `CollectionView` baut neu auf und
   zeigt ab dem Listenanfang — gewünschtes Verhalten laut Anforderung.

Beteiligte Klassen/Komponenten: `AppUpdate`, `SourceItemListSync`, `SourceOverviewPage`.

### Edge-Cases des Abgleichs

- **Verworfener Stale-Append / Fehler beim Anhängen:** `Items` im Modell
  unverändert → `Sync` ohne Ereignisse — Liste bleibt stehen.
- **Retry nach Append-Fehler:** Folgeseite wird erneut angehängt (Präfix gleich)
  → nur `Add`.
- **Rückkehr auf die Seite (gleiche Quelle):** Modell unverändert → `Sync` no-op;
  die gebundene Liste steht noch am bisherigen Stand (Zustand bleibt — wie bisher).
- **Seitenwechsel auf andere Quelle:** `NewBrowser` liefert leere/andere `Items`
  → Listenwechselpfad → Anzeige ab Anfang. (Die neue Seite instanziiert ohnehin
  eine eigene `ObservableCollection`.)

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `SourceItemListSync` | statische Klasse (`VideoPlayer.Mvu/SourceItemListSync.cs`) | Gleicht `ObservableCollection<SourceMediaItem>` (ItemsSource der Seite) mit `IReadOnlyList<SourceMediaItem>` (Modell) ab: gemeinsames Präfix per `Id` unverändert lassen, View-Ende ab Präfix entfernen, Modell-Ende ab Präfix anhängen |
| `SourceItemListSyncTests` | Testklasse (`VideoWebPlayer.Maui.Tests/SourceItemListSyncTests.cs`) | Unit-Tests des Abgleichs (Append ohne Reset, Ersetzen, Schrumpfen, No-Op) |
| `SourcePagingTests` | E2E-Testklasse (`VideoPlayer.E2E.Tests/Tests/SourcePagingTests.cs`) | E2E-Szenario „Nachladen hält Scrollposition" |

## Änderungen an bestehenden Klassen

### `SourceOverviewPage` (Code-Behind, `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml.cs`)

- **Neues Feld:** `private readonly ObservableCollection<SourceMediaItem> browserItems = [];`
  — stabile gebundene Liste (Kommentar wie bei `DownloadsPage`: stabile
  ItemsSource gegen Ansichts-Reset).
- **Geänderter Konstruktor:** `ItemsCollection.ItemsSource = browserItems;`
  einmalig setzen (nach `InitializeComponent`).
- **Geänderte Methode `UpdateUi`:** zusätzlich
  `SourceItemListSync.Sync(browserItems, browser.Items);` aufrufen.
- **Neuer using-Eintrag:** `System.Collections.ObjectModel`.

### `SourceOverviewPage` (XAML, `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml`)

- **Entfernt:** `ItemsSource="{Binding Model.SourceBrowser.Items}"` an
  `ItemsCollection` (ItemsSource kommt nun aus dem Code-Behind, einmalig).

### `UiDriver` (`VideoPlayer.E2E.Tests/Support/UiDriver.cs`)

- **Neue Methode `ScrollToEnd(string automationId, TimeSpan? timeout)`:** Element
  per AutomationId suchen, im Teilbaum das erste Element mit unterstütztem
  UIA-`ScrollPattern` finden und per `Scroll(ScrollAmount.LargeIncrement, NoAmount)`
  bis zum Ende scrollen (Abbruch, wenn `VerticalScrollPercent` nicht mehr steigt
  oder ~100 erreicht). Fallback ohne `ScrollPattern`: Mausrad-Scroll
  (`FlaUI.Core.Input.Mouse.Scroll`) über dem Elementmittelpunkt.
- **Neue Methode `GetVerticalScrollPercent(string automationId)`:** liefert
  `VerticalScrollPercent` des ersten Elements mit `ScrollPattern` im Teilbaum;
  `double.NaN`, wenn kein Scroll-Provider existiert.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine (keine neuen Eingaben; `Sync` wirft `ArgumentNullException` bei
`null`-Argumenten — gleiche Konvention wie `DownloadListSync`).

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Genre-Leiste / Suchfeld / Fehler- und Leerzustände:** unverändert — sie
  binden nicht an `Items`.
- **Item-Auswahl (`SelectionChanged`):** unverändert; `SelectedItem = null`-Reset
  nach Auswahl bleibt.
- **`ItemsCollection.IsVisible`-Umschalten bei `ShowEmpty`:** unverändert.
- **DevApi `TrySelectItem`/`/tree` `Items`:** liest `ItemsSource` — die bleibt
  weiterhin eine `IEnumerable` der Modell-Einträge (jetzt `ObservableCollection`
  mit identischem Inhalt) → E2E-Pfade (`InvokeAsync("ItemsCollection", i)`)
  funktionieren unverändert.
- **Risiko E2E:** `ScrollPattern` am MAUI-CollectionView-ScrollViewer könnte
  unter Windows nicht exponiert sein → Mausrad-Fallback in `ScrollToEnd`;
  schlägt beides, wird der Test als nicht ausführbar dokumentiert statt
  geschönt.
- **Bekannter gleichgelagerter Defekt außerhalb des Umfangs:**
  `PlaylistDetailsPage` (gleicher Mechanismus) wird nicht geändert —
  Anforderung betrifft nur die Quellen-Übersicht.

## Umsetzungsreihenfolge

1. **`SourceItemListSync` anlegen**
   - Voraussetzungen: keine (nur BCL `ObservableCollection`, `SourceMediaItem` vorhanden)
   - Beschreibung: neue statische Klasse in `VideoPlayer.Mvu` mit `Sync(viewItems, items)`:
     gemeinsames Präfix über `Id` bestimmen; `viewItems` ab Präfix bis zum Ende
     entfernen; `items` ab Präfix anhängen.

2. **`SourceItemListSyncTests` anlegen (Unit-Tests)**
   - Voraussetzungen: Schritt 1
   - Beschreibung: Append-only (Instanzen bleiben, nur `Add`-Ereignisse, kein
     `Reset`/`Remove`), Ersetzen bei abweichendem ersten Eintrag, Schrumpfen
     (kürzere Modell-Liste), unveränderte Liste (keine Ereignisse), leere
     Modell-Liste leert die gebundene Liste.

3. **`SourceOverviewPage` umstellen (XAML + Code-Behind)**
   - Voraussetzungen: Schritt 1 (Kompilierbarkeit)
   - Beschreibung: `ItemsSource`-Binding aus dem XAML entfernen; `browserItems`
     einmalig als `ItemsSource` setzen; `Sync` in `UpdateUi` aufrufen.

4. **`UiDriver` Scroll-Hilfen**
   - Voraussetzungen: keine (nur FlaUI vorhanden)
   - Beschreibung: `ScrollToEnd` (ScrollPattern → LargeIncrement bis Ende;
     Fallback Mausrad) und `GetVerticalScrollPercent` ergänzen.

5. **E2E-Test `SourcePagingTests` anlegen**
   - Voraussetzungen: Schritte 1–4
   - Beschreibung: Mock-Server mit > 40 Einträgen (> 2 Seiten à 20); Quelle
     öffnen; per `ScrollToEnd` ans Ende scrollen → Folgeseite(n) laden;
     `SourceBrowserItemTitles` vollständig prüfen und `VerticalScrollPercent > 0`
     nach dem letzten Append behaupten; Server-Requests `page=1` (und `page=2`)
     protokolliert.

6. **Unit-Testlauf**
   - Voraussetzungen: Schritte 1–3
   - Beschreibung: `dotnet test VideoWebPlayer.Maui.Tests` — alle bisherigen
     plus neuen Tests grün.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Sync_AppendOnly_AddsTail_WithoutReset` | `SourceItemListSyncTests` | Anhängen einer Folgeseite: bestehende Einträge bleiben `Same`, nur `Add`-Ereignisse, kein `Reset`/`Remove` — Kern des Scroll-Erhalt-Nachweises |
| `Sync_ChangedPrefix_ReplacesFromDivergence` | `SourceItemListSyncTests` | Neuladen/Filterwechsel: Liste ab der ersten abweichenden Position ersetzt |
| `Sync_ShorterModel_RemovesTail` | `SourceItemListSyncTests` | Modell kürzer als gebundene Liste → Endabschnitt entfernt |
| `Sync_Unchanged_NoNotifications` | `SourceItemListSyncTests` | Modell-Tick ohne Listenänderung → kein `CollectionChanged` |
| `Sync_EmptyModel_Clears` | `SourceItemListSyncTests` | Leere Modell-Liste (Reset-Pfad) leert die gebundene Liste |
| `Item(id)`-Hilfsmethode | `SourceItemListSyncTests` | `SourceMediaItem`-Testdaten |
| `ScrollToEnd` | `UiDriver` (E2E) | Scrollt einen Container ans Ende (ScrollPattern, Fallback Mausrad) |
| `GetVerticalScrollPercent` | `UiDriver` (E2E) | Liest die vertikale Scrollposition eines Containers |
| `NextPageLoad_KeepsScrollPosition` | `SourcePagingTests` | Pflicht-E2E: nach dem Nachladen bleibt `VerticalScrollPercent > 0` (Liste springt nicht an den Anfang); Einträge wachsen seitenweise an |

### Betroffene bestehende Tests

Keine — kein bestehender Test hängt an der `ItemsSource`-Bindung der Seite
oder am Typ von `SourceBrowserState.Items` (unverändert `IReadOnlyList`).

### E2E-Tests (primärer Funktionsnachweis)

Der Benutzerfluss „Durchscrollen mit automatischem Nachladen" ist der Kern der
Anforderung — Unit-Tests des Sync-Helpers prüfen die Ereignis-Granularität, aber
nur ein E2E-Test belegt, dass die `CollectionView` ihre Position tatsächlich hält.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | An das Ende scrollen → Folgeseite(n) laden → Scrollposition bleibt (nicht Listenanfang) | `VideoPlayer.E2E.Tests/Tests/SourcePagingTests.cs` | „Beim Nachladen einer Folgeseite bleibt die bisherige Scrollposition erhalten; nachgeladene Einträge erscheinen am Ende" | Nur die echte Oberfläche zeigt den Ansichts-Reset bzw. dessen Wegfall |
| Pflicht (abgedeckt im selben Szenario) | Nachgeladene Einträge hängen am Ende, bisherige bleiben stehen | `SourcePagingTests` | „nachgeladene Einträge erscheinen am Ende angehängt" | Modell-Assertion allein prüft nicht die gebundene Ansicht |
| Gilt gleichermaßen für Such-/Genre-Treffer | Implizit abgedeckt: derselbe Anzeige- und Nachladepfad; zusätzlich abgesichert durch die bestehenden E2E-Tests `SourceBrowsingTests`/`SourceSearchTests` (bleiben grün) | `SourcePagingTests` + Bestand | „gleichermaßen für die ungefilterte Liste und für Such-/Genre-Treffer (derselbe Anzeige- und Nachladepfad)" | Gleicher Pfad — ein Pflichtszenario genügt; Genre/Suche ändern nur Effektparameter |
| Erstladen/Genre-Wechsel/neue Suche von Anfang an | Bestehende E2E-Tests `GenreBar_VisibleAndFilters_ResetsOnAll`, `SubmitViaEnter_FiltersFromFirstPage` bleiben grün (Liste zeigt ab Seite 0); keine neuen Szenarien nötig | Bestand | „Erstladen, ein Genre-Wechsel und eine neue Suche zeigen die Liste weiterhin von Anfang an" | Bereits durch Bestandstests des selben Pfads abgesichert |

Bestehende E2E-Tests angepasst: **Keine.**

## Offene Punkte

Keine.
