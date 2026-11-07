# Plan-Review

Geprüft: `plan.md` gegen die Implementierung (Selbstprüfung ohne Unteragenten —
dokumentierte Abweichung, es stehen keine Unteragenten zur Verfügung).
Alle Planelemente wurden im Code gesucht und vollständig gelesen.

## Ergebnis

**Status:** Vollständig umgesetzt (mit einer dokumentierten,
plan-konformen Verfeinerung)

## Umgesetzte Planelemente

### Projekt `VideoPlayer.Mvu` (net10.0)

- [x] `HomeMediaItem` — `Id`, `MediaType`, `Title`, `PosterPictureId`,
  `Progress` (0–1, nur Weiterschauen), plus `HasPoster`/`ShowProgress`.
- [x] `HomeLists` — `ContinueWatching`, `Favorites`, `Recent`, `Empty`.
- [x] `HomeListService` — liest `cached_lists` (`continue_watching`,
  `recent`) und `cached_favorites`; defensives JSON-Lesen (beschädigte
  Einträge → leere Teilliste); Einträge ohne identifizierbares Medium
  werden übersprungen; Medientyp wie geplant (Server-Typ der
  Weiterschauen-Liste, sonst CLR-Typname kleingeschrieben).
- [x] `AppModel` — `ContinueWatching`, `Favorites`, `RecentEntries` plus
  `HasContinueWatching`, `HasFavorites`, `HasRecentEntries`,
  `HasHomeContent`.
- [x] Messages — `HomeListsLoadedMessage`, `CachedListsChangedMessage`,
  `MediaItemSelectedMessage`.
- [x] Effekte — `LoadHomeListsEffect`, `NavigateToMediaDetailsEffect`
  (`MediaType`, `MediaId`, `Title`).
- [x] `AppUpdate` — Start-Workflow enthält `LoadHomeListsEffect` nach
  `InitializeStorageEffect`; `SynchronizationCompletedMessage` →
  `LoadHomeListsEffect`; `CachedListsChangedMessage` →
  `LoadHomeListsEffect`; `HomeListsLoadedMessage` übernimmt die Listen;
  `MediaItemSelectedMessage` → Navigation nur bei identifizierbarem
  Medium (`Id > 0`, `MediaType` nicht leer).

### Projekt `VideoPlayer.Maui`

- [x] `ShellAppEffectHandler` — `LoadHomeListsEffect` →
  `HomeListService` → `HomeListsLoadedMessage` (Fehler → `HomeLists.Empty`);
  nach jeder Teil-Synchronisation `CachedListsChangedMessage`-Rückdispatch;
  `NavigateToMediaDetailsEffect` → Shell-Route `mediadetails` auf dem
  Hauptthread.
- [x] `PosterImageService` — `api/pictures/{id}` via
  `GetPictureAsync`, Speicherung in `AppStorageLayout.PreviewsDirectory`
  (nicht backup-pflichtig), zusätzlich In-Memory-Cache.
- [x] `CachedPosterImage` — `BindableProperty PictureId`, Auflösung über
  `PosterImageService`, verwirft nachgereichte Ergebnisse.
- [x] `MediaCarousel` (ContentView) — Titel + horizontale Kartenliste
  (`BindableLayout`), Poster/Titel-Fallback, Fortschrittsbalken,
  `ItemTapped`-Ereignis; blendet sich bei leerer Liste aus.
- [x] `HomePage.xaml(.cs)` — drei Abschnitte „Weiterschauen", „Favoriten",
  „Neu im Programm"; Offline-Banner und Serverstatus erhalten; Leer-
  Hinweis bei komplett leeren Listen; `ItemTapped` →
  `MediaItemSelectedMessage`.
- [x] `MediaDetailsPage` — Platzhalter-Detailseite mit
  `QueryProperty` `mediaType`/`id`/`title`, deutsch übersetztem
  Medientyp (beide Typkonventionen) und Hinweis auf Schritt 8.
- [x] `AppShell` — Route `mediadetails` registriert (Push-Route ohne
  Menüeintrag).
- [x] `MauiProgram` — `HomeListService`, `PosterImageService`,
  `MediaDetailsPage` registriert.

### Tests

- [x] `AppUpdateTests_HomeLists` — Start-Effekt-Sequenz, Übernahme der
  Listen, `CachedListsChanged` → Neu-Laden, Ereignis→Teil-Sync→Neu-Laden-
  Kette, Auswahl → `NavigateToMediaDetailsEffect`, Guard bei
  ungültigem Eintrag.
- [x] `HomeListServiceTests` — realer SQLite-Roundtrip für alle drei
  Quellen, Mapping (Fortschritt, Poster-Fallback, Medientypen),
  Überspringen nicht identifizierbarer Einträge, beschädigtes JSON,
  Wieder-Lesen ohne Server (Cache-First).
- [x] `HomePageContractTests` — drei Listen/Titel, horizontale
  Orientierung, Poster-Cache-Control, Tap-Geste, Dispatch der Auswahl,
  Route-Registrierung, Effekt-Handler-Verdrahtung, DI-Registrierungen,
  Ablage der Vorschaubilder im nicht backup-pflichtigen Verzeichnis.
- [x] Bestandstests angepasst — `MvuHostTests` (10 Effekte),
  `AppUpdateTests_Startup` (5 Effekte),
  `AppUpdateTests_NetworkSync.SynchronizationCompleted_ClearsSyncFlag`
  (erwartet `LoadHomeListsEffect`).

## Abweichung vom Plan

- Die drei Listenabschnitte sind nicht dreifach in `HomePage.xaml`
  dupliziert, sondern als wiederverwendbares Control
  `Controls/MediaCarousel` (analog zum `MediaCarousel`-Muster der alten
  App) umgesetzt — gleiche Datenbindung, gleiches Verhalten, weniger
  Duplikation. Abdeckung über `HomePageContractTests` angepasst.

## Hinweise

- `RaiseUiActionRequested`-Prüfung: nicht anwendbar — das Pattern
  existiert nur im Blazor-/Altbestand, die neue App nutzt MAUI-Pages mit
  `MvuHost`-Bindungen. Geprüft: alle neuen UI-Aktionen laufen über
  `DispatchAsync` (kein unbehandelter UI-Aktionspfad).
