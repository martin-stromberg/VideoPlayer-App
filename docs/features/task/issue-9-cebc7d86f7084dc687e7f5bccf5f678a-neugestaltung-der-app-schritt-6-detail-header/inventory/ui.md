# Bestandsaufnahme — UI / Seiten

## `MediaDetailsPage.xaml`
Datei: `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml`

- Wurzel: `Grid` mit `Padding="16"`, `RowDefinitions="Auto,Auto,*"`,
  `RowSpacing="10"` — das Seiten-Padding verhindert aktuell jede
  Vollbreiten-Darstellung (Web: Header zieht über den Content-Gutter).
- Zeile 0: `ActivityIndicator` (`LoadingIndicator`), an
  `Model.MediaDetails.IsLoading` gebunden.
- Zeile 1: `ErrorPanel`/`ErrorLabel` (`VpDanger`), `IsVisible="False"`.
- Zeile 2: `ScrollView` `DetailsScroll` → `VerticalStackLayout Spacing="12"`:
  - **Kopfbereich (zu ersetzen):** `Grid` 140/* — `CachedPosterImage`
    `HeaderImage` (140×200, `AspectFill`) + Textspalte mit `TitleLabel`
    (22 pt, `VpPrimarySoft`), `PlayButton` „Abspielen", `DownloadButton` „⤓"
    (`IconButton`), `FavoriteButton` „☆" (`IconButton`), `MetaLabel`,
    `GenreLabel`, `DownloadNoticeLabel`.
  - `PlotLabel` (14 pt, WordWrap) unterhalb des Kopfs.
  - `SeasonSection`: `SeasonPicker` + `EpisodesCollection` (`EpisodeItem`-
    Template: 110×62-`CachedPosterImage`, `DisplayTitle`, `Plot`,
    „Nicht heruntergeladen", Download-/Play-Buttons mit
    `IsPlaceholder`-Trigger).
  - `CollectionSection`: „Enthaltene Filme" + `CollectionCollection`
    (3-spaltiges `GridItemsLayout`, `CollectionMovieItem`-Template).

## `MediaDetailsPage.xaml.cs`
Datei: `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs`

- `QueryProperty`-Route `mediadetails` (mediaType/id/title/show/season/episode).
- Konstruktor: `mvuHost.ModelChanged += OnModelChanged` — kein Unsubscribe
  (bekannter Querschnittsbefund aus Schritt 4; in diesem Schritt nicht
  verschärfen).
- `OnAppearing` dispatcht einmalig `MediaDetailsRequestedMessage`.
- `UpdateUi` filtert auf die angefragte Identität, befüllt Titel/Plot/Meta/
  Genre/`HeaderImage.PictureId = PosterPictureId ?? BannerPictureId`,
  Favoriten-/Play-/Download-Schalter, `UpdateSeasonSection`,
  `UpdateCollectionSection`.
- `OnSeasonSelected` → `SeasonSelectedMessage` (mit `updatingSeasonPicker`-
  Guard); `OnEpisodeSelected` → `MediaItemSelectedMessage` → Navigation zur
  Episoden-Detailseite (kein In-Place-Wechsel wie im Web); Play/Download/
  Favorit → entsprechende Messages.
- Keine `SizeChanged`-Behandlung — eine responsive Header-Höhe fehlt.

## `HomePage.xaml` / `HomePage.xaml.cs` (Muster aus Schritt 5)

- `HeroSection`-`Grid`: `Image` `AspectFill` (cover), `Border` mit
  `LinearGradientBrush` 0,0→1,0 (`VpHeroSideStrong`/`Mid`/`Soft`), zwei
  `RadialGradientBrush`-Akzente (`VpHeroAccentPrimary`/`Secondary`),
  `LinearGradientBrush` 0,0→0,1 Bottom-Fade (`VpHeroFadeClear` → `VpBg`),
  `BoxView`-Trennlinie (`VpBorder`), Inhalt `VerticalOptions="End"`,
  `MaximumWidthRequest="820"`.
- Code-Behind: `UpdateHero` wandelt `model.HomeHero.ImageBytes` per
  `ImageSource.FromStream` nur bei Referenzwechsel; `OnSizeChanged` setzt
  `HeroSection.HeightRequest = Math.Clamp(Width * 0.42, 260, 520)`.

## `Resources/Styles/Colors.xaml`
Datei: `VideoPlayer.Maui/Resources/Styles/Colors.xaml`

Vorhandene Tokens: `VpBg` `#131313`, `VpBgDeep`, `VpSurfaceLow`…`Bright`,
`VpText`, `VpTextMuted`, `VpTextSoft`, `VpBorder`, `VpBorderStrong`,
`VpPrimary`/`VpPrimaryHover`/`VpPrimarySoft`, `VpSecondary`/
`VpSecondarySoft`, `VpDanger`, `VpOverlay` — plus die Schritt-5-Gradienten
`VpHeroSideStrong` `#FA131313`, `VpHeroSideMid` `#C2131313`,
`VpHeroSideSoft` `#2E131313`, `VpHeroFadeClear` `#00131313`,
`VpHeroAccentPrimary` `#4DE50914`, `VpHeroAccentSecondary` `#3800A8E8`.

→ Für den Detail-Header reichen die vorhandenen Tokens (seitlicher Verlauf
`.98→.72→.1` entspricht `VpHeroSideStrong/Mid/Soft`, Bottom-Fade
`VpHeroFadeClear`→`VpBg`); der Header-Grund ohne Bild ist `VpSurfaceLow`.
