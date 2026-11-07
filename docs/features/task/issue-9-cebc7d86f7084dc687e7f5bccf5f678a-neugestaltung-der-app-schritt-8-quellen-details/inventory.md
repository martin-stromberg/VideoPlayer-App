# Bestandsaufnahme: Quellen-Browsing und Medien-Detailseiten

Analyse des neuen App-Bestands (`VideoPlayer.Mvu`, `VideoPlayer.Maui`,
`VideoPlayer.Storage`, `VideoPlayer.ApiClient`) sowie des Altbestands
(`VideoWebPlayer.Maui` als fachliche UI-Referenz) bezogen auf die
Anforderung „Quellen-Browsing und Medien-Detailseiten"
(Entwicklungsschritt 8).

## Zusammenfassung

- **Quellen-Seite ist Platzhalter:** `VideoPlayer.Maui/Pages/SourcesPage.xaml`
  zeigt nur Titel + Hinweis „wird in einem späteren Entwicklungsschritt
  gefüllt"; `SourcesPage.xaml.cs` hat keinen `MvuHost`-Bezug. Der
  Menüeintrag „Quellen" (Route `sources`) existiert in `AppShell.xaml`.
- **Detailseite ist Platzhalter:** `MediaDetailsPage.xaml(.cs)` zeigt
  Titel + Medientyp-Bezeichnung + Platzhalterhinweis; die Shell-Route
  `mediadetails?mediaType=…&id=…&title=…` ist registriert
  (`AppShell.xaml.cs:25`) und wird von `NavigateToMediaDetailsEffect`
  (Startseiten-Auswahl, Schritt 7) bereits benutzt.
- **API-Client vollständig:** `VideoPlayerApiClient` implementiert alle
  benötigten Endpunkte bereits (Schritt 1, per Endpoint-Tests belegt):
  `GetSourcesAsync` (`api/Sources`), `GetSourceGenresAsync`
  (`api/SourceGenres/{id}`), `GetSourceItemsAsync` (`api/items` mit
  `mediaSourceId`/`page`/`size`/`search`/`genreId`),
  `GetMovieAsync`/`GetMovieCollectionAsync`/`GetTvShowAsync`/
  `GetTvShowEpisodeAsync` (`api/items/{typ}/{id}`), `GetFavoritesAsync`,
  `ToggleFavoriteAsync`, `RemoveFavoriteAsync` (`api/favorites…`),
  `GetPictureAsync` (`api/pictures/{id}`), `GetSourceIconAsync`
  (`api/sourceicons/{id}`).
- **Medienmodelle vorhanden:** polymorphe `MediaEntry`-Hierarchie
  (`Movie`, `MovieCollection` mit `Movies`, `TvShow` mit `Seasons`,
  `TvShowSeason` mit `Episodes`, `TvShowEpisode` mit `Season`),
  `MediaItemSummary` (Quellen-Einträge: `Type`, `Id`, `Title`,
  `PictureId`, `ItemCount`), `MediaSource` (`IconPictureId`),
  `SourceGenres`/`Genre` — siehe [inventory/models.md](inventory/models.md).
- **Favoriten-Infrastruktur vorhanden:** `ServerSyncService.
  SyncFavoritesAsync` ersetzt `cached_favorites` aus `api/favorites`
  (Schlüssel `{clrtype}:{id}`); `HomeListService` mappt sie auf die
  Startseiten-Favoriten; `ServerEventReceivedMessage(FavoritesChanged)`
  → `SyncFavoritesEffect` → `CachedListsChangedMessage` →
  `LoadHomeListsEffect`. **Lücke:** kein clientseitiger Toggle-/Pflege-
  Dienst für Favoriten-Änderungen aus der UI.
- **Vorschaubilder vorhanden:** `PosterImageService` (`api/pictures/{id}`
  → `PreviewsDirectory`, memory+datei-Cache) und `CachedPosterImage`-
  Control (`PictureId`-Property). **Lücke:** Quellen-Icons laufen über
  `api/sourceicons/{id}` — anderer Endpunkt, noch kein Abruf.
- **Offline-Erkennung vorhanden:** `AppModel.IsOffline`
  (`ConnectionState.Offline`); Startseite zeigt `OfflineBanner`. Für die
  Quellen-Seite fehlt jede Offline-Behandlung.
- **MVU-Erweiterungspunkte:** `AppModel` (record), `AppMessage`/
  `AppEffect` (records), `AppUpdate.Update` (switch, reine Funktion),
  `ShellAppEffectHandler.HandleAsync` (switch; dispatcht Ergebnis-
  Messages über `MvuHost` auf dem MainThread) — siehe
  [inventory/logic.md](inventory/logic.md).
- **UI-Referenz Altbestand:** `SourceOverviewPage` (Genre-Leiste mit
  „Alle"-Eintrag, Medien-Raster 150er-Karten, `PageSize=20`, Scroll-
  basiertes Nachladen, `Type == "Movie"` → Kollektion sonst Serie),
  `TVShowDetailsPage` (Staffel-`Picker`, Episodenliste, Favoriten-Stern
  ★/☆ mit `#FFB84D`-Akzent), `MovieCollectionDetailsPage` (Filmraster,
  Favoriten-Stern des gewählten Films) — siehe
  [inventory/logic.md](inventory/logic.md).

