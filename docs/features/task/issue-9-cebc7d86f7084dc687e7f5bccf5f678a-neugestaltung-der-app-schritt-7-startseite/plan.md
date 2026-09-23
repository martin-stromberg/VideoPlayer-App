# Umsetzungsplan: Startseite mit gecachten Medienlisten

## Übersicht

Die Startseite (`HomePage`, `AppPage.Home`) zeigt die drei horizontalen
Medienlisten „Weiterschauen", „Favoriten" und „Neu im Programm". Die Listen
werden aus der `CacheDatabase` gelesen (Schlüssel `continue_watching` /
`recent` in `cached_lists` sowie `cached_favorites` — Befüllung durch
`ServerSyncService` bereits vorhanden) und als anzeigefertige Einträge im
`AppModel` geführt. Geladen wird Cache-First im Start-Workflow sowie nach
jeder (Teil-)Synchronisation, sodass Server-Ereignisse die Anzeige ohne
manuelle Aktualisierung frisch halten. Der Tap auf einen Eintrag navigiert
über eine neue Shell-Route `mediadetails` auf eine Platzhalter-Detailseite
(Medientyp + Id + Titel als Query-Parameter; Schritt 8 füllt sie).

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|------------------|------------|
| Anzeige-Modell der Listen | Neuer plattformneutraler Record `HomeMediaItem` in `VideoPlayer.Mvu` (`Id`, `MediaType`, `Title`, `PosterPictureId`, `Progress` 0–1 oder `null`); drei `IReadOnlyList<HomeMediaItem>` im `AppModel` plus berechnete `Has…`-Flags | Der MVU-Kern bleibt frei von MAUI-Typen (kein `ImageSource` im Model); Sichtbarkeit leerer Sektionen über berechnete Flags statt über einen Konverter. `Progress` gehört fachlich zur Weiterschauen-Liste (Positionsbalken auf der Karte). |
| Lesen der Caches | Neuer Service `HomeListService` in `VideoPlayer.Mvu` auf `CacheDatabase` — deserialisiert `cached_lists`/`cached_favorites` und mappt auf `HomeMediaItem` (`LoadHomeListsAsync` → `HomeLists`-Ergebnis-Record) | Gleiche Schicht/Konvention wie `ServerSyncService` (Mvu nutzt Storage + ApiClient-Modelle); plattformneutral testbar ohne MAUI. |
| Cache-First-Befüllung | `LoadHomeListsEffect` wird der Effektliste von `StartupRequestedMessage` angehängt (nach `InitializeStorageEffect`); der Handler liest den Cache und dispatcht `HomeListsLoadedMessage` | „Bei erneutem Öffnen erscheinen zunächst die gecachten Inhalte" — die Daten liegen im Model, bevor die Startseite sichtbar wird, und gelten auch im Offline-Zustand. |
| Aktualisierung nach Sync/Ereignis | `SynchronizationCompletedMessage` → `LoadHomeListsEffect`; der Effekt-Handler dispatcht nach jeder Teil-Synchronisation zusätzlich `CachedListsChangedMessage` → `LoadHomeListsEffect` | Die vorhandene Ereignisverdrahtung (Schritt 6) bleibt unverändert; die Rückmeldung schließt die Lücke „Cache aktualisiert → Anzeige aktualisiert" ohne Polling. |
| Auswahl → Detailseite | `MediaItemSelectedMessage(HomeMediaItem)` → `NavigateToMediaDetailsEffect(MediaType, MediaId, Title)`; Handler navigiert per `Shell.Current.GoToAsync("mediadetails?mediaType=…&id=…&title=…")` auf die neue Platzhalterseite `MediaDetailsPage` | Entspricht dem Routen-Muster von `serversetup`/`login` (kein Menüeintrag, `Routing.RegisterRoute`, kein `AppPage`-Eintrag — `AppRoutes.TryGetPage` ignoriert die Route, `CurrentPage` bleibt `Home`). Titel wird mitgegeben, damit die Platzhalterseite keinen internen Schlüssel anzeigt. |
| Medientyp der Einträge | `MediaType` = serverseitiger `ContinueWatchingEntry.MediaType`, sonst CLR-Typname kleingeschrieben (`movie`, `moviecollection`, `tvshow`, `tvshowseason`, `tvshowepisode` — Konvention `ServerSyncService.MediaTypeOf`) | Konsistent mit den `cached_favorites`-Schlüsseln und den `MediaTypes`-Konstanten des Altbestands; direkte Grundlage für die Detail-Routen-Auflösung in Schritt 8. |
| Listen-UI | Je Sektion: Titel-`Label` + horizontaler `ScrollView` mit `BindableLayout.ItemsSource` (Muster `MediaCarousel` des Altbestands/Webanwendung); Karte = `Border` 140×210, `RoundRectangle 8`, `Shadow`, Hintergrund `#23232a`, `TapGestureRecognizer`; Poster via `CachedPosterImage`, ohne Bild ein Titel-`Label` als Fallback; Weiterschauen-Karten mit schmalem `ProgressBar` unten; Sektion unsichtbar bei leerer Liste | Direkte Übernahme des etablierten UI-Musters der bestehenden Anwendung; `BindableLayout` reicht ohne neue Paketabhängigkeit (kein CommunityToolkit im neuen Projekt). |
| Vorschaubilder | `PosterImageService` (`VideoPlayer.Maui/Services`): Datei-Cache in `AppStorageLayout.PreviewsDirectory` (`{id}.poster`), sonst `AuthSessionService.Client.GetPictureAsync` → Datei schreiben → `ImageSource`; dazu `Controls/CachedPosterImage : Image` mit `PictureId`-BindableProperty (Dienst via `IPlatformApplication.Current.Services`) | `api/pictures/{id}` ist JWT-geschützt — kein direkter URL-`ImageSource` möglich; `PreviewsDirectory` ist der vorgesehene, nicht backup-pflichtige Ablageort. Fehler → kein Bild (nur Log), Fallback-Label bleibt sichtbar. |

