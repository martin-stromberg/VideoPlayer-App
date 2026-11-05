# Abnahmeprüfung – Entwicklungsschritt 6

## Ergebnis

**Status:** Anforderung erfüllt – externe Verifikation ausstehend

## Ausstehende externe Verifikationen

- **Laufzeitverhalten auf tvOS-Simulator/-Gerät:** Fokussierbarkeit der
  Staffel-Chips und der Episodenreihe per Siri Remote, das Verhalten des
  modalen Detailseiten-Stapels (Detail → Detail, Menü-Taste löst eine
  Ebene auf, darunterliegende Seite fordert ihre Identität erneut an),
  das Nachladen der Episoden-Stills/Kopfbilder sowie die Befüllung mit
  echten Serverdaten sind auf Code-Ebene und über Contract-Tests
  verifiziert, nicht per Laufzeitnachweis. Im Verifikationsregister
  `pending-verification.md` (Zeile „2–laufend") erfasst.
- Der macOS-CI-Nachweis selbst ist erbracht: Run `42122059558`
  (`build-tvos` + `verify` grün) auf `headSha`
  `346a114aede7d8410d5fd96e079c9d1bee20cad2` = aktueller HEAD = origin;
  keine ungepushten Commits (`git log origin/<branch>..HEAD` leer),
  Arbeitsbaum sauber bis auf untracked `.work/`.

## Hinweise

Prüfung anhand des Diffs `task/issue-86-...-apple-tv-app-tvos-lean-back-pl...HEAD`
(14 Dateien, +1408/−40) und Lesen der Quelldateien samt der
plattformneutralen MVU-Gegenseite und des MAUI-Referenzpfads
(`ShellAppEffectHandler`).

**(a) Alle 5 Medientypen abgedeckt:** `MediaDetailsService.LoadAsync`
(`VideoPlayer.Mvu/MediaDetailsService.cs` Z. 99–109) dispatcht
`movie`, `moviecollection` (mit 404-Fallback auf Film,
Z. 229–240), `tvshow`, `tvshowseason` (Serienkontext mit
Staffel-Vorwahl, Z. 252–263) und `tvshowepisode` (Serienkontext über
Hint bzw. `Season.Show`-Bezug, sonst Basisdetails, Z. 278–337).
`MediaDetailsViewController` rendert generisch Kopfbereich
(Bild, Titel, Genre-/Jahres-Meta `HeaderMeta`, Plot `HeaderPlot`),
Serien-Sektionen (Staffel-Chips + Episodenreihe, nur bei
`HasSeasons`) und Kollektions-Karussell (`HasMovies`). Seiten-
Identität wird gegen den globalen `MediaDetails`-Slot geprüft
(Z. 431–447) und per `MediaDetailsRequestedMessage` beim Erscheinen
angefordert (Z. 384–400).

**(b) Gesehen + lokaler-Datei-Hinweis:** Badges „✓ Gesehen",
„Auf diesem Gerät" (`HasLocalFile`) und „Offline-Stand"
(`IsOfflineSnapshot`) im Kopfbereich (Z. 207–209, Anwendung
Z. 457–460); bei ausgewählter Episode zählt deren
`SelectedEpisode.IsWatched`. `WatchedAt`-Erweiterung in Mvu korrekt:
`MediaDetailsView.WatchedAt`/`IsWatched` (`MediaDetailsView.cs`
Z. 169–172), befüllt in `MapMovie` (Z. 558), `MapCollection`
(Z. 587) und im eigenständigen Episoden-Pfad (Z. 334); im
Serienkontext tragen die `EpisodeItem` den Stand
(`MapSeasons` Z. 649, Offline-Snapshot Z. 490). `EpisodeItemCell`
zeigt Gesehen-/lokale-Datei-Badges, Hervorhebung der navigierten
Episode und abgedunkelte Platzhalter „nicht heruntergeladen"
(`EpisodeItemCell.cs` Z. 165–187). Serien-/Staffel-Ebene trägt
bewusst kein Gesehen-Badge — `TvShow` hat serverseitig kein
`WatchedAt` (nur `MediaEntry`, `ApiClient/Models/MediaEntry.cs`
Z. 64); Kennzeichnung erfolgt dort auf Episodenebene.

**(c) Staffel-/Episodenauswahl fokussierbar:** Staffel-Chips als
horizontale `UICollectionView` mit wiederverwendeter `SourceGenreCell`
(`CanBecomeFocused`, `SourceGenreCell.cs` Z. 53) →
`SeasonSelectedMessage` → `UpdateSeasonSelected`
(`AppUpdate.cs` Z. 1937–1980, Web-Parität: Vorwahl der ersten
Episode, Kopfbild-Invalidierung); Episodenreihe mit
`EpisodeItemCell` (`CanBecomeFocused`, Fokus-Skalierung) →
`MediaItemSelectedMessage` mit Show/Season/Episode-Hints → neue
Detailseite auf dem modalen Stapel; Platzhalter-Episoden sind über
`IsSelectable` im Update nicht auswählbar (`AppUpdate.cs`
Z. 1363–1367). `RemembersLastFocusedIndexPath` gesetzt.

**(d) Favoriten-Toggle:** `OnFavorite` →
`FavoriteToggleRequestedMessage` → `UpdateFavoriteToggleRequested`
(`AppUpdate.cs` Z. 1982–1993) → `ToggleFavoriteEffect` →
`TvosAppEffectHandler.ToggleFavoriteAsync` →
`FavoritesService.ToggleAsync` (`FavoritesService.cs` Z. 38–58):
Server-Toggle `api/favorites/toggle` + autoritative Erneuerung von
`cached_favorites` via `SyncFavoritesAsync` →
`FavoriteToggleCompletedMessage` aktualisiert
`details.IsFavorite` → `FavoriteStarText` (★/☆) im Render
(Z. 467–468) plus `CachedListsChangedMessage` für die
Startseiten-Listen. Fehlschlag → `FavoriteError`-Label;
Schalter deaktiviert bei `IsFavoriteUpdating` und
`IsOfflineSnapshot`.

**(e) Offline-Snapshot-Pfad:** `LoadAsync` weicht ohne angemeldeten
Client auf `LoadOfflineAsync` aus (`MediaDetailsService.cs`
Z. 92–97, 130–219): Snapshot aus Download-Manifest,
`ShowDetailsCache` (Serienstruktur mit `WatchedAt` je Episode) und
`cached_favorites`; `IsOfflineSnapshot` blendet Download-Button und
deaktiviert Favorit, „Abspielen" bleibt bei `HasLocalFile`
verfügbar; nicht heruntergeladene Episoden erscheinen als
Platzhalter. Einschränkung paritätisch zu MAUI: für
`moviecollection` existiert kein lokaler Datenbestand →
Offline-Aufruf zeigt die reguläre Fehlerseite (definiertes
Service-Verhalten, Z. 216–218). Der Offline-Auslöser ist wie in
MAUI `authSession.Client is null` (Offline-Start ohne Sitzung);
bei mitten in der Sitzung verlorener Verbindung schlägt der
Serverabruf fehl und die Seite zeigt den regulären Fehlertext mit
„Erneut versuchen" — identisches Verhalten zur Hauptanwendung.

**(f) Play/Download vorbereitet ohne Fehler:** „▶ Abspielen"
(nur `IsPlayable` = movie/tvshowepisode; im Offline-Snapshot nur mit
`HasLocalFile`) → `PlayRequestedMessage` → `StartPlayback` →
`NavigateToPlayerEffect` → `PresentPreparedTarget`
(„Die Wiedergabe folgt in einem späteren Entwicklungsschritt.",
`TvosAppEffectHandler` Z. 204–214); `ResolvePlaybackSourceEffect`
bleibt bewusst im protokollierten No-Op. „Herunterladen" (nur
`IsPlayable` und nicht `IsOfflineSnapshot`) →
`DownloadRequestedMessage` mit Serienkontext wie MAUI →
`EnqueueDownloadEffect` → Handler meldet
`DownloadEnqueueCompletedMessage` mit Hinweis „Der Download folgt in
einem späteren Entwicklungsschritt." (kein Fehler, kein
`DownloadService`-Aufruf) → `downloadNoticeLabel` (Z. 471–474).

