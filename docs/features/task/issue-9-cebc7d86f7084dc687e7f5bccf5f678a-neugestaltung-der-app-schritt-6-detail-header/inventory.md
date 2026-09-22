# Bestandsaufnahme: Detailseiten — Vollbreiten-Header mit Hintergrundgrafik (Schritt 6)

Analysiert wurde die Medien-Detailseite der neuen App (`VideoPlayer.Maui` +
`VideoPlayer.Mvu`) sowie die Bildabrufe des `VideoPlayer.ApiClient`, bezogen
auf die Anforderung eines vollbreiten Header-Bereichs mit Hintergrundgrafik
nach Web-Vorbild (`.tvshow-header`-Muster). Die Legacy-App
`VideoWebPlayer.Maui` ist nicht Teil der Anforderung und bleibt unverändert.

## Zusammenfassung

- Die Detailseite `MediaDetailsPage.xaml` deckt bereits alle fünf Medientypen
  ab (`movie`, `moviecollection`, `tvshow`, `tvshowseason`, `tvshowepisode`)
  über `IsVisible`-Umschaltung in einem einzigen `ScrollView`; der Kopfbereich
  ist heute ein schmales 140×200-`CachedPosterImage` neben Titel/Meta —
  genau der Bereich, den die Anforderung ersetzt.
- `MediaDetailsView` trägt bereits die aufgelöste Banner-Kette
  (`BannerPictureId` = Banner → Fanart → Poster) für Filme, Kollektionen,
  Serien und eigenständige Episoden sowie `PosterPictureId`; Staffel- und
  Episoden-Bilder liegen in `SeasonItem`/`EpisodeItem` noch **nicht** als
  View-Felder vor (die API-Modelle `TvShowSeason`/`TvShowEpisode` liefern
  `BannerPictureId`/`FanartPictureId`/`PosterPictureId` bzw. zusätzlich
  `GeneratedBackgroundPictureId`).
- Der Episoden-Still-Endpunkt `api/episodes/{id}/background-image` ist
  serverseitig verifiziert (`EpisodesController.GetBackgroundImage`:
  generiertes Bild → Banner/Fanart-Fallback → Platzhalter, Bearer-Auth), im
  `VideoPlayerApiClient` fehlt er noch — `GetPictureAsync`,
  `GetSourceIconAsync` und `GetHeroBackgroundAsync` zeigen das Muster.
- Das MVU-Ladegerüst existiert vollständig: `MediaDetailsRequestedMessage` →
  `LoadMediaDetailsEffect` → `MediaDetailsService.LoadAsync` →
  `MediaDetailsLoadedMessage`; `SeasonSelectedMessage` wechselt bereits die
  `SelectedSeasonId`. Ein Header-Bildzustand und ein Bild-Ladeeffekt fehlen.
- Schritt 5 hat das wiederverwendbare Muster etabliert: `VpHero*`-Tokens in
  `Colors.xaml`, `Image` + `AspectFill` + geschichtete
  `LinearGradientBrush`/`RadialGradientBrush` in `HomePage.xaml`,
  `Math.Clamp`-Höhe in `HomePage.xaml.cs`, Bilddaten flüchtig als `byte[]`
  im Model (`HomeHeroState.ImageBytes`), `HeroBackgroundLoadedMessage` und
  `LoadHeroBackgroundEffect` mit Ausführung in `ShellAppEffectHandler`.
- Web-Verhalten beim Episoden-/Staffelwechsel (verifiziert an
  `TVShowDetails.razor`): bei `selectedEpisode != null` zeigt der Header das
  Episoden-Still (`api/episodes/{id}/background-image`) und Episoden-Titel,
  „Episode {Nr}" + Jahr sowie Episoden-Plot; bei `showSeasonInfo` (navigierte
  Staffel) Serien-Backdrop der Staffel (Banner → Fanart → Platzhalter), Titel
  „{Serie} - {Staffel}", Staffeljahr und Episodenanzahl; ein Staffelwechsel im
  Dropdown wählt automatisch die erste Episode (`SelectSeasonCoreAsync`).
- Test-Ausgangszustand: volle Suite `dotnet test
  VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` — **697/697
  bestanden** (siehe [Tests](inventory/tests.md)).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [UI / Seiten](inventory/ui.md)
- [Tests](inventory/tests.md)