## Relevante Erkenntnisse je Bereich

### Quellenliste und Browsing

- `api/Sources` liefert nur vom Server freigegebene Quellen — die
  Anzeige kann die Liste ungefiltert übernehmen (Autorisierung liegt
  serverseitig); `MediaSource.IconPictureId` → `api/sourceicons/{id}`.
- `GetSourceItemsAsync(mediaSourceId, page, pageSize, searchText,
  genreId)`: `page` ist 0-basiert (Endpoint-Test ruft `page: 0`
  auf); optionale Parameter werden nur gesetzt, wenn belegt.
- Der Altbestand lädt beim Genre-Wechsel drei Seiten à 20 und lädt
  ab ~70 % Scrollposition weiter nach; „Alle" ist ein künstlicher
  Genre-Eintrag mit `Id = 0` (= kein Filter). In der neuen App wird
  das Nachladen über `CollectionView.RemainingItemsThreshold` statt
  über ScrollView-Mathematik gelöst (idiomatisch, weniger Code).
- `MediaItemSummary.Type` kommt in Server-Schreibweise (`"movie"` im
  Endpoint-Test, `"Movie"` im Altbestand-Vergleich). Der Altbestand
  bildet `Type == "Movie"` → Kollektion, alles andere → Serie ab.
  Die neue App normalisiert auf die `mediadetails`-Medientypen:
  `movie`/`moviecollection`/`collection` → `moviecollection`-Details
  nur bei `ItemCount > 1`-Hinweis? — Entscheidung im Plan
  (Altbestand-Verhalten: jeder Filmeintrag der Quellenliste öffnet die
  Kollektionsansicht; Serieneinträge die Serienansicht).
- Textsuche ist im Altbestand **nicht** vorhanden — sie ist eine
  neue Anforderung dieses Schritts (`search`-Parameter des Clients).

### Detailseite

- `mediadetails`-Query-Properties `mediaType`/`id`/`title` sind
  vergeben; die Seite muss beim Erscheinen die Detaildaten selbst
  nachladen (die Route trägt nur die Identität).
- Serien: `GetTvShowAsync` liefert `Seasons` mit eingebetteten
  `Episodes` — Staffelwahl und Episodenliste sind ohne Zusatzaufrufe
  möglich (Altbestand-Muster: `Picker` + `CollectionView`).
- Episoden aus Weiterschauen/Favoriten (`mediaType` `episode`/
  `tvshowepisode`): `GetTvShowEpisodeAsync` liefert `Season` →
  `Season.Show` — darüber lässt sich die Serie laden und die Episode
  im Staffelkontext vorwählen (Altbestand öffnete die Serienansicht
  mit vorgewählter Staffel/Episode).
- Kollektionen: `GetMovieCollectionAsync` liefert `Movies[]` — Raster
  wie im Altbestand; Auswahl eines Films navigiert weiter zur
  `movie`-Detailseite.
- Staffeln (`mediaType` `season`/`tvshowseason`): es gibt **keinen**
  Staffel-Endpunkt; die `TvShowSeason` trägt `Show` nur, wenn der
  Server sie einbettet (Favoriten/Weiterschauen liefern eingebettete
  `Entry`-Objekte — dort ist `Show` gesetzt). Ohne `Show`-Bezug kann
  keine Serienansicht geladen werden → Hinweiszustand.

### Favoriten

- `ToggleFavoriteAsync(MediaEntry)` → `POST api/favorites/toggle`
  liefert `bool` (neuer Zustand); erwartet einen typkorrekten
  `MediaEntry` (polymorphes `$type`).
- `cached_favorites`-Konvention: `Key = "{clrtype}:{id}"`,
  `VideoId`/`VideoType` + `Json` (serialisierte `FavoriteEntry`) —
  siehe `ServerSyncService.SyncFavoritesAsync`.
- `FavoriteEntry` hat `Id` (Favoriten-Eintrags-Id, für `remove`)
  und `Entry` (das Medium). Ein lokaler Toggle-Eintrag hat noch keine
  Server-`Id` — Konvention: `Id = 0` bis zum nächsten Sync.

### Ablage

- `cached_favorites` und `cached_lists` liegen in der nicht
  backup-pflichtigen `CacheDatabase` — passt zur Anforderung
  („Favoriten-Kopie nicht backup-pflichtig"); keine Schemaänderung.

## Testbestand

- Basislauf 350/350 grün — siehe [inventory/tests.md](inventory/tests.md)
  und [test-results/baseline-dotnet-test.log](inventory/test-results/baseline-dotnet-test.log).
- Kein UI-Automatisierungs-Framework im Repository; `VideoPlayer.Maui`
  ist vom Testprojekt nicht referenzierbar → UI-Nachweise per
  Contract-Tests (Quelltext-Prüfung, Muster `HomePageContractTests`),
  Logik per `AppUpdateTests_*`/Service-Tests auf `VideoPlayer.Mvu`.
