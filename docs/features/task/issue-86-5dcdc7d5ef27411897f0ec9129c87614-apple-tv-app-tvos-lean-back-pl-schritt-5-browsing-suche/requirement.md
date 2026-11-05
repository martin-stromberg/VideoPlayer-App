# Übersetzte Anforderung: tvOS-App — Browsing und Suche (Schritt 5)

## Fachliche Zusammenfassung

Die tvOS-App `VideoPlayer.TvOS` erhält den bestehenden, plattformneutral
bereits vollständig im MVU-Kern implementierten Quellen-Browser als
Lean-back-Oberfläche: Auswahl einer freigegebenen Medienquelle,
Genre-Filterleiste inklusive synthetischem „Alle"-Eintrag, seitenweise
nachladendes Poster-Raster der `SourceMediaItem`-Einträge sowie eine
Suche über `SourceSearchSubmittedMessage` — Eingabe per tvOS-Systemtastatur
(Diktat/Continuity-Tastatur). Eintrags-Auswahl führt über
`SourceMediaItemSelectedMessage`/`NavigateToMediaDetailsEffect` auf das
bereits vorhandene vorbereitete Navigationsziel
(`PreparedTargetViewController`). Keine Schreibzugriffe auf Inhalte.

## Betroffene Klassen und Komponenten

### Neu (tvOS-Projekt, `VideoPlayer.TvOS/Ui/`)

- `SourceBrowserViewController` — modale Browsing-Seite einer geöffneten
  Quelle (`IModelRenderable`): Titel, Suchfeld (`UITextField`),
  Genre-Leiste (horizontale `UICollectionView`), Eintrags-Raster
  (vertikale `UICollectionView` mit Nachladen am Listenende),
  Lade-/Leer-/Fehlerzustände mit Wiederhol-Button; Menü-Taste schließt
  die Seite.
- `SourceGenreCell` — fokussierbare Genre-Kachel der Filterleiste
  (Auswahlzustand sichtbar).
- `SourceCarouselView` — horizontale Quellen-Reihe auf der Startseite
  (Pendant zu `HomeCarouselView` für `SourceItem`).
- `SourceItemCell` — fokussierbare Quellen-Karte (Name, optionales
  Server-Icon über `api/sourceicons`).

### Zu ändern (tvOS-Projekt)

- `VideoPlayer.TvOS/AppComposition.cs` — `SourceBrowserService` als
  Singleton ergänzen (Kompositionswurzel).
- `VideoPlayer.TvOS/Services/TvosAppEffectHandler.cs` — Verdrahtung der
  Effekte `LoadSourcesEffect`, `LoadSourceGenresEffect`,
  `LoadSourceItemsEffect`, `NavigateToSourceOverviewEffect` (Pendant zu
  `ShellAppEffectHandler`).
- `VideoPlayer.TvOS/Ui/TvosNavigator.cs` — `PresentSourceBrowser`
  (modal, aktualisiert statt stapelt); `Render` verteilt das Modell an
  die Browser-Seite; `PresentPreparedTarget` präsentiert auf dem
  obersten präsentierten Controller (stapelt über dem Browser).
- `VideoPlayer.TvOS/Ui/HomePageViewController.cs` — Quellen-Sektion
  („Quellen") mit `SourceCarouselView` samt Lade-/Fehler-/Leerzustand
  und Neuversuch (`SourcesReloadRequestedMessage`).
- `VideoPlayer.TvOS/Services/TvosPosterImageService.cs` —
  `GetSourceIconAsync` für `api/sourceicons/{id}` (gleiche
  Cache-Konvention, eigener Datei-Suffix/Schlüsselraum).

### Plattformneutral (keine Änderung — bereits vorhanden)

- `VideoPlayer.Mvu`: `SourceBrowserState`, `SourceBrowserService`,
  `SourceItem`, `SourceGenreItem`, `SourceMediaItem`,
  `SourcesOverviewState`, Messages (`SourceSelectedMessage`,
  `SourceBrowserOpenedMessage`, `SourceGenreSelectedMessage`,
  `SourceSearchSubmittedMessage`, `SourceItemsNextPageRequestedMessage`,
  `SourceItemsRetryRequestedMessage`, `SourceMediaItemSelectedMessage`,
  `SourcesReloadRequestedMessage`) und Effekte (`LoadSourcesEffect`,
  `LoadSourceGenresEffect`, `LoadSourceItemsEffect`,
  `NavigateToSourceOverviewEffect`, `NavigateToMediaDetailsEffect`).

### Tests

- `VideoPlayer.Maui.Tests/TvOsBrowsingContractTests.cs` — Quell-Contract-
  Tests für die neuen tvOS-Dateien und die Verdrahtung (Muster:
  `TvOsHomePageContractTests`).

## Implementierungsansatz

- Reine UI-/Rand-Verdrahtung im tvOS-Projekt: Der MVU-Kern liefert
  `AppModel.SourcesOverview` (Quellenliste) und `AppModel.SourceBrowser`
  (Browser-Zustand) vollständig — die tvOS-Seite konsumiert beide über
  `IModelRenderable.Render` und dispatcht die vorhandenen Messages.
- Quellen-Auswahl als eigene Sektion auf der Startseite (Menüstruktur
  `AppRoutes.MenuPages` bleibt unverändert; `SourceSelectedMessage`
  löst Navigation + Laden aus).
- `NavigateToSourceOverviewEffect` wird auf eine modale
  Full-Screen-Präsentation des `SourceBrowserViewController` abgebildet
  (Muster: `PreparedTargetViewController`); `SourceBrowserOpenedMessage`
  beim Erscheinen deckt den Wiederherstellungspfad des Updates ab.
- Suche über `UITextField` (tvOS-Systemtastatur mit Diktat;
  Continuity-Tastatur systemseitig); Absenden bei `EditingDidEnd` →
  `SourceSearchSubmittedMessage`. Der eingegebene Text bleibt sichtbar:
  Render schreibt das Feld nur zurück, wenn es nicht bearbeitet wird
  und sich der Modell-Suchtext geändert hat.
- Nachladen über `WillDisplayCell` nahe dem Listenende →
  `SourceItemsNextPageRequestedMessage` (Update dedupliziert über
  `IsLoadingMore`/`HasMore`).
- Lade-/Leer-/Fehlerzustände aus `SourceBrowserState.IsLoading`,
  `ShowEmpty`, `HasError` + `IsLoadingMore`; Fehler mit Wiederhol-Button
  → `SourceItemsRetryRequestedMessage`.

## Konfiguration

Keine — keine neuen Einstellungen oder Konfigurationseinträge.

## Offene Fragen

Keine — alle Interaktionen sind durch den bestehenden MVU-Zyklus und
die etablierten tvOS-Muster (modal präsentierte Seiten, Fokus-Engine,
`TvosControls`) eindeutig bestimmt.
