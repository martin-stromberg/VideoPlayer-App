# Plan-Review: Episodenlisten — Angesehen-Symbol bei gesehenen Episoden

## Ergebnis

**Status:** Vollständig umgesetzt

## Task-Abgleich

| # | Geplant | Umgesetzt | Nachweis |
|---|---------|-----------|----------|
| 1 | `MediaEntry.WatchedAt` (`DateTime?`) an der Basisklasse | Ja | `VideoPlayer.ApiClient/Models/MediaEntry.cs:64` (deutsche XML-Doku) |
| 2 | `EpisodeItem.WatchedAt` + `IsWatched` | Ja | `VideoPlayer.Mvu/MediaDetailsView.cs:67` (`init`), `IsWatched => WatchedAt.HasValue` (`:85`) |
| 3 | `CachedShowEpisode.WatchedAt` | Ja | `VideoPlayer.Mvu/ShowDetailsCache.cs:27` (abwärtskompatibel dokumentiert) |
| 4 | `ShowDetailsCache.WriteAsync` schreibt `WatchedAt` mit | Ja | `ShowDetailsCache.cs:128` (`WatchedAt = e.WatchedAt` je Episode) |
| 5 | `MediaDetailsService.MapSeasons` mappt `WatchedAt` (online) | Ja | `MediaDetailsService.cs:627` |
| 6 | `MediaDetailsService.MapOfflineShow` mappt `WatchedAt` (Offline) | Ja | `MediaDetailsService.cs:470` |
| 7 | `AppUpdate.MarkEpisodeWatchedInDetails` + Aufrufe | Ja | `AppUpdate.cs:1793` (Hilfsmethode, Gates: `IsWatched`, `tvshowepisode`, `HasSeasons`, Episode vorhanden, `WatchedAt` leer); Aufrufe vor `AdvanceToNextEpisode` und im Rückgabepfad von `UpdatePlaybackProgress` (`:1718`, `:1726`) und `UpdatePlaybackEnded` (`:1769`, `:1776`) |
| 8 | `VpBadgeBg`/`VpBadgeBorder` in `Colors.xaml` | Ja | `Colors.xaml:46-47` (`#AD000000` / `#47FFFFFF` mit Web-Quellkommentar) |
| 9 | Watched-Badge im Episoden-`DataTemplate` | Ja | `MediaDetailsPage.xaml:176-228` — Poster-`Grid`, `Border` `AutomationId="WatchedIndicator"`, `IsVisible="{Binding IsWatched}"`, `RoundRectangle 999`, `VpBadgeBg`/`VpBadgeBorder`, Position oben links (`HorizontalOptions`/`VerticalOptions="Start"`, `Margin 6,6,0,0`), Augen-`Path` (Web-SVG-Kontur, `VpPrimarySoft`) + Pupille + Glanzpunkt (`VpText`), `ToolTipProperties.Text="Gesehen"`, `InputTransparent` |
| 10 | `MediaEntrySerializationTests`: `WatchedAt`-Deserialisierung | Ja | `MediaEntry_DeserializesWatchedAt`, `MediaEntry_WithoutWatchedAt_StaysNull` |
| 11 | `MediaDetailsServiceTests`: Mapping online + Offline | Ja | `Load_TvShow_MapsEpisodeWatchedAt`, `Load_WithoutClient_ShowSnapshot_MapsEpisodeWatchedAt`, `Load_WithoutClient_LegacyCache_TreatsEpisodesAsUnwatched` |
| 12 | `AppUpdateTests_Player`: Markierung bei Ende/Watched-Fenster | Ja | `PlaybackEnded_MarksEpisodeWatchedInDetails`, `PlaybackEnded_AutoResume_StillMarksFinishedEpisode`, `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails`, `PlaybackEnded_AlreadyWatchedEpisode_KeepsServerTimestamp`, `PlaybackEnded_Movie_DoesNotTouchEpisodeList`, `PlaybackEnded_UnknownEpisode_LeavesDetailsUnchanged` |
| 13 | `MediaDetailsPageContractTests`: Badge-Markup-Vertrag | Ja | `MediaDetailsPage_EpisodeWatchedBadge_BindsIsWatched`, `MediaDetailsPage_EpisodeWatchedBadge_FollowsWebDesign`, `EpisodeItem_CarriesWatchedState` |
| 14 | `DesignTokensContractTests`: Badge-Tokens pinnen | Ja | `Colors_DefinesWatchedBadgeTokens` |

## Umgesetzte Planelemente

- [x] `MediaEntry` — Feld `WatchedAt` (`DateTime?`) ergänzt
- [x] `EpisodeItem` — Felder `WatchedAt`, `IsWatched` ergänzt
- [x] `CachedShowEpisode` — Feld `WatchedAt` ergänzt
- [x] `ShowDetailsCache.WriteAsync` — Cache-Schreibung erweitert
- [x] `MediaDetailsService.MapSeasons`/`MapOfflineShow` — Mapping erweitert
- [x] `AppUpdate` — `MarkEpisodeWatchedInDetails` + Aufrufe in `UpdatePlaybackProgress`/`UpdatePlaybackEnded`
- [x] `Colors.xaml` — Tokens `VpBadgeBg`, `VpBadgeBorder`
- [x] `MediaDetailsPage.xaml` — Badge-Overlay im Episoden-Template
- [x] Alle 14 geplanten Testmethoden — vorhanden und grün

## Offene Aufgaben

Keine.

## Abweichungen vom Plan

- Der Plan skizzierte die Skalierung des Augen-`Path` über
  `Path.Stretch="Uniform"`; in MAUI heißt die Eigenschaft `Aspect`
  (`Stretch` ist kein XAML-attributierbarer Member — MAUIX2002 beim
  ersten Build, korrigiert zu `Aspect="Uniform"`). Funktion und
  Web-Proportionen (viewBox 24×24 → 18 px) sind identisch.
- Zusätzlich zu den geplanten Tests entstanden vier weitere Randfall-
  Tests (`AutoResume`, Server-Zeitpunkt-Erhalt, Movie-Wiedergabe,
  unbekannte Episode) — Absicherung der Gates der Hilfsmethode.

## Nächster Schritt

Schritt 8 — Usability-Review.
