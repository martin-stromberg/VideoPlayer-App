# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| 1. Quellenliste entspricht den Daten der Server-API | Live-Abruf `GetSourcesAsync` (`api/Sources`) beim Öffnen; ungefilterte Übernahme der serverautorisierten Liste | `SourcesPageOpened_Connected_Loads`, `SourcesLoaded_SetsList`, `SourceBrowserServiceTests.LoadSources_MapsEntries` | Abgedeckt |
| 2. Genre-Filter, Suche, Paging | Genre-Leiste mit „Alle", `SearchBar`, `RemainingItemsThreshold`-Nachladen; `api/items` mit `genreId`/`search`/`page`/`size` | `SourceGenreSelected_*`, `SourceSearchSubmitted_*`, `SourceItemsNextPageRequested_*`, `SourceBrowserServiceTests` | Abgedeckt |
| 3. Detailseiten (Serien-Staffeln/Episoden, Kollektions-Filme, Basisdetails) | `MediaDetailsService` je Medientyp; `Picker` + Episodenliste; Filmraster; eingebettete Staffeln/Episoden | `MediaDetailsServiceTests` (je Typ + Serienkontext + Fallback), `SeasonSelected_SwitchesEpisodes`, XAML-Contract | Abgedeckt |
| 4. Favoriten zum Server + konsistent nach Neustart | `ToggleFavoriteAsync` + `SyncFavoritesAsync` (ersetzt `cached_favorites`) + `CachedListsChangedMessage` → Startseiten-Liste | `FavoritesServiceTests` (realer SQLite-Roundtrip), `FavoriteToggle*`-Sequenzen, Handler-Contract | Abgedeckt |
| 5. Offline: Quellen-Seite zeigt Hinweis | `SourcesPageOpenedMessage` ohne Effekt im Offline-Zustand + `IsOffline`-gebundener Hinweis | `SourcesPageOpened_Offline_NoLoad`, XAML-Contract | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss | Geplanter Test | Status |
|---------------|----------------|--------|
| Quellen öffnen → Liste | MVU-Sequenz + Service-Test + XAML-Contract | Abgedeckt (kein UI-Automatisierungs-Framework im Repo — dokumentierte Substitution wie Schritt 6/7) |
| Quelle durchblättern: Genre/Suche/Nachladen | MVU-Sequenzen + URL-Parameter-Test | Abgedeckt |
| Detailseiten je Medientyp inkl. Staffelwahl | Service-Mapping-Tests + MVU-Sequenz + XAML-Contract | Abgedeckt |
| Favorit umschalten → Server + Cache + Startseite | SQLite-Roundtrip + MVU-Sequenz + Handler-Contract | Abgedeckt |
| Neustart-Konsistenz der Favoriten | Bestehende `ServerSyncServiceTests`/`HomeListServiceTests` decken denselben Pfad | Abgedeckt |

## Hinweise

- Alle Auswahlen erfolgen über benannte Einträge (Quellenliste, Genre-
  Leiste, Suche, Staffel-`Picker`, Episoden-/Filmraster) — keine Eingabe
  interner Kennungen; Ids laufen nur über Routen-Query-Parameter
  (Shell-intern, nicht anwendersichtbar).
- Offene Punkte sind mit Empfehlungen versehen (Mapping der
  `Type`-Werte, Staffel ohne Serien-Hint, Stern-Ziel im Serienkontext) —
  keine Blocker.
- Fremde Änderungen (`.gitignore`, `.agents/`) werden nicht committet.