## Programmabläufe

### App-Start mit Cache-First (Akzeptanzkriterium 2)

1. `StartupRequestedMessage` → Effekte `[InitializeStorageEffect,
   LoadSettingsEffect, LoadServersEffect, StartNetworkMonitoringEffect,
   LoadHomeListsEffect]` (werden sequenziell ausgeführt — die
   Cache-Datenbank ist beim Lesen initialisiert).
2. `ShellAppEffectHandler` → `HomeListService.LoadHomeListsAsync` →
   Rückdispatch `HomeListsLoadedMessage(lists)`.
3. `AppUpdate` setzt `ContinueWatching`/`Favorites`/`RecentEntries` im
   Model — die Startseite zeigt beim ersten Sichtbarwerden den gecachten
   Stand (auch im Offline-Zustand, unabhängig von der Serverantwort).

### Aktualisierung durch Serverdaten (Akzeptanzkriterien 1 + 3)

1. Voll-Sync: `RunFullSyncEffect` → `ServerSyncService.SynchronizeAsync`
   → `SynchronizationCompletedMessage` → `AppUpdate` hängt
   `LoadHomeListsEffect` an → Model-Listen werden aus dem frischen Cache
   neu gesetzt.
2. Server-Ereignis: `ServerEventReceivedMessage` → (Bestand)
   `SyncContinueWatchingEffect`/`SyncFavoritesEffect`/`SyncMetadataEffect`
   → Handler führt Teil-Sync aus und dispatcht danach
   `CachedListsChangedMessage` → `LoadHomeListsEffect` →
   `HomeListsLoadedMessage` → Anzeige aktualisiert ohne manuelle
   Aktualisierung.
3. Fehlschlag der Teil-Synchronisation ändert nichts am Verhalten: Die
   Nachladen-Message wird trotzdem dispatcht (der Cache enthält dann den
   unveränderten Bestand — kein Datenverlust, keine Fehleranzeige nötig).

