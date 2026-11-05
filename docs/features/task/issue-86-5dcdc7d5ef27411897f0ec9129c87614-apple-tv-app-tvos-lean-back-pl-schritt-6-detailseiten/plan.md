# Umsetzungsplan: tvOS-Detailansichten (Schritt 6)

## Ansatz

Eine neue modale Detailseite `MediaDetailsViewController` ersetzt den
Platzhalter `PreparedTargetViewController` als Ziel des
`NavigateToMediaDetailsEffect`. Die Seite rendert `AppModel.MediaDetails`
(identitätsgeprüft wie in MAUI) und dispatcht ausschließlich bestehende
MVU-Messages. Der `TvosAppEffectHandler` verdrahtet die Detail-Effekte
analog zum `ShellAppEffectHandler`; `MediaDetailsService` und
`FavoritesService` werden in `AppComposition` komponiert.

## Änderungen

### 1. `VideoPlayer.Mvu` (plattformneutral, klein)

- `MediaDetailsView`: `DateTime? WatchedAt` + `bool IsWatched` ergänzen
  (Gesehen-Kennzeichnung des Mediums selbst).
- `MediaDetailsService`: `WatchedAt` in `MapMovie`, `MapCollection` und
  im eigenständigen Episoden-Pfad (`LoadEpisodeAsync`-Basisdetails)
  befüllen — jeweils aus `MediaEntry.WatchedAt`. Offline-Snapshot bleibt
  ohne Zeitpunkt (keine Quelle). Für `tvshowepisode` im Serienkontext
  deckt `SelectedEpisode.IsWatched` die Kennzeichnung bereits ab.

### 2. `VideoPlayer.TvOS` — neue UI-Dateien

- `Ui/MediaDetailsViewController.cs` — modale, modellgetriebene
  Detailseite (`IModelRenderable`):
  - Kopfbereich: `UIImageView` für `HeaderImageBytes` + Verlauf,
    `HeaderTitle`/`HeaderMeta`/`HeaderPlot`; Badges „Gesehen",
    „Auf diesem Gerät" (`HasLocalFile`), „Offline-Stand"
    (`IsOfflineSnapshot`).
  - Aktionszeile (fokussierbare `UIButton`s): „Abspielen" (nur
    `IsPlayable` und online bzw. mit lokaler Datei →
    `PlayRequestedMessage`), „Herunterladen" (nur `IsPlayable` und
    nicht `IsOfflineSnapshot` → `DownloadRequestedMessage` mit
    Serienkontext wie MAUI; der Effekt-Handler meldet den vorbereiteten
    Hinweis → `DownloadNotice`-Label), „☆/★ Favorit"
    (`FavoriteToggleRequestedMessage`, deaktiviert bei
    `IsFavoriteUpdating`/`IsOfflineSnapshot`).
  - Serienkontext: horizontale Staffel-Chips (Wiederverwendung
    `SourceGenreCell` + Mapping auf `SourceGenreItem`, Auswahl →
    `SeasonSelectedMessage`) und horizontale Episoden-Reihe mit neuer
    `EpisodeItemCell`; Episoden-Tap (kein Platzhalter) →
    `MediaItemSelectedMessage` (tvshowepisode + Show/Season/Episode-
    Hints → Detailseite der Episode im Stapel).
  - Kollektion: `HomeCarouselView` („Filme") mit `CollectionMovieItem`
    → `HomeMediaItem`-Mapping; Tap → `MediaItemSelectedMessage` (movie).
  - Zustände: `IsLoading`, `Error` mit „Erneut versuchen"
    (erneutes `MediaDetailsRequestedMessage`), `FavoriteError`,
    `DownloadNotice`; Identitäts-Guard wie `SourceBrowserViewController`.
  - `ViewWillAppear` → `MediaDetailsRequestedMessage` (einmalig +
    bei geänderter Fremd-Identität, MAUI-Muster); `PressesBegan`
    Menü-Taste → `DismissViewController` + Navigator-Freigabe.
- `Ui/EpisodeItemCell.cs` — fokussierbare Episodenkarte (16:9-Still via
  `EpisodeItem.PosterPictureId` + `TvosPosterImageService`,
  `DisplayTitle`, `✓`-Gesehen-Badge, „Auf diesem Gerät"-Hinweis,
  abgedunkelte Platzhalter-Variante, Hervorhebung der navigierten
  Episode).

### 3. `VideoPlayer.TvOS` — Verdrahtung

- `Ui/TvosNavigator.cs`: `PresentMediaDetails(NavigateToMediaDetailsEffect)`
  — neue `MediaDetailsViewController`-Instanz je Navigations-Effekt,
  fullscreen-modal auf `TopmostPresented()` gestapelt (Detail-Seiten
  stapeln sich wie der MAUI-Navigationsstack); Liste offener
  Detailseiten für `Render(model)`; Freigabe-Callback beim Schließen.
- `Services/TvosAppEffectHandler.cs`:
  - `NavigateToMediaDetailsEffect` → `navigator.PresentMediaDetails`
    (ersetzt `PresentPreparedTarget` nur für diesen Fall — der
    Platzhalter bleibt für Wiedergabe/Playlist-Playback bestehen).
  - `LoadMediaDetailsEffect` → `services.MediaDetails.LoadAsync` →
    `MediaDetailsLoadedMessage` (Fehlerpfad wie MAUI).
  - `LoadMediaDetailsHeaderEffect` → `authSession.Client` Bildabruf
    (Episoden-Still / `api/pictures`) → `MediaDetailsHeaderImageLoadedMessage`.
  - `ToggleFavoriteEffect` → `services.Favorites.ToggleAsync` →
    `FavoriteToggleCompletedMessage` + `CachedListsChangedMessage`.
  - `EnqueueDownloadEffect` → `DownloadEnqueueCompletedMessage` mit
    Hinweis „Download folgt in einem späteren Entwicklungsschritt"
    (vorbereitete Aktion, kein Fehler).
  - `ResolvePlaybackSourceEffect` bleibt bewusst im No-Op (Wiedergabe-
    Schritt); der vorbereitete Player-Ziel-Platzhalter übernimmt.
- `AppComposition.cs`: `MediaDetails` (`MediaDetailsService`) und
  `Favorites` (`FavoritesService`) komponieren.

### 4. Tests

- `VideoPlayer.Maui.Tests/TvOsDetailsContractTests.cs` (Muster
  `TvOsBrowsingContractTests`): Seiten-Messages/Badges/Aktionen,
  Staffel-/Episoden-Fokussierbarkeit, Offline-/Lade-/Fehler-Zustände,
  Navigator-Stapel, Effekt-Verdrahtung, Komposition, `WatchedAt`-
  Erweiterung.

### 5. Dokumentation

- `docs/help/tv-app/`: `beschreibung.md`, `ablauf-anwender.md` und
  `index.md` um die Detailansichten ergänzen (Platzhalter-Formulierung
  ersetzen); `README.md` tvOS-Zeilen anpassen.

## Risiken / Kontrollen

- tvOS kompiliert lokal nicht → streng an geprüften Signaturen arbeiten;
  `build-tvos`-CI ist das Kompilier-Orakel (Push autorisiert).
- Modaler Stapel mehrerer Detailseiten: je Seite eigener Controller +
  Identitäts-Guard (globaler Detail-Slot ist einzeln).
- Keine Schreibzugriffe auf Inhalte; Playlist-/Metadaten-Aktionen werden
  nicht angeboten.

## Offene Punkte

—
