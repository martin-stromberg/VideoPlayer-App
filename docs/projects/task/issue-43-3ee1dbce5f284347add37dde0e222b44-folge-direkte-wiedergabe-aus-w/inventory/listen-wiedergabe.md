# Detailbefund — Anforderung 1: Listen, Tap-Fluss und Wiedergabestart

## Startseiten-Listen (Weiterschauen / Favoriten / Neu im Programm)

**UI:** `VideoPlayer.Maui/Pages/HomePage.xaml` (Z. 150–161) — drei `controls:MediaCarousel`-Instanzen,
alle mit demselben Handler `ItemTapped="OnMediaItemTapped"`:

| Liste | ItemsSource | Kartenformat |
|---|---|---|
| „Weiterschauen" | `Model.ContinueWatching` | 256×144 (16:9-Stills) |
| „Favoriten" | `Model.Favorites` | Standard 150×225 (Poster 2:3) |
| „Neu im Programm" | `Model.RecentEntries` | Standard 150×225 |

- `MediaCarousel` (`VideoPlayer.Maui/Controls/MediaCarousel.xaml` + `.xaml.cs`): horizontale
  `HorizontalStackLayout` per `BindableLayout.ItemsSource`; Karte = `Border` mit
  `TapGestureRecognizer` (`MediaCarousel.xaml` Z. 39–41) → Ereignis `ItemTapped` (`MediaCarousel.xaml.cs` Z. 81, 91–97).
  Es gibt **kein** Listenattribut im Control — der Handler kann die Liste aktuell nicht unterscheiden
  (Auslöser ist nur das `HomeMediaItem`).
- `HomePage.xaml.cs` Z. 103–108: `OnMediaItemTapped` dispatcht `MediaItemSelectedMessage(item)` —
  für **alle drei Listen identisch**.

## Tap-Verarbeitung im MVU-Zyklus

- `MediaItemSelectedMessage(HomeMediaItem)` — `VideoPlayer.Mvu/AppMessage.cs` Z. 248
  (Kommentar: „die App navigiert zur Detailseite des Mediums").
- `AppUpdate.UpdateMediaItemSelected` — `VideoPlayer.Mvu/AppUpdate.cs` Z. 1188–1203:
  Guard (Id > 0, MediaType belegt, `IsSelectable` — Offline-Platzhalter sind nicht auswählbar),
  dann **einziger Effekt: `NavigateToMediaDetailsEffect`** mit Serienkontext-Hints
  (`ShowId`, `SeasonId`, `EpisodeId` aus `HomeMediaItem`).
- `ShellAppEffectHandler.NavigateToMediaDetailsAsync` — `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`
  Z. 437–464: baut die Shell-Route `mediadetails?mediaType=…&id=…&title=…[&show=…&season=…&episode=…]`
  und pusht sie via `Shell.Current.GoToAsync`.
- `MediaDetailsPage` (`VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs` Z. 80–107): `OnAppearing`
  dispatcht `MediaDetailsRequestedMessage` → `LoadMediaDetailsEffect` → `MediaDetailsService.LoadAsync`.

**Befund Anf. 1:** Aktuell führt ein Tap auf einen Weiterschauen-Eintrag nur zur Detailseite —
keine direkte Wiedergabe.

## Weitere Listen mit Detail-Navigation (Abgrenzung „andere Listen")

- **Favoriten-Seite** (`VideoPlayer.Maui/Pages/FavoritesPage.xaml.cs` Z. 73–86): `CollectionView.SelectionChanged`
  → `MediaItemSelectedMessage(item.ToMediaItem())` — derselbe Detail-Navigationspfad.
- **Quellen-Übersicht** (`VideoPlayer.Maui/Pages/SourceOverviewPage.xaml.cs` Z. 122–133):
  `SourceMediaItemSelectedMessage` → `AppUpdate.UpdateSourceMediaItemSelected` (Z. 1463–1473)
  → `NavigateToMediaDetailsEffect` (ohne Serien-Hints).
- **Episodenliste der Serienansicht** (`MediaDetailsPage.xaml.cs` Z. 262–288): `OnEpisodeSelected`
  → `MediaItemSelectedMessage` mit `MediaType="tvshowepisode"` + Serienkontext → Detailseite der Episode.
- **Kollektions-Filme** (`MediaDetailsPage.xaml.cs` Z. 290–310): `OnCollectionMovieSelected`
  → `MediaItemSelectedMessage` → Film-Detailseite.

## Wiedergabestart (Ist-Zustand)

Auslöser ist ausschließlich die **Detailseite**:

- „Abspielen"-Schalter `PlayButton` (`MediaDetailsPage.xaml` Z. 111–117, sichtbar nur bei
  `details.IsPlayable` = `movie`|`tvshowepisode`) → `OnPlayClicked` (`MediaDetailsPage.xaml.cs`
  Z. 317–330) → `PlayRequestedMessage(MediaType, MediaId, Title)`.