### Auswahl eines Eintrags (Akzeptanzkriterium 4)

1. Tap auf eine Karte → `HomePage.OnMediaItemTapped` →
   `MediaItemSelectedMessage(item)`.
2. `AppUpdate` → `NavigateToMediaDetailsEffect(item.MediaType, item.Id,
   item.Title)`.
3. Handler → `Shell.Current.GoToAsync("mediadetails?mediaType=…&id=…&title=…")`
   (URI-escaped) → die auf den Navigationsstack geschobene
   `MediaDetailsPage` zeigt Titel und Medientyp-Bezeichnung sowie den
   Platzhalterhinweis. `CurrentPage` bleibt `Home` (Route ohne
   `AppPage`-Zuordnung; Zurück-Navigation der Shell führt zur Startseite
   zurück).

### Bildanzeige eines Eintrags

1. `CachedPosterImage.PictureId` wird per Binding gesetzt →
   `PosterImageService.GetImageSourceAsync(id)`.
2. Vorhandene Datei `previews/{id}.poster` → `ImageSource.FromFile`
   (funktioniert offline).
3. Sonst mit angemeldetem Client: `GetPictureAsync` → Bytes in die
   Cache-Datei schreiben → `ImageSource` aus Bytes. Ohne Client/Fehler:
   `null` → Titel-Fallback bleibt sichtbar.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `HomeMediaItem` | Datenmodell-Record (`VideoPlayer.Mvu`) | Anzeigeeintrag: `Id`, `MediaType`, `Title`, `PosterPictureId`, `Progress` (`double?`, 0–1), berechnetes `ShowProgress` |
| `HomeLists` | Datenmodell-Record (`VideoPlayer.Mvu`) | Ergebnisbündel: `ContinueWatching`, `Favorites`, `Recent` |
| `HomeListService` | Klasse (`VideoPlayer.Mvu`) | Liest `cached_lists`/`cached_favorites` aus `CacheDatabase` und mappt auf `HomeLists` |
| `LoadHomeListsEffect` | `AppEffect`-Record (`VideoPlayer.Mvu`) | Gecachte Listen lesen (Ergebnis: `HomeListsLoadedMessage`) |
| `NavigateToMediaDetailsEffect` | `AppEffect`-Record (`VideoPlayer.Mvu`) | Navigation zur Detail-Route (`MediaType`, `MediaId`, `Title`) |
| `HomeListsLoadedMessage` | `AppMessage`-Record (`VideoPlayer.Mvu`) | Geladene Listen ins Model übernehmen |
| `CachedListsChangedMessage` | `AppMessage`-Record (`VideoPlayer.Mvu`) | Cache-Inhalte wurden durch Teil-Sync geändert → Listen neu laden |
| `MediaItemSelectedMessage` | `AppMessage`-Record (`VideoPlayer.Mvu`) | Eintrag auf der Startseite ausgewählt |
| `PosterImageService` | Klasse (`VideoPlayer.Maui/Services`) | Bild-Bytes über den angemeldeten Client laden, Datei-Cache in `PreviewsDirectory`, liefert `ImageSource?` |
| `CachedPosterImage` | Klasse (`VideoPlayer.Maui/Controls`, erbt `Image`) | `PictureId`-BindableProperty, löst `ImageSource` asynchron über `PosterImageService` auf |
| `MediaDetailsPage` | `ContentPage` (`VideoPlayer.Maui/Pages`) | Platzhalter-Detailseite (Query-Properties `mediaType`, `id`, `title`) |

## Änderungen an bestehenden Klassen

### `AppModel` (`VideoPlayer.Mvu`, Record)

- **Neue Eigenschaften:** `ContinueWatching`, `Favorites`, `RecentEntries`
  (`IReadOnlyList<HomeMediaItem>`, Default `[]`) — Startseiten-Listen;
  `HasContinueWatching`, `HasFavorites`, `HasRecentEntries` (`bool`,
  berechnet) — Sichtbarkeit der Sektionen.

### `AppUpdate` (`VideoPlayer.Mvu`, statische Klasse)

