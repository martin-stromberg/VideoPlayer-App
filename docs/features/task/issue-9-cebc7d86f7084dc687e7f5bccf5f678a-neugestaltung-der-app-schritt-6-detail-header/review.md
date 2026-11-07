# Plan-Review: Detailseiten — Vollbreiten-Header mit Hintergrundgrafik

## Ergebnis

**Status:** Plan vollständig umgesetzt

## Task-Abgleich

| # | Geplant | Umgesetzt | Nachweis |
|---|---------|-----------|----------|
| 1 | `GetEpisodeBackgroundImageAsync` im API-Client | Ja | `VideoPlayer.ApiClient/VideoPlayerApiClient.cs` (Methode + `api/episodes/{id}/background-image` über `SendWithPolicyAsync`) |
| 2 | `SeasonItem.BannerPictureId`/`Year` | Ja | `VideoPlayer.Mvu/MediaDetailsView.cs`, Befüllung in `MediaDetailsService.MapSeasons` (Kette Banner → Fanart → Poster; `ReleaseDate` → `PremieredAt`) |
| 3 | `EpisodeItem.ReleaseYear` | Ja | `MediaDetailsView.cs` + `MapSeasons` |
| 4 | `SelectedEpisodeId` + Header-Ableitungen | Ja | `HeaderEpisodeId`, `HeaderPictureId`, `HeaderTitle`, `HeaderMeta`, `HeaderPlot`, `SelectedEpisode` — plattformneutral, kein `ImageSource` im MVU-Core |
| 5 | `MediaDetailsState`-Header-Felder | Ja | `HeaderImageBytes`, `IsHeaderImageLoading`, `HasHeaderImage` |
| 6 | `LoadMediaDetailsHeaderEffect` | Ja | `AppEffect.cs` |
| 7 | `MediaDetailsHeaderImageLoadedMessage` | Ja | `AppMessage.cs` |
| 8 | `MediaDetailsService`-Mapping | Ja | `MapShow`/`MapSeasons`/`LoadEpisodeAsync`/`MapOfflineShow` (+ Standalone-Offline-Episode: `SelectedEpisodeId = mediaId`) |
| 9 | `Update`-Switch + `UpdateMediaDetailsLoaded` | Ja | `AppUpdate.cs` |
| 10 | `UpdateSeasonSelected` (erste Episode + Reload) | Ja | `AppUpdate.cs` — Web-Parität, Identitätsvergleich, Bytes-Verwerfung |
| 11 | `UpdateMediaDetailsHeaderImageLoaded` + `WithDetailsHeaderLoad` | Ja | `AppUpdate.cs` |
| 12 | `ShellAppEffectHandler`-Fall + `LoadMediaDetailsHeaderAsync` | Ja | `Services/ShellAppEffectHandler.cs` (Episoden-Still vs. `GetPictureAsync`, Fehlschlag → `null` + Log) |
| 13 | XAML-Header | Ja | `MediaDetailsPage.xaml`: `HeaderSection` vollbreit, `AspectFill`, `VpHero*`-Gradienten, `VpBorder`-Trennlinie, Favorit oben rechts, Aktionen unten rechts, Inhalt unten links (max. 820); Wurzel-Padding auf Unterbereich verschoben |
| 14 | Code-Behind | Ja | `UpdateHeaderImage` (Referenzvergleich + `ImageSource.FromStream`), `OnSizeChanged` (`Math.Clamp(Width*0.52,360,620)`) |
| 15 | Endpunkt-Tests | Ja | `EndpointCases`-Eintrag + `…RequestsStillEndpoint…` + `…SendsBearerToken` |
| 16 | `AppUpdateTests_DetailsHeader` | Ja | 14 Tests (Quelle je Kontext, Offline, Identitäts-Abgleich, Fehlschlag, Staffelwechsel, Header-Texte) |
| 17 | `MediaDetailsServiceTests`-Erweiterungen | Ja | Bannerkette/Jahre, `SelectedEpisodeId` online+offline |
| 18 | `MediaDetailsPageContractTests` | Ja | 7 Vertragstests |

## Abweichungen vom Plan

- `HeaderEpisodeId` ist als `SelectedEpisodeId`-Durchreiche implementiert
  statt `SelectedEpisode?.Id` mit `MediaId`-Fallback — äquivalent, da der
  Dienst `SelectedEpisodeId` bei eigenständigen Episoden auf `MediaId`
  setzt; simpler und deckt den nicht-in-der-Liste-auflösbaren Fall ab.
- Offline-Einzel-Episode ohne Serienkontext erhält ebenfalls
  `SelectedEpisodeId` (Konsistenz; ohne Verbindung wird dennoch kein
  Bild abgerufen — Gate auf `ConnectionState.Connected`).

## Nächster Schritt

Schritt 8 — Usability-Review.