- Play-Icon je Episoden-Eintrag → `OnEpisodePlayClicked` (Z. 332–344) →
  `PlayRequestedMessage("tvshowepisode", episode.Id, episode.Title)`.
- `AppUpdate.UpdatePlayRequested` (`AppUpdate.cs` Z. 1923–1945): nimmt nur `movie`/`tvshowepisode`
  an (nach `MediaTypeNames.Normalize`), setzt `PlayerState` mit `UpcomingEpisodes =
  NextEpisodeQueue(model, mediaId)` und Effekten `NavigateToPlayerEffect` +
  `ResolvePlaybackSourceEffect`. **`PlayRequestedMessage` trägt keine Position** — die
  Fortsetzungsposition wird bei der Quellauflösung ermittelt.
- `PlayerPage` (Shell-Route `player`, registriert `AppShell.xaml.cs` Z. 77): `UpdateUi` setzt
  `VideoElement.Source` (`MediaElement` aus CommunityToolkit.Maui.MediaElement) auf Stream-URL
  oder lokale Datei (`PlayerPage.xaml.cs` Z. 171–224).

## Fortsetzungsposition (Ist-Zustand)

- `PlaybackSourceService.ResolveAsync` (`VideoPlayer.Mvu/PlaybackSourceService.cs` Z. 78–130):
  1. `ResolveResumePositionAsync` (Z. 141–166): online via `client.GetContinueWatchingAsync()`,
     sonst `ReadCachedResumePositionAsync` (Z. 168–189) aus `cached_lists[continue_watching]`.
  2. `FindResumePosition` (Z. 191–201): Eintrag mit passendem Typ + `Entry.Id == mediaId`
     und `PositionSeconds > 0` → `TimeSpan`.
  3. Quelle: fertige lokale Datei aus `download_manifest` vor Server-Stream
     `api/items/{typ}/{id}/stream?access_token=…` (Z. 94–129).
  4. Ergebnis: `PlaybackSource { Kind, Location, ResumePosition }`
     (`VideoPlayer.Mvu/PlaybackSource.cs`).
- `PlayerPage.xaml.cs`: `OnMediaOpened` (Z. 263–288) merkt `ResumePosition` als
  `pendingResumePosition`; `TryApplyPendingResumeSeek` (Z. 298–327) führt `SeekTo` erst bei
  `Playing`/`Paused` aus — iOS-Workaround für Toolkit-Issue #3249 (Kommentar Z. 265–270);
  `ApplyResumeSeekAsync` (Z. 336–351) mit 10-s-Timeout (`ResumeSeekTimeout`, Z. 24).

**Konsequenz für Anf. 1:** Ein direkter Wiedergabestart aus der Weiterschauen-Liste müsste
`PlayRequestedMessage` (o. ä.) dispatchieren; die Fortsetzungsposition kommt dann automatisch
über `PlaybackSourceService`. Abspielbar sind nur `movie`/`episode` (→`tvshowepisode`);
Weiterschauen-Einträge der Typen `show`/`season`/`collection` sind nicht direkt abspielbar
(Update-Guard `AppUpdate.cs` Z. 1926). Eine Unterscheidung der Liste am Auslösepunkt ist nötig,
da `MediaCarousel.ItemTapped` nur das Item liefert.

## Mapping Weiterschauen-Eintrag → HomeMediaItem

`HomeListService.MapContinueWatching` (`VideoPlayer.Mvu/HomeListService.cs` Z. 244–266):
- `Id`/`MediaType` aus dem eingebetteten `Entry` (`MediaTypeOf` Z. 327–335: Server-Typ der
  Weiterschauen-Liste hat Vorrang — `movie`, `show`, `season`, `episode`, `collection`).
- `Progress` aus `PositionSeconds/DurationSeconds` (Z. 259–261).
- Serienkontext aus `MediaHints` (Z. 317–325): Episode → `ShowId`/`SeasonId`/`EpisodeId`.
- **Kein Playlist-Bezug** im Mapping (es existiert kein solches Feld im DTO).

Offline-Variante: `OfflineContinueWatchingService.LoadContinueWatchingAsync`
(`VideoPlayer.Mvu/OfflineContinueWatchingService.cs` Z. 93–163) baut „Weiterschauen" aus dem
Download-Manifest (`completed` + Datei vorhanden, `UpdatedAtUtc` absteigend) plus Platzhalter
fehlender Folge-Episoden (`IsPlaceholder` → nicht auswählbar).
