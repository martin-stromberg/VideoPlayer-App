# Umsetzungsplan: Detailseiten — Vollbreiten-Header mit Hintergrundgrafik

## Übersicht

Die `MediaDetailsPage` (deckt `movie`, `moviecollection`, `tvshow`,
`tvshowseason`, `tvshowepisode` ab) erhält einen vollbreiten
Hintergrundbild-Header nach Web-Vorbild (`.tvshow-header`):
Cover-Bild + geschichtete Dunkelgradienten + Inhalt unten links
(Titel, Meta, Plot, Aktionen) + Favoriten-Schalter oben rechts.
Die Bildquelle folgt der Web-Kette: `api/pictures/{id}` für
Serien/Staffeln/Filme/Kollektionen (Banner → Fanart → Poster),
`api/episodes/{id}/background-image` für Episoden-Stills. Der
Serien-/Staffel-Header wechselt wie im Web auf das Still der ausgewählten
Episode. Umgesetzt wird nach dem Schritt-5-Hero-Muster: Bilddaten flüchtig
als `byte[]` im MVU-Model, Laden über eigenen Effekt/Nachricht,
`ImageSource.FromStream` + `Math.Clamp`-Höhe in der Seite.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Header-Bildtransport | `byte[]` flüchtig in `MediaDetailsState`, geladen über `LoadMediaDetailsHeaderEffect` → `MediaDetailsHeaderImageLoadedMessage` | Schritt-5-Muster (`HomeHeroState`/`LoadHeroBackgroundEffect`); deckt beide Endpunkte (`api/pictures/{id}` und `api/episodes/{id}/background-image`) einheitlich ab; MVU-Übergänge sind im Testprojekt direkt testbar — `CachedPosterImage`/`PosterImageService` können den Episoden-Endpunkt nicht. |
| Auswahl der Header-Identität | Berechnete Eigenschaften auf `MediaDetailsView` (`HeaderEpisodeId` ⊻ `HeaderPictureId`, `HeaderTitle`, `HeaderMeta`, `HeaderPlot`) | Plattformneutral prüfbar; die Seite bleibt dünn. Die Ableitung bildet exakt das Web-`GetHeaderBackgroundUrl()` ab. |
| „Ausgewählte Episode" | Neue Eigenschaft `SelectedEpisodeId` im `MediaDetailsView` — initial die navigierte Episode (`tvshowepisode`), sonst `null`; `SeasonSelectedMessage` setzt sie auf die erste Episode der gewählten Staffel | Web-Parität: `SelectSeasonCoreAsync` wählt bei Staffelwechsel die erste Episode; der Header zeigt dann deren Still und Episoden-Infos. `HighlightedEpisodeId` bleibt unverändert die *navigierte* Episode (wird für Play-/Download-Titel verwendet). |
| Staffel-Backdrop | `SeasonItem.BannerPictureId` = `Banner → Fanart → Poster` aufgelöst; Header-Fallback zusätzlich auf den Serien-Backdrop (`MediaDetailsView.BannerPictureId`) | Das Web nutzt `Banner → Fanart → Platzhalter`; die breitere Kette ist robust und deckt die Anforderung (Fallback „bricht nicht zusammen") besser ab. |
| Vollbreite | `Padding` vom Wurzel-`Grid` entfernen; nicht-Header-Inhalte in einen gepaddeten `VerticalStackLayout` unterhalb des Header-`Grid` verschieben | Negative Margins sind in MAUI plattformabhängig fragil; verschobenes Padding erzielt denselben Vollbreiten-Effekt (`margin: -2rem -gutter` im Web) zuverlässig. |
| Aktions-Schaltflächen | Favoriten-Schalter oben rechts auf dem Header (Web: `.favorite-btn` top/right, 44px pill); „Abspielen" und „Herunterladen" unten rechts auf dem Header (Web: `.play-button` bottom/right) | Web-Header-Muster; Schalter bleiben an dieselben Clicked-Handler gebunden. |
| Offline | Kein Bildabruf ohne verbundenen Zustand; Header zeigt Gradienten über `VpSurfaceLow` | Wie im Web ohne Bild (Gradienten über dem Flächenhintergrund); keine lokale Bildablage nötig. |

## Programmabläufe

### Detail-Header laden (nach Detail-Ladung)

1. `MediaDetailsPage.OnAppearing` dispatcht `MediaDetailsRequestedMessage` (unverändert).
2. `UpdateMediaDetailsRequested` erzeugt neue `MediaDetailsState` (Header-Bild zurückgesetzt) + `LoadMediaDetailsEffect` (unverändert).
3. `ShellAppEffectHandler.LoadMediaDetailsAsync` liefert `MediaDetailsLoadedMessage` mit `MediaDetailsView` (unverändert).
4. `UpdateMediaDetailsLoaded` übernimmt die Details (unverändert) **und** wertet `details.HeaderEpisodeId`/`HeaderPictureId` aus: bei vorhandener Header-Identität und `ConnectionState.Connected` wird `IsHeaderImageLoading = true` gesetzt und `LoadMediaDetailsHeaderEffect` angehängt.
5. `ShellAppEffectHandler` führt den Effekt aus: `HeaderEpisodeId` > 0 → `client.GetEpisodeBackgroundImageAsync`, sonst `client.GetPictureAsync(HeaderPictureId)`; ohne Client/Fehler → `null`. Ergebnis: `MediaDetailsHeaderImageLoadedMessage`.
6. `UpdateMediaDetailsHeaderImageLoaded` prüft Medien-Identität (`MediaType`/`MediaId`) **und** Header-Identität gegen den aktuellen Stand; bei Übereinstimmung werden `HeaderImageBytes` übernommen und `IsHeaderImageLoading` aufgehoben; Fehlschlag (`null`) löscht kein vorhandenes Bild (Muster `UpdateHeroBackgroundLoaded`).
7. `MediaDetailsPage.UpdateUi` setzt `HeaderImage.Source` per `ImageSource.FromStream` bei Referenzwechsel der Bytes.

Beteiligte Klassen: `AppUpdate`, `MediaDetailsState`, `MediaDetailsView`, `ShellAppEffectHandler`, `VideoPlayerApiClient`, `MediaDetailsPage`.

### Staffelwechsel (Header folgt der Auswahl)

1. `SeasonPicker` → `OnSeasonSelected` → `SeasonSelectedMessage` (unverändert).
2. `UpdateSeasonSelected` setzt `SelectedSeasonId` **und** `SelectedEpisodeId` auf die erste Episode (`Number`-aufsteigend) der gewählten Staffel (Web: `SelectSeasonCoreAsync`); Staffel ohne Episoden → `SelectedEpisodeId = null`.
3. Ändert sich dadurch die Header-Identität (`HeaderEpisodeId`/`HeaderPictureId` neu berechnet), wird `HeaderImageBytes` verworfen und — bei verbundenem Zustand und vorhandener Identität — `IsHeaderImageLoading` + `LoadMediaDetailsHeaderEffect` gesetzt. Beispiele: Serie → erste Episode = Episoden-Still; Staffel ohne Bilder/Bildlos → nur Gradienten.

Beteiligte Klassen: `AppUpdate`, `MediaDetailsView`, `MediaDetailsPage`.

### Episoden-Auswahl (navigierte Episode)

Unverändert: `OnEpisodeSelected` → `MediaItemSelectedMessage` → `NavigateToMediaDetailsEffect` → neue Detailseite mit `mediaType=tvshowepisode`; dort trägt `SelectedEpisodeId` initial die Episoden-Id → Header zeigt `api/episodes/{id}/background-image` mit Episoden-Titel/-Meta/-Plot.

## Neue Klassen

Keine neuen Klassen — nur neue Records-Mitglieder bzw. Effekt-/Nachrichten-Records.

| Typ | Art | Zweck |
|-----|-----|-------|
| `LoadMediaDetailsHeaderEffect` | `AppEffect`-Record (neu) | Header-Bild laden: `MediaType`, `MediaId`, `HeaderPictureId`, `HeaderEpisodeId` |
| `MediaDetailsHeaderImageLoadedMessage` | `AppMessage`-Record (neu) | Ergebnis: `MediaType`, `MediaId`, `HeaderPictureId`, `HeaderEpisodeId`, `ImageBytes` |
| `MediaDetailsPageContractTests` | Testklasse (neu) | Quell-Vertragstests der Header-Umsetzung |
| `AppUpdateTests_DetailsHeader` | Testklasse (neu) | MVU-Übergänge des Header-Ladezyklus |

## Änderungen an bestehenden Klassen

### `MediaDetailsView` (`VideoPlayer.Mvu`, Record)

- **Neue Eigenschaften (init):** `SelectedEpisodeId` (`long?`) — im Header
  ausgewählte Episode.
- **Neue berechnete Eigenschaften:**
  - `SelectedEpisode` (`EpisodeItem?`) — Auflösung von `SelectedEpisodeId` in `Seasons`.
  - `HeaderEpisodeId` (`long?`) — `SelectedEpisode?.Id`, bzw. `MediaId` wenn `MediaType == "tvshowepisode"` ohne auflösbare Listen-Episode; sonst `null`.
  - `HeaderPictureId` (`long?`) — nur wenn `HeaderEpisodeId` `null`: `MediaType == "tvshowseason"` → `SelectedSeason?.BannerPictureId ?? BannerPictureId`, sonst `BannerPictureId`.
  - `HeaderTitle` (`string`) — Episode: `SelectedEpisode?.Title ?? HighlightedEpisodeTitle ?? Title`; Staffel (ohne Episode): `"{Title} - {SelectedSeason.Name}"`; sonst `Title`.
  - `HeaderMeta` (`string?`) — Episode: `{ReleaseYear} · Episode {Number}` (nur vorhandene Teile); Staffel: `{Year} · {Count} Episoden`; sonst `{MediaTypeText} · {ReleaseYear} · {GenreNames}`.
  - `HeaderPlot` (`string?`) — Episode: `SelectedEpisode?.Plot ?? Plot`; Staffel ohne Episode: `null` (Web: leer); sonst `Plot`.

### `SeasonItem` (`VideoPlayer.Mvu`, Record)

- **Neue Eigenschaften:** `BannerPictureId` (`long?`, aufgelöste Kette Banner → Fanart → Poster), `Year` (`int?`, `PremieredAt`/`ReleaseDate`-Jahr).

### `EpisodeItem` (`VideoPlayer.Mvu`, Record)

- **Neue Eigenschaft:** `ReleaseYear` (`int?`, aus `ReleaseDate`).

### `MediaDetailsState` (`VideoPlayer.Mvu`, Record)

- **Neue Eigenschaften:** `HeaderImageBytes` (`byte[]?`), `IsHeaderImageLoading` (`bool`), `HasHeaderImage` (`bool`, berechnet).

### `AppMessage` / `AppEffect` (`VideoPlayer.Mvu`)

- **Neue Records:** `LoadMediaDetailsHeaderEffect`, `MediaDetailsHeaderImageLoadedMessage` (Signaturen s. o.).

### `AppUpdate` (`VideoPlayer.Mvu`)

- **Geänderte Methoden:**
  - `Update` — Verteilung der neuen Nachricht `MediaDetailsHeaderImageLoadedMessage`.
  - `UpdateMediaDetailsLoaded` — nach Übernahme der Details Header-Ladeeffekt anhängen (Bedingungen: Details vorhanden, `Connected`, Header-Identität vorhanden).
  - `UpdateSeasonSelected` — zusätzlich `SelectedEpisodeId` setzen (erste Episode der Staffel), bei geänderter Header-Identität `HeaderImageBytes` verwerfen und Ladeeffekt anhängen.
- **Neue Methoden:** `UpdateMediaDetailsHeaderImageLoaded` (Identitäts-Abgleich + Übernahme), Hilfsmethode `WithDetailsHeaderLoad` (Muster `WithHeroLoad`).

### `MediaDetailsService` (`VideoPlayer.Mvu`)

- **Geänderte Methoden:**
  - `MapSeasons` — `SeasonItem.BannerPictureId`, `SeasonItem.Year`, `EpisodeItem.ReleaseYear` befüllen.
  - `MapShow` — `SelectedEpisodeId = highlightedEpisodeId` bei `tvshowepisode` (navigierte Episode); sonst `null`.
  - `LoadEpisodeAsync` — Standalone-Episode: `SelectedEpisodeId = episode.Id`.
  - `MapOfflineShow` — `SelectedEpisodeId = highlightedEpisodeId` bei `tvshowepisode` (Offline-Snapshot zeigt Header-Text ohne Bild — keine Bild-Ids im Cache).

### `VideoPlayerApiClient` (`VideoPlayer.ApiClient`)

- **Neue Methode:** `GetEpisodeBackgroundImageAsync(long episodeId, CancellationToken)` → `GET api/episodes/{episodeId}/background-image` → `byte[]` über `SendWithPolicyAsync` (Bearer + Re-Auth).

### `MediaDetailsPage.xaml` (`VideoPlayer.Maui`)

- Wurzel-`Grid`: `Padding="16"` entfernen; `ErrorPanel` erhält `Margin="16,0"`.
- Kopf-`Grid` (140/* mit `CachedPosterImage`) wird ersetzt durch `Grid` `HeaderSection` (erste Zeile des Scroll-Inhalts, volle Breite):
  - `Image` `HeaderImage` `AspectFill` (cover),
  - `Border` seitlicher Gradient `LinearGradientBrush` 0,0→1,0 (`VpHeroSideStrong`/`Mid`/`Soft`),
  - `Border` Bottom-Fade `LinearGradientBrush` 0,0→0,1 (`VpHeroFadeClear` → `VpBg`),
  - `BoxView` Trennlinie `VpBorder` am unteren Rand,
  - `FavoriteButton` `HorizontalOptions="End" VerticalOptions="Start"` (Web `favorite-btn`),
  - `HorizontalStackLayout` unten rechts: `DownloadButton`, `PlayButton` (Web `play-button`),
  - `VerticalStackLayout` unten links (`MaximumWidthRequest="820"`, Padding): `TitleLabel`, `MetaLabel` (`VpPrimarySoft`, fett), `PlotLabel` (`VpTextSoft`, max. Zeilen).
- `GenreLabel` entfällt (in `HeaderMeta` aufgegangen); `PlotLabel` wandert in den Header.
- Darunter `VerticalStackLayout Padding="16" Spacing="12"` mit `DownloadNoticeLabel`, `SeasonSection`, `CollectionSection` (unverändert).

### `MediaDetailsPage.xaml.cs` (`VideoPlayer.Maui`)

- **Geänderte Methoden:**
  - `UpdateUi` — befüllt `HeaderTitle`/`HeaderMeta`/`HeaderPlot`, Header-Bild aus `state.HeaderImageBytes` per Referenzvergleich (`appliedHeaderBytes`, Muster `HomePage.UpdateHero`); Schalter-Logik (`IsPlayable`, Offline) unverändert.
  - `OnSizeChanged` (neu registriert) — `HeaderSection.HeightRequest = Math.Clamp(Width * 0.52, 360, 620)` (Web `clamp(360px,52vw,620px)`).
- **Neue Methoden:** `UpdateHeader` (Texte + Bild), `OnSizeChanged`.
- `mvuHost.ModelChanged`-Abo bleibt wie gehabt (bestehendes Leck-Muster nicht verschärfen; kein neues Abo hinzufügen).

### `ShellAppEffectHandler` (`VideoPlayer.Maui`)

- **Geänderte Methode:** Effekt-Switch um `LoadMediaDetailsHeaderEffect` erweitern.
- **Neue Methode:** `LoadMediaDetailsHeaderAsync` — Episoden-Still vs. `api/pictures`-Abruf, Fehlschlag → `null` + Debug-Log (Muster `LoadHeroBackgroundAsync`).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — keine neue Anwendereingabe.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Layout-Padding:** Das Entfernen des Wurzel-Paddings betrifft `LoadingIndicator` und `ErrorPanel` — beide erhalten Ränder über `Margin`, damit sie nicht an den Seiten kleben.
- **Staffelwechsel-Verhalten:** `SeasonSelectedMessage` setzt neu `SelectedEpisodeId` — der Header zeigt danach Episoden-Infos (Web-Parität). `IsHighlighted` der `EpisodeItem`s bleibt unverändert (wird in der UI ohnehin nicht gerendert); `HighlightedEpisodeTitle` für Play-/Download-Titel der *navigierten* Episode bleibt unberührt.
- **Bild-Bytes im Model:** Header-Bilder sind groß (Backdrop); sie leben flüchtig in `MediaDetailsState` und werden beim Identitätswechsel (`MediaDetailsRequested`) verworfen — paralleler Aufwand wie beim Hero akzeptabel.
- **Race-Bedingungen:** Staffelwechsel während laufendem Bildabruf — die Header-Identität in der Nachricht verhindert verspätete Übernahme falscher Bilder.
- **Legacy-App:** `VideoWebPlayer.Maui` bleibt unberührt.

## Umsetzungsreihenfolge

1. **API-Client: `GetEpisodeBackgroundImageAsync`**
   - Voraussetzungen: keine (Muster `GetPictureAsync` vorhanden).
   - Beschreibung: Methode + Endpunkt-Testfall ergänzen.
2. **MVU-View-Erweiterungen**
   - Voraussetzungen: keine.
   - Beschreibung: `SeasonItem.BannerPictureId`/`Year`, `EpisodeItem.ReleaseYear`, `MediaDetailsView.SelectedEpisodeId` + berechnete Header-Eigenschaften; `MediaDetailsService`-Mapping.
3. **MVU-Zustand, Effekt, Nachricht**
   - Voraussetzungen: Schritt 2 (Header-Identitäten).
   - Beschreibung: `MediaDetailsState.HeaderImageBytes`/`IsHeaderImageLoading`/`HasHeaderImage`, `LoadMediaDetailsHeaderEffect`, `MediaDetailsHeaderImageLoadedMessage`.
4. **`AppUpdate`-Übergänge**
   - Voraussetzungen: Schritt 3.
   - Beschreibung: `Update`-Switch, `UpdateMediaDetailsLoaded`, `UpdateSeasonSelected`, `UpdateMediaDetailsHeaderImageLoaded`, `WithDetailsHeaderLoad`.
5. **Effekt-Ausführung**
   - Voraussetzungen: Schritte 1 und 3.
   - Beschreibung: `ShellAppEffectHandler`-Switch + `LoadMediaDetailsHeaderAsync`.
6. **Seiten-UI**
   - Voraussetzungen: Schritte 2–4.
   - Beschreibung: `MediaDetailsPage.xaml` Header-Grid + Padding-Umstellung, `MediaDetailsPage.xaml.cs` Befüllung + Höhe.
7. **Tests**
   - Voraussetzungen: Schritte 1–6.
   - Beschreibung: neue/erweiterte Testklassen (s. u.).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `EndpointCases`-Eintrag `EpisodeBackgroundImage` | `VideoPlayerApiClientTests_Endpoints` | `GET /api/episodes/{id}/background-image` |
| `GetEpisodeBackgroundImageAsync_SendsBearerToken` | `VideoPlayerApiClientTests_Endpoints` | Bearer-Header am Endpunkt |
| `MediaDetailsLoaded_Show_EmitsHeaderPictureEffect` | `AppUpdateTests_DetailsHeader` | Effekt mit `HeaderPictureId` = Banner |
| `MediaDetailsLoaded_Episode_EmitsEpisodeStillEffect` | `AppUpdateTests_DetailsHeader` | Effekt mit `HeaderEpisodeId` |
| `MediaDetailsLoaded_Season_UsesSeasonBanner` | `AppUpdateTests_DetailsHeader` | `HeaderPictureId` = Staffel-Banner, Fallback Serie |
| `MediaDetailsLoaded_NoHeaderSource_NoEffect` | `AppUpdateTests_DetailsHeader` | kein Effekt ohne Bild-Identität |
| `MediaDetailsLoaded_Offline_NoHeaderEffect` | `AppUpdateTests_DetailsHeader` | kein Abruf ohne `Connected` |
| `HeaderImageLoaded_MatchingIdentity_AppliesBytes` | `AppUpdateTests_DetailsHeader` | Bytes im State, `IsHeaderImageLoading` false |
| `HeaderImageLoaded_StaleIdentity_Discarded` | `AppUpdateTests_DetailsHeader` | falsche Medien-/Header-Identität verworfen |
| `HeaderImageLoaded_Failure_StaysStable` | `AppUpdateTests_DetailsHeader` | `null` → kein Absturz, `IsHeaderImageLoading` false |
| `SeasonSelected_SelectsFirstEpisode_ReloadsHeader` | `AppUpdateTests_DetailsHeader` | `SelectedEpisodeId`, Header-Effekt mit neuer Episoden-Id, alte Bytes verworfen |
| `SeasonSelected_EmptySeason_ShowsSeasonHeader` | `AppUpdateTests_DetailsHeader` | `SelectedEpisodeId = null`, Effekt mit Staffel-Banner |
| `MediaDetailsView_HeaderText` | `AppUpdateTests_DetailsHeader` | `HeaderTitle`/`HeaderMeta`/`HeaderPlot` je Kontext (Show, Season, Episode, Movie) |
| `MapShow_SeasonBannerAndYears` | `MediaDetailsServiceTests` | `SeasonItem.BannerPictureId`-Kette, `Year`, `EpisodeItem.ReleaseYear` |
| `LoadAsync_Episode_SetsSelectedEpisodeId` | `MediaDetailsServiceTests` | navigierte Episode → `SelectedEpisodeId` = Episoden-Id |
| `MapOfflineShow_NoPictureIds_HeaderStable` | `MediaDetailsServiceTests` | Offline-Snapshot ohne Bild-Identität |
| `MediaDetailsPage_FullWidthHeader` | `MediaDetailsPageContractTests` | XAML: `HeaderSection`, `AspectFill`, `VpHero*`-Gradienten, `VpBorder`-Trennlinie, kein Wurzel-Padding, `FavoriteButton` im Header |
| `MediaDetailsPage_CodeBehind_HeaderBinding` | `MediaDetailsPageContractTests` | `HeaderImageBytes`→`ImageSource.FromStream`, `Math.Clamp(Width * 0.52, 360, 620)`, `HeaderTitle`/`HeaderMeta`/`HeaderPlot` |
| `ShellAppEffectHandler_HeaderLoadEffect` | `MediaDetailsPageContractTests` | `LoadMediaDetailsHeaderEffect`, `GetEpisodeBackgroundImageAsync`, `GetPictureAsync`, `MediaDetailsHeaderImageLoadedMessage` |
| `MediaDetailsView_HeaderIdentityMembers` | `MediaDetailsPageContractTests` | `HeaderEpisodeId`/`HeaderPictureId`/`SelectedEpisodeId` im View-Source |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppUpdateTests_MediaDetails.SeasonSelected_SwitchesSelectedSeason` | Bleibt gültig (die dort gewählte Staffel 22 hat keine Episoden und die Sicht trägt kein `BannerPictureId` — keine neue Effekt-Wirkung); die neue `SelectedEpisodeId`-Wirkung wird in `AppUpdateTests_DetailsHeader` nachgewiesen |

Darüber hinaus keine bestehenden Tests betroffen.

### E2E-Tests (primärer Funktionsnachweis)

Das Repository enthält keinerlei UI-E2E-Infrastruktur für die MAUI-App
(kein Playwright/Appium/FlaUI im Testprojekt — nur xUnit auf `net10.0`);
die MAUI-Schicht ist dort nicht instanziierbar. Der etablierte Funktions-
nachweis für UI-Schritte (Schritt 5 Hero, Schritt 7 Startseite) sind
MVU-Übergangstests plus Quell-Vertragstests — dieser Plan folgt demselben
Muster. Keine bestehenden E2E-Tests betroffen.

## Offene Punkte

Keine.
