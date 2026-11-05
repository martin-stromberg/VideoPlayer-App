# Umsetzungsplan: tvOS-App — Browsing und Suche (Schritt 5)

## Übersicht

Der plattformneutrale Quellenbrowser ist im MVU-Kern bereits vollständig
(`AppModel.SourcesOverview`, `AppModel.SourceBrowser`, alle
Messages/Effekte, `SourceBrowserService`). Dieser Schritt verdrahtet ihn
in der tvOS-App: Quellen-Auswahl als Sektion auf der Startseite, eine
modal präsentierte Browsing-Seite (Genre-Leiste, Suchfeld, Poster-Raster
mit Nachladen, Lade-/Leer-/Fehlerzustände) sowie die fehlenden
Effekt-Verdrahtungen im `TvosAppEffectHandler`. Die Eintrags-Auswahl
läuft über das bereits verdrahtete `NavigateToMediaDetailsEffect` auf
das vorbereitete Navigationsziel — kein Schreibzugriff.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Einstieg Quellen-Auswahl | „Quellen"-Sektion auf der Startseite (`SourceCarouselView` aus `AppModel.SourcesOverview`) | `AppRoutes.MenuPages` ist mit der MAUI-App geteilt — ein neuer Tab würde den plattformneutralen Kern ändern; die Startseite ist der Lean-back-Einstieg und trägt bereits modellgetriebene Sektionen |
| Browsing-Seite | `SourceBrowserViewController`, modal Full-Screen durch `TvosNavigator.PresentSourceBrowser` | Etabliertes Muster `PreparedTargetViewController`; der modellgetriebene `MainViewController.Show`-Rahmen verdrängt modale Seiten nicht; Menü-Taste schließt zurück zum Tab-Rahmen |
| Eintrags-Zellen des Rasters | `HomeMediaItemCell` wiederverwenden (`SourceMediaItem` → `HomeMediaItem` abbilden) | Identische Kartenanatomie (Poster, Titel, Fokus); `HomeMediaItem` ist required-minimal (`Id`, `MediaType`, `Title`) — keine doppelte Zellklasse |
| Sucheingabe | `UITextField` (`TvosControls.TextField`) statt `UISearchController` | Systemtastatur mit Diktat und Continuity-Tastatur deckt die Anforderung ab; direkte Kontrolle über den sichtbaren Text (Akzeptanzkriterium „Text bleibt sichtbar"); kein zusätzlicher Results-Controller nötig |
| Such-Trigger | `EditingDidEnd` → `SourceSearchSubmittedMessage`; Render schreibt `Text` nur bei geändertem `browser.SearchText` und nur wenn das Feld nicht editiert wird | Update dedupliziert identische Texte; `mirroredSearchText`-Muster der MAUI-Seite verhindert das bekannte Zurücksetzen des Suchtexts |
| Nachladen | `WillDisplayCell` nahe dem Listenende → `SourceItemsNextPageRequestedMessage` | Update dedupliziert über `IsLoadingMore`/`HasMore` — kein UI-seitiger Schutz nötig |
| Quellen-Icons | `TvosPosterImageService.GetSourceIconAsync` (`api/sourceicons/{id}`, Suffix `.sourceicon`, eigener Speicher-Cache-Schlüsselraum) | Gleiche Cache-Konvention wie Poster; Icons der Quelleneinträge der MAUI-App entsprechend |
| Stapeln der Ziele | `PresentPreparedTarget` präsentiert auf dem obersten präsentierten Controller (`main.PresentedViewController ?? main`) | Eintrags-Auswahl im modalen Browser muss den Platzhalter über dem Browser zeigen können; Rückkehr führt in den Browser zurück |

## Programmabläufe

### Quelle wählen und Browser öffnen

1. `HomePageViewController` rendert `model.SourcesOverview`: bei
   `HasSources` die `SourceCarouselView` mit `SourceItemCell`-Karten;
   bei `IsLoading`/`HasError`/`ShowEmpty` Statustext bzw. Fehlertext mit
   „Erneut versuchen"-Button.
2. Auswahl einer Karte → `SourceSelectedMessage`.
3. `AppUpdate.UpdateSourceSelected` setzt `SourceBrowser = NewBrowser`
   und emittiert `NavigateToSourceOverviewEffect`,
   `LoadSourceGenresEffect`, `LoadSourceItemsEffect(0, Append: false)`.
4. `TvosAppEffectHandler` → `TvosNavigator.PresentSourceBrowser(id, name)`
   → modaler `SourceBrowserViewController` (Full-Screen), der sein
   `Render(model)` ab sofort erhält.
5. `ViewWillAppear` des Browsers → `SourceBrowserOpenedMessage`
   (Wiederherstellungspfad; bei bereits aktivem Browser `Unchanged`).

Beteiligte Klassen: `HomePageViewController`, `SourceCarouselView`,
`SourceItemCell`, `TvosNavigator`, `SourceBrowserViewController`,
`TvosAppEffectHandler`, `SourceBrowserService`.

### Genre wählen

1. Genre-Leiste (horizontale `UICollectionView` mit `SourceGenreCell`)
   zeigt `browser.Genres`, sichtbar nur bei `ShowGenreFilter`;
   `IsSelected` hebt den aktiven Filter hervor.
2. Auswahl → `SourceGenreSelectedMessage` → Update setzt
   `SelectedGenreId` und lädt die Liste ab Seite 0 neu
   (`ReloadBrowserItems`).

### Suchen

1. `UITextField` (Placeholder „Suche") öffnet per Fokus/Auswahl die
   tvOS-Systemtastatur — Diktat (Siri Remote) und Continuity-Tastatur
   (iPhone) systemseitig.
2. `EditingDidEnd` → `SourceSearchSubmittedMessage(field.Text)` →
   Update setzt `SearchText` (getrimmt) und lädt ab Seite 0.
3. `Render` übernimmt `browser.SearchText` in das Feld nur, wenn er sich
   gegenüber dem zuletzt angewendeten Wert geändert hat **und** das Feld
   nicht fokussiert/editiert wird — eingegebener Text bleibt sichtbar.

### Einträge laden und nachladen

1. Raster (`UICollectionView`, vertikales Flow-Layout) zeigt
   `browser.Items` als `HomeMediaItemCell` (Poster via
   `TvosPosterImageService`, Titel-Fallback).
2. `WillDisplayCell` mit `indexPath.Item >= Items.Count - Vorlauf` →
   `SourceItemsNextPageRequestedMessage` (Update guarded doppelt).
3. Zustände: `IsLoading` → Ladeanzeige; `ShowEmpty` → Leer-Text;
   `HasError` → Fehlertext + „Erneut versuchen" →
   `SourceItemsRetryRequestedMessage`; `IsLoadingMore` →
   Nachlade-Hinweis unter dem Raster.

### Eintrag wählen

1. Auswahl → `SourceMediaItemSelectedMessage` → Update emittiert
   `NavigateToMediaDetailsEffect` → `PresentPreparedTarget` auf dem
   obersten präsentierten Controller (über dem Browser).

### Quellen-Neuversuch (Fehler der Quellenliste)

1. `SourcesOverview.HasError` → Fehlertext + „Erneut versuchen" auf der
   Startseiten-Sektion → `SourcesReloadRequestedMessage` →
   `LoadSourcesEffect` → `TvosAppEffectHandler.LoadSourcesAsync` →
   `SourcesLoadedMessage`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `SourceBrowserViewController` | `UIViewController, IModelRenderable` (`VideoPlayer.TvOS/Ui/`) | Modale Browsing-Seite: Titel, Suchfeld, Genre-Leiste, Eintrags-Raster, Lade-/Leer-/Fehlerzustände, Menü-Taste schließt |
| `SourceGenreCell` | `UICollectionViewCell` (`VideoPlayer.TvOS/Ui/`) | Fokussierbare Genre-Kachel mit sichtbarem Auswahlzustand |
| `SourceCarouselView` | `UIView` (`VideoPlayer.TvOS/Ui/`) | Horizontale Quellen-Reihe der Startseite (Muster `HomeCarouselView`) |
| `SourceItemCell` | `UICollectionViewCell` (`VideoPlayer.TvOS/Ui/`) | Fokussierbare Quellen-Karte (Icon/Name) |
| `TvOsBrowsingContractTests` | Testklasse (`VideoPlayer.Maui.Tests/`) | Quell-Contract-Tests der neuen tvOS-Dateien und Verdrahtung |

## Änderungen an bestehenden Klassen

### `HomePageViewController` (`UIViewController`)

- **Neue Felder:** `sourcesSection` (`UIStackView`), `sourcesStatus`
  (`UILabel`), `sourcesRetry` (`UIButton`), `sourcesCarousel`
  (`SourceCarouselView`)
- **Geänderte Methoden:** `ApplyRender` — Quellen-Sektion aus
  `model.SourcesOverview` befüllen (Karten / Lade- / Leer- /
  Fehlerzustand mit Neuversuch)
- **Neue Methoden:** `OnSourceSelected(SourceItem)` →
  `SourceSelectedMessage`; `OnSourcesRetry` →
  `SourcesReloadRequestedMessage`

### `TvosNavigator` (Klasse)

- **Neue Felder:** `sourceBrowserPage` (`SourceBrowserViewController?`)
- **Neue Methoden:** `PresentSourceBrowser(long sourceId, string
  sourceName)` — modale Full-Screen-Präsentation (Instanz wiederverwenden
  und Quellen-Kontext setzen; bereits präsentiert → nicht stapeln)
- **Geänderte Methoden:** `Render` — Modell zusätzlich an
  `sourceBrowserPage` verteilen; `PresentPreparedTarget` —
  Präsentationsanker auf `main.PresentedViewController ?? main`

### `TvosAppEffectHandler` (`IAppEffectHandler`)

- **Neue Switch-Fälle:** `LoadSourcesEffect` → `LoadSourcesAsync`;
  `LoadSourceGenresEffect` → `LoadSourceGenresAsync`;
  `LoadSourceItemsEffect` → `LoadSourceItemsAsync`;
  `NavigateToSourceOverviewEffect` → `navigator.PresentSourceBrowser`
  (über `MainThreadInvoker.InvokeOnMainThreadAsync`)
- **Neue Methoden:** `LoadSourcesAsync`, `LoadSourceGenresAsync`,
  `LoadSourceItemsAsync` — 1:1-Muster des `ShellAppEffectHandler`
  (gleiche Fehlertexte)

### `AppComposition` (Kompositionswurzel)

- **Neue Eigenschaft:** `SourceBrowser` (`SourceBrowserService`) —
  Singleton, `new SourceBrowserService(AuthSession)`

### `TvosPosterImageService` (Dienst)

- **Neue Methode:** `GetSourceIconAsync(long pictureId, CancellationToken)`
  → `UIImage?` — `api/sourceicons/{id}` via `client.GetSourceIconAsync`,
  Datei `{id}.sourceicon`, eigene Speicher-Map (kein Schlüsselkonflikt
  mit Postern)

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — Eingaben laufen ausschließlich über bestehende MVU-Messages
(Trimmen des Suchtexts im Update; `Id`-Guards vorhanden).

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Startseite:** neue Sektion verändert die Sektionsliste — das
  `appliedKeys`-Neuaufbaumuster bleibt unberührt, da die Quellen-Sektion
  außerhalb der Karussell-Deskriptoren liegt.
- **`PresentPreparedTarget`-Anker:** Wechsel auf den obersten
  präsentierten Controller ist verhaltensneutral, solange kein Browser
  präsentiert ist (dann `null` → `main` wie bisher).
- **Modale Folge:** Browser (modal) → PreparedTarget (modal darüber) —
  Menü-Taste schließt jeweils die oberste Seite; beide Seiten besitzen
  `PressesBegan`-Handling.
- **`MenuTabBarController.Render`** verteilt weiter an alle Tab-Kinder;
  die Browser-Seite ist kein Tab-Kind und wird separat vom Navigator
  bedient.
- **Kein Schreibzugriff:** es werden nur Lese-Effekte verdrahtet;
  Mutations-Messages/-Effekte bleiben unverdrahtete No-Ops (durch
  bestehenden Contract-Test `TvOsEffectHandler_DoesNotWireContentMutations`
  abgesichert).

## Umsetzungsreihenfolge

1. **`TvosPosterImageService.GetSourceIconAsync` + `SourceItemCell` + `SourceCarouselView`**
   - Voraussetzungen: `VideoPlayerApiClient.GetSourceIconAsync` (vorhanden),
     `TvosControls`, `HomeMediaItemCell`-Muster (vorhanden)
   - Beschreibung: Icon-Ladepfad und Quellen-Reihe anlegen.

2. **`HomePageViewController`: Quellen-Sektion**
   - Voraussetzungen: Schritt 1
   - Beschreibung: Sektion „Quellen" aus `model.SourcesOverview` rendern
     (Karten/Lade/Leer/Fehler + Neuversuch); Auswahl →
     `SourceSelectedMessage`, Retry → `SourcesReloadRequestedMessage`.

3. **`SourceGenreCell` + `SourceBrowserViewController`**
   - Voraussetzungen: `HomeMediaItemCell` (Wiederverwendung), `TvosControls`
   - Beschreibung: Modale Browser-Seite mit Genre-Leiste, Suchfeld
     (`mirroredSearchText`-Schutz), Raster mit `WillDisplayCell`-Nachladen,
     Zustandsanzeigen, `PressesBegan`-Menü-Rückkehr,
     `SourceBrowserOpenedMessage` beim Erscheinen.

4. **`TvosNavigator`: `PresentSourceBrowser` + Render-Verteilung + Anker**
   - Voraussetzungen: Schritt 3
   - Beschreibung: `PresentSourceBrowser`, `sourceBrowserPage`-Referenz,
     `Render`-Verteilung, `PresentPreparedTarget`-Anker auf obersten
     präsentierten Controller.

5. **`AppComposition` + `TvosAppEffectHandler`: Effekt-Verdrahtung**
   - Voraussetzungen: Schritt 4; `SourceBrowserService` (plattformneutral
     vorhanden)
   - Beschreibung: `SourceBrowser`-Singleton; vier Switch-Fälle +
     Lademethoden nach `ShellAppEffectHandler`-Muster.

6. **`TvOsBrowsingContractTests`**
   - Voraussetzungen: Schritte 1–5
   - Beschreibung: Quell-Contract-Tests (Existenz/Verdrahtung/Messages/
     Zustände/Kein-Schreibzugriff) — `dotnet test VideoPlayer.Maui.Tests`
     muss grün bleiben.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `SourceBrowserPage_RendersGridGenresAndSearchFromModel` | `TvOsBrowsingContractTests` | `SourceBrowserViewController`: `IModelRenderable`, `UICollectionView`, `UITextField`, Genre-Leiste, alle Browser-Messages |
| `SourceBrowserPage_SearchTextStaysVisibleWhileEditing` | `TvOsBrowsingContractTests` | `mirroredSearchText`/Bearbeitungs-Guard gegen Suchtext-Reset |
| `SourceBrowserPage_StatesAndRetryAreRemoteFriendly` | `TvOsBrowsingContractTests` | `IsLoading`, `ShowEmpty`, `HasError`, `IsLoadingMore`, `SourceItemsRetryRequestedMessage`, Menü-Rückkehr |
| `SourceBrowserPage_LoadsNextPageAtListEnd` | `TvOsBrowsingContractTests` | `WillDisplayCell` → `SourceItemsNextPageRequestedMessage` |
| `HomePage_ShowsSourceSection` | `TvOsBrowsingContractTests` | `SourceCarouselView`, `SourceItemCell`, `SourceSelectedMessage`, `SourcesReloadRequestedMessage` |
| `SourceCells_AreFocusableCards` | `TvOsBrowsingContractTests` | `SourceItemCell`/`SourceGenreCell`: `UICollectionViewCell`, `CanBecomeFocused`, `ReuseId`, `RegisterClassForCell` |
| `TvOsBrowsing_WiringIsComplete` | `TvOsBrowsingContractTests` | `AppComposition.SourceBrowser`, Effekt-Fälle im `TvosAppEffectHandler`, `PresentSourceBrowser` im Navigator |
| `TvOsEffectHandler_DoesNotWireContentMutations` | bestehend (`TvOsHomePageContractTests`) | Bleibt grün — keine neuen Mutations-Verdrahtungen |

### Betroffene bestehende Tests

Keine — es werden ausschließlich neue Dateien und additive Verdrahtung
eingeführt; `TvOsCompileContractTests` prüfen automatisch auch die neuen
Dateien mit.

### E2E-Tests (Pflicht)

Für tvOS existiert keine E2E-Infrastruktur (FLAUI/E2E-Suite deckt die
Windows-App ab; tvOS läuft nur im macOS-CI/Simulator). Der Nachweis der
Benutzerabläufe erfolgt über die Quell-Contract-Tests (Verdrahtung aller
Interaktionen: Quellen-/Genre-/Eintrags-Auswahl, Suche, Nachladen,
Retry) plus den `build-tvos`-Kompiliernachweis im CI — Vorgehen der
Vorschritte.

## Offene Punkte

Keine.