**(g) Kein Schreibzugriff auf Inhalte/Playlists:** Der Handler
verdrahtet nur den erlaubten `ToggleFavoriteEffect`; keine
Playlist-/Metadaten-Mutationen, kein `DownloadService`/
`EnqueueAsync` — abgesichert durch die angepassten Contract-Tests
(`TvOsAppContractTests.TvOsEffectHandler_DoesNotWireExcludedAreas`,
`TvOsHomePageContractTests.TvOsEffectHandler_DoesNotWireContentMutations`).

**(h) Tests:** `dotnet test VideoPlayer.Maui.Tests` —
**1488/1488 bestanden, 0 Fehler** (47 s). Neue
`TvOsDetailsContractTests.cs` (8 Tests) prüfen Kopfbereich/Badges,
Aktions-Messages, Fokussierbarkeit, Zustände/Menü-Taste,
Episodenzelle, Navigator-Stapel, Effekt-Verdrahtung/Komposition und
die `WatchedAt`-Erweiterung.

**(i) tvOS-Kompilierfallen:** Stichprobe sauber — `.Spacing` nur auf
`UIStackView`; `nfloat` als `static readonly` statt `const`
(`EpisodeItemCell.cs` Z. 26); `RegisterClassForCell` mit `typeof` +
`string`-`ReuseId`; `new UIStackView([...])` nutzt denselben
`UIView[]`-Konstruktor wie `TvosControls.Section`;
`PressesBegan`/`ToArray<UIPress>` identisch zu den kompilierenden
Bestands-Controllern; `TvosControls.*`-Signaturen passend
(`Status()`/`Error()` parameterlos). Endgültiger Nachweis:
`build-tvos` auf HEAD grün (Run `42122059558`, 8m34s).

**(j) CI/Push:** `gh run view 42122059558` → `conclusion: success`,
beide Jobs (`build-tvos`, `verify`) grün, `headSha` = `346a114` =
HEAD = `origin/<branch>`; keine ungepushten Commits.

**Weitere Beobachtungen (keine Abweichung):** untracked `.work/`
im Arbeitsbaum (Werkzeug-Ablage, nicht Teil des Diffs); bekannte
NU1903-Warnung (`Microsoft.AspNetCore.SignalR.Common` 10.0.10) ist
Altbestand und nicht Gegenstand dieses Schritts; Hilfe-Dateien
(`docs/help/tv-app/*`) und `README.md` wurden konsistent auf die
echten Detailansichten umgestellt.