- **Neue Message-Cases:** `HomeListsLoadedMessage` → Listen ins Model;
  `CachedListsChangedMessage` → `LoadHomeListsEffect`;
  `MediaItemSelectedMessage` → `NavigateToMediaDetailsEffect`.
- **Geänderte Cases:** `StartupRequestedMessage` → zusätzlich
  `LoadHomeListsEffect` am Ende der Effektliste;
  `SynchronizationCompletedMessage` → `LoadHomeListsEffect` statt leerer
  Effektliste.

### `ShellAppEffectHandler` (`VideoPlayer.Maui/Services`, Klasse)

- **Neue Effekt-Cases:** `LoadHomeListsEffect` →
  `HomeListService.LoadHomeListsAsync` + Rückdispatch
  `HomeListsLoadedMessage`; `NavigateToMediaDetailsEffect` →
  `Shell.Current.GoToAsync` auf `mediadetails` mit Query-Parametern
  (MainThread).
- **Geänderte Methoden:** `SyncContinueWatchingEffect`-/
  `SyncFavoritesEffect`-/`SyncMetadataEffect`-Cases → nach
  `RunPartialSyncAsync` zusätzlich `DispatchMessageAsync(new
  CachedListsChangedMessage())`.

### `HomePage` (`VideoPlayer.Maui/Pages`, ContentPage)

- XAML: Platzhalter-Inhalt ersetzt durch `ScrollView` mit drei Sektionen
  (Titel + horizontale Liste via `BindableLayout`); `OfflineBanner`
  bleibt; Sichtbarkeit über `Model.Has…`-Flags; Item-Template mit
  `CachedPosterImage`, Titel-Fallback und `TapGestureRecognizer`;
  `ProgressBar` auf Weiterschauen-Karten.
- Code-Behind: `OnMediaItemTapped` dispatcht `MediaItemSelectedMessage`
  (BindingContext der Karte = `HomeMediaItem`).

### `AppShell` (`VideoPlayer.Maui`, Shell)

- `Routing.RegisterRoute("mediadetails", typeof(Pages.MediaDetailsPage))`.

### `MauiProgram` (`VideoPlayer.Maui`)

- `AddSingleton<HomeListService>()`, `AddSingleton<PosterImageService>()`,
  `AddTransient<Pages.MediaDetailsPage>()`.

## Datenbankmigrationen

Keine — die Tabellen `cached_lists` und `cached_favorites` existieren und
werden bereits befüllt; der Bild-Cache ist eine Dateiablage im vorhandenen
`PreviewsDirectory`.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `cached_lists`/`cached_favorites`-JSON | Deserialisierung defensiv: fehlender Schlüssel oder ungültiges JSON → leere Liste | `HomeListService` protokolliert und liefert `[]` statt zu werfen |
| Einträge ohne `Entry` | Nicht identifizierbare Medien werden übersprungen | Eintrag erscheint nicht in der Liste |
| `NavigateToMediaDetailsEffect`-Parameter | `MediaId > 0`, `MediaType` nicht leer | Auswahl ohne gültige Identifikation wird nicht navigiert (Update liefert `Unchanged`) |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Start-Effektliste:** `LoadHomeListsEffect` verlängert die
  Start-Sequenz um einen lokalen DB-Lesevorgang (kein Server-Aufruf →
  keine Verzögerung des Verbindungs-Workflows).
- **`SynchronizationCompletedMessage`:** bestehender Test
  `SynchronizationCompleted_ClearsSyncFlag` erwartet leere Effekte —
  muss auf `LoadHomeListsEffect` angepasst werden.
- **`NavigateToMessage`/`NavigatedMessage`:** unverändert — Rückkehr zur
  Startseite löst kein Neuladen aus; das Model ist bereits aktuell
  (Start-/Sync-/Ereignis-Ladevorgänge aktualisieren es seitenunabhängig).
- **Detail-Route:** `AppRoutes.TryGetPage` ignoriert `mediadetails` —
  `CurrentPage` bleibt `Home`; die Zustands-Guards der Menü-Navigation
  bleiben unberührt.

