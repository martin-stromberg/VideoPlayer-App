# Bestandsaufnahme: MVU-Kern und Player-Kontext

## `AppModel` (`VideoPlayer.Mvu/AppModel.cs`)

Unveränderliches Gesamtmodell (sealed record). Relevante Felder:

- `CurrentPage` (`AppPage`), `ConnectionState`, `IsOffline`,
  `IsOnline` — Zustandssteuerung und Offline-Erkennung
- `Settings` (`AppSettings`) — beim Start über `LoadSettingsEffect`
  geladen; enthält `AutoResumeEnabled` (Default `true`),
  `AutoResumeRemainingSeconds` (Default 300, Klamm 30–3600),
  `WatchedEndWindowSeconds` (Default 300, Klamm 30–3600)
- `MediaDetails` (`MediaDetailsState`) — trägt das geladene
  `MediaDetailsView` mit dem Serienkontext (s. u.)
- `ContinueWatching` (`IReadOnlyList<HomeMediaItem>`) — Cache-Stand
  der Weiterschauen-Liste (mit `Progress` 0–1, aber ohne absolute
  Position in Sekunden)

Ein `Player`-Zustand existiert noch nicht.

## `MediaDetailsView` (`VideoPlayer.Mvu/MediaDetailsView.cs`)

Trägt den Serienkontext für „nächste Episode":

- `MediaType`, `MediaId`, `Title` des navigierten Mediums
- `ShowId`, `Seasons` (`SeasonItem` mit `Number` und sortierter
  `Episodes`-Liste), `SelectedSeasonId`, `SelectedSeason`,
  `SelectedSeasonEpisodes`, `HighlightedEpisodeId`,
  `HighlightedEpisodeTitle`
- `EpisodeItem`: `Id`, `Number`, `Title`, `Plot`, `PosterPictureId`,
  `IsHighlighted`

Staffelübergreifende „nächste Episode" ist aus `Seasons`
(geordnet nach `Number`) + deren `Episodes` (geordnet nach
`Number`) ableitbar.

## `AppMessage` / `AppEffect` / `AppUpdate`

- `AppUpdate.Update` bildet jede Message per `switch` auf
  `(AppModel, AppEffect[])` ab; private `Update…`-Methoden je
  Message (Muster siehe `UpdateMediaDetailsRequested`,
  `UpdateSourceSelected`).
- `NavigatedMessage` synchronisiert Shell-Navigationen ins Model;
  `NavigateToPageEffect` führt Menü-Navigationen aus; geschobene
  Routen laufen über dedizierte Effekte
  (`NavigateToMediaDetailsEffect`, `NavigateToSourceOverviewEffect`).
- Fortschritts-/Player-Messages und -Effekte existieren noch nicht.

## `MediaTypeNames`

`Normalize` bildet Server-Konventionen auf die CLR-Konvention ab
(`movie`, `moviecollection`, `tvshow`, `tvshowseason`,
`tvshowepisode`). Der Stream-Endpunkt des Altbestands nutzt exakt
diese CLR-Konvention (`api/items/{movie|tvshowepisode}/{id}/stream`).

## `MvuHost`

Serialisiert Dispatchs (`SemaphoreSlim gate`), wendet `Update` an,
führt Effekte sequenziell über `IAppEffectHandler` aus; Seiten
binden an `Model` und lauschen `ModelChanged`.
