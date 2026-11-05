# Bestandsaufnahme: tvOS-App — Browsing und Suche (Schritt 5)

Analysiert wurden der MVU-Quellenbrowser (Messages, Effekte, Zustand,
Ladedienst), die tvOS-UI-Infrastruktur (Navigator, Tab-Rahmen,
Karussell-/Zell-Muster, Effekt-Handler) und die MAUI-Referenzimplementierung
der Browsing-Seite — bezogen auf die Lean-back-Umsetzung des Browsing- und
Suchfunktionsumfangs.

## Zusammenfassung

- **Plattformneutraler Kern vollständig vorhanden:** `AppModel.SourcesOverview`
  (Quellenliste mit Lade-/Fehlerzustand) und `AppModel.SourceBrowser`
  (geöffnete Quelle, Genres, Suchtext, paginierte Einträge,
  Lade-/Fehlerflags) samt allen Messages/Effekten und `AppUpdate`-Handlern
  — keinerlei MVU-Änderung nötig.
- **`SourceBrowserService`** kapselt `api/Sources`, `api/SourceGenres/{id}`
  (inkl. synthetischem „Alle") und `api/items` (Paging/Suche/Genre) — nur
  lesend, noch nicht in `AppComposition` der tvOS-App verdrahtet.
- **tvOS-Lücken:** `TvosAppEffectHandler` bildet `LoadSourcesEffect`,
  `LoadSourceGenresEffect`, `LoadSourceItemsEffect` und
  `NavigateToSourceOverviewEffect` derzeit auf das protokollierte No-Op
  ab; es gibt keine Quellen-Auswahl in der UI und keine Browsing-Seite.
- **Wiederverwendbare tvOS-Muster vorhanden:** `MenuTabBarController`
  (`IModelRenderable`-Verteilung), `TvosNavigator` (modale Präsentation
  via `PresentPreparedTarget`), `HomeCarouselView`/`HomeMediaItemCell`
  (fokussierbare Poster-Karten), `TvosControls.TextField` (Systemtastatur
  mit Diktat/Continuity), `TvosPosterImageService` (`api/pictures/{id}`
  mit Speicher-/Datei-Cache).
- **`NavigateToMediaDetailsEffect`** ist bereits auf
  `PresentPreparedTarget` verdrahtet — die Eintrags-Auswahl des Browsers
  landet ohne Zusatzaufwand auf dem vorbereiteten Navigationsziel;
  Präsentation muss aber auf dem obersten Controller stapeln können
  (Browser ist modal).

## Details

- [Datenmodell](inventory/models.md) — `SourceItem`, `SourceGenreItem`,
  `SourceMediaItem`, `SourceBrowserState`, `SourcesOverviewState`,
  `HomeMediaItem`
- [Logik](inventory/logic.md) — `SourceBrowserService`,
  `AppUpdate`-Handler, `ShellAppEffectHandler`-Referenz,
  `TvosAppEffectHandler`, `TvosNavigator`, `MenuTabBarController`,
  `TvosPosterImageService`, `VideoPlayerApiClient`
- [UI-Bausteine](inventory/ui.md) — `HomePageViewController`,
  `HomeCarouselView`, `HomeMediaItemCell`, `TvosControls`,
  `PreparedTargetViewController`, `PagePlaceholderViewController`,
  `IModelRenderable`, MAUI-`SourceOverviewPage`
- [Tests](inventory/tests.md) — `TvOsHomePageContractTests`,
  `TvOsCompileContractTests`, `TvOsConnectionContractTests`,
  bekannte tvOS-Kompilierfallen