## Umsetzungsreihenfolge

1. **`HomeMediaItem`/`HomeLists` und `HomeListService` in `VideoPlayer.Mvu`**
   - Voraussetzungen: keine (nutzt `CacheDatabase`, ApiClient-Modelle)
   - Beschreibung: Records anlegen; Lesedienst mit defensivem
     Deserialisieren und Mapping (Titel-Fallback `Entry.Name`,
     Poster-Fallback `Entry.PosterPictureId`, `Progress` aus
     `PositionSeconds`/`DurationSeconds`, `MediaType`-Ableitung).
2. **MVU-Erweiterung: Messages, Effekte, `AppModel`, `AppUpdate`**
   - Voraussetzungen: Schritt 1
   - Beschreibung: neue Records in `AppMessage.cs`/`AppEffect.cs`,
     Model-Eigenschaften, Update-Cases (s. o.).
3. **`ShellAppEffectHandler` + `MauiProgram` + `AppShell`-Route**
   - Voraussetzungen: Schritt 2
   - Beschreibung: Effekt-Cases, Teil-Sync-Rückdispatch, DI-Registrierung
     `HomeListService`, `Routing.RegisterRoute("mediadetails", …)`.
4. **Bild-Pipeline: `PosterImageService` + `CachedPosterImage`**
   - Voraussetzungen: keine neuen (nutzt `AuthSessionService`,
     `AppStorageLayout`)
   - Beschreibung: Datei-Cache in `PreviewsDirectory`, `GetPictureAsync`,
     `Image`-Subklasse mit `PictureId`-Property; DI-Registrierung.
5. **`HomePage`-Ausbau + `MediaDetailsPage`**
   - Voraussetzungen: Schritte 2–4
   - Beschreibung: drei Listen-Sektionen nach `MediaCarousel`-Muster,
     Tap-Handler, Platzhalter-Detailseite mit Query-Properties;
     `MediaDetailsPage` in `MauiProgram` registrieren.
6. **Tests**
   - Voraussetzungen: Schritte 1–5
   - Beschreibung: `AppUpdateTests_HomeLists`, `HomeListServiceTests`
     (reale SQLite-Ablage), `HomePageContractTests` (XAML-/Route-/DI-
     Verdrahtung); betroffenen Bestandstest
     `SynchronizationCompleted_ClearsSyncFlag` anpassen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `StartupRequested_IncludesHomeListLoad` | `AppUpdateTests_HomeLists` | `LoadHomeListsEffect` in der Start-Effektliste (nach `InitializeStorageEffect`) |
| `HomeListsLoaded_SetsLists` | `AppUpdateTests_HomeLists` | Listen + `Has…`-Flags im Model |
| `SynchronizationCompleted_ReloadsHomeLists` | `AppUpdateTests_HomeLists` | `LoadHomeListsEffect` nach Voll-Sync |
| `CachedListsChanged_ReloadsHomeLists` | `AppUpdateTests_HomeLists` | `LoadHomeListsEffect` nach Teil-Sync-Rückmeldung |
| `MediaItemSelected_NavigatesToDetails` | `AppUpdateTests_HomeLists` | `NavigateToMediaDetailsEffect` mit `MediaType`/`MediaId`/`Title` |
| `MediaItemSelected_InvalidItem_NoNavigation` | `AppUpdateTests_HomeLists` | `Id <= 0`/leerer `MediaType` → kein Effekt |
| `ServerEventReceived_FullSequence_RefreshesLists` | `AppUpdateTests_HomeLists` | Message-Sequenz Ereignis → Teil-Sync-Effekt → `CachedListsChanged` → `LoadHomeListsEffect` (E2E-Ersatz des Benutzerflusses „Liste aktualisiert sich selbst") |
| `LoadHomeLists_EmptyCache_ReturnsEmpty` | `HomeListServiceTests` | Leere DB → leere `HomeLists` |
| `LoadHomeLists_MapsContinueWatching` | `HomeListServiceTests` | Titel/`Id`/`MediaType`/Poster-Fallback/`Progress` aus `cached_lists`/`continue_watching` |
| `LoadHomeLists_MapsFavorites` | `HomeListServiceTests` | `cached_favorites`-Zeilen → Einträge mit `Entry`-Daten |
| `LoadHomeLists_MapsRecent` | `HomeListServiceTests` | `recent`-Liste → Einträge mit `Entry`-Daten |
| `LoadHomeLists_SkipsEntriesWithoutEntry` | `HomeListServiceTests` | `Entry = null` → Eintrag entfällt |
| `LoadHomeLists_InvalidJson_ReturnsEmptyList` | `HomeListServiceTests` | Beschädigter Cache → leere Liste statt Fehler |
| `HomePage_ContainsThreeLists` | `HomePageContractTests` | XAML: Sektionen „Weiterschauen"/„Favoriten"/„Neu im Programm" mit Bindings an `Model.*` |
| `HomePage_ItemsAreTappable` | `HomePageContractTests` | `TapGestureRecognizer` → `MediaItemSelectedMessage`-Dispatch im Code-Behind |
| `HomePage_UsesPosterImageCache` | `HomePageContractTests` | `CachedPosterImage` im Item-Template; `PosterImageService` nutzt `PreviewsDirectory` (NoBackup) |
| `MediaDetails_RouteRegistered` | `HomePageContractTests` | `Routing.RegisterRoute("mediadetails", …)` in `AppShell.xaml.cs`; `MediaDetailsPage` liest `mediaType`/`id`/`title` |
| `ShellAppEffectHandler_HandlesNewEffects` | `HomePageContractTests` | Cases für `LoadHomeListsEffect`/`NavigateToMediaDetailsEffect`, `CachedListsChangedMessage`-Rückdispatch nach Teil-Syncs |
| `MauiProgram_RegistersHomeServices` | `HomePageContractTests` | `HomeListService`, `PosterImageService`, `MediaDetailsPage` in DI |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppUpdateTests_NetworkSync.SynchronizationCompleted_ClearsSyncFlag` | Erwartet leere Effektliste — jetzt `LoadHomeListsEffect` |

### E2E-Tests (primärer Funktionsnachweis)

Das Repository besitzt kein UI-Automatisierungs-Framework und das
Testprojekt kann `VideoPlayer.Maui` nicht referenzieren (Bestandsaufnahme
`inventory/tests.md`); der Benutzerfluss wird daher — wie in den
vorherigen Schritten — über vollständige Message-Sequenz-Tests auf
MVU-Ebene plus Contract-Tests der Oberflächen-Verdrahtung nachgewiesen.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | App-Start → Startseite zeigt sofort gecachte Listen, Server-Sync aktualisiert sie anschließend | `AppUpdateTests_HomeLists` (Sequenz `StartupRequested` → `LoadHomeListsEffect` → `HomeListsLoaded` → `SynchronizationCompleted` → erneutes `LoadHomeListsEffect`) + `HomeListServiceTests` (realer Cache-Roundtrip) | AK 1 + 2 | Sichtbarer Benutzerfluss; MVU-Sequenz + Service-Test sind der höchste im Repo erreichbare Nachweis |
| Pflicht | Server-Ereignis aktualisiert Liste ohne manuelle Aktion | `AppUpdateTests_HomeLists.ServerEventReceived_FullSequence_RefreshesLists` + `HomePageContractTests.ShellAppEffectHandler_HandlesNewEffects` | AK 3 | Automatische Aktualisierung ist Kern der Anforderung |
| Pflicht | Auswahl eines Eintrags führt zur Detail-Route | `AppUpdateTests_HomeLists.MediaItemSelected_*` + `HomePageContractTests` (Tap-Verdrahtung + Route-Registrierung) | AK 4 | Nutzeraktion mit sichtbarem Ergebnis |

Bestehende E2E-Tests angepasst: Keine.

## Offene Punkte

Keine.
