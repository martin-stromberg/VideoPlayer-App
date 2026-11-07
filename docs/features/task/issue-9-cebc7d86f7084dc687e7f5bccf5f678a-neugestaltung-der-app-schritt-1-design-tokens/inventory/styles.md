# Detailinventur: Styling in `VideoPlayer.Maui`

Bezug: Schritt-1-Anforderung (`requirement.md`) — Umstellung aller Farben/Radien
auf die Web-Design-Tokens aus `web-design-recherche.md`.

## `Resources/Styles/Colors.xaml`

Datei: `VideoPlayer.Maui/Resources/Styles/Colors.xaml` (45 Zeilen)

Unveränderte MAUI-Template-Palette — **kein einziges Web-Token**:

| Schlüssel | Wert | Verwendungen (`{StaticResource …}`) |
|-----------|------|--------------------------------------|
| `Primary` | `#512BD4` | 9 (Styles.xaml) |
| `PrimaryDark` | `#ac99ea` | 1 (Button-Hintergrund Dark) |
| `PrimaryDarkText` | `#242424` | 1 (Button-Text Dark) |
| `Secondary` | `#DFD8F7` | 2 |
| `SecondaryDarkText` | `#9880e5` | 2 (Shell.ForegroundColor/TitleColor Dark) |
| `Tertiary` | `#2B0B98` | 1 (TertiaryBrush, sonst ungenutzt) |
| `White` | `White` | 36 |
| `Black` | `Black` | 11 |
| `Magenta` | `#D600AA` | 3 (TabBar-Farben) |
| `MidnightBlue` | `#190649` | 2 (Headline/SubHeadline Light) |
| `OffBlack` | `#1f1f1f` | 3 (Page-/Shell-/NavBar-Hintergrund Dark) |
| `Gray100`–`Gray950` | `#E1E1E1`–`#141414` | 46 gesamt |
| `*Brush`-Schlüssel | SolidColorBrush je Farbe | nicht referenziert (0 Fundstellen außer Definition) |

## `Resources/Styles/Styles.xaml`

Datei: `VideoPlayer.Maui/Resources/Styles/Styles.xaml` (435 Zeilen)

Implizite Styles für `ActivityIndicator`, `IndicatorView`, `Border`, `BoxView`,
`Button`, `CheckBox`, `DatePicker`, `Editor`, `Entry`, `ImageButton`, `Label`
(+ `Headline`/`SubHeadline`), `Picker`, `ProgressBar`, `RadioButton`,
`RefreshView`, `SearchBar`, `SearchHandler`, `Shadow`, `Slider`, `SwipeItem`,
`Switch`, `TimePicker`, `Page`, `Shell`, `NavigationPage`, `TabbedPage`.
Alle Farbwerte über `AppThemeBinding` Light/Dark auf die Template-Palette.

Relevante Pins:

- `Button` (`Styles.xaml:26-50`): `CornerRadius="8"` (Web-Vorgabe: 4),
  `BackgroundColor` Dark=`PrimaryDark` `#ac99ea`, `TextColor`
  Dark=`PrimaryDarkText` `#242424`; `PointerOver`-VisualState leer.
- `Border` (`:16-20`): `Stroke` Gray200/Gray500, `StrokeThickness=1` —
  implizit für alle `Border`-Karten.
- `Page` (`:404-407`): `BackgroundColor` Dark=`OffBlack` `#1f1f1f`
  (Web-Vorgabe `#131313`).
- `Shell` (`:409-420`): `BackgroundColor` Dark=`OffBlack`, `ForegroundColor`/
  `TitleColor` Dark=`SecondaryDarkText` `#9880e5`, `UnselectedColor` Gray200,
  TabBar-Farben `Magenta`/Black.
- `NavigationPage` (`:422-426`): `BarBackgroundColor` Dark=`OffBlack`.
- `TabbedPage` (`:428-433`): `BarBackgroundColor` Dark=`Gray950`.
- `Label` (`:157-174`): `TextColor` Dark=`White`.
- `ProgressBar` (`:213-227`): `ProgressColor` Dark=`White`.
- `Switch`/`Slider`/`CheckBox`/`ActivityIndicator`: `Primary`/`White`-Pins.
- `Shadow` (`:299-304`): `Brush` White (beide Themes!), `Opacity 0.5` —
  implizit; Karten setzen eigenen `Shadow Brush="Black"`.

## `App.xaml`

Merged `Colors.xaml` + `Styles.xaml`. Kein `UserAppTheme`/RequestedTheme-Pin —
die App folgt dem Systemtheme (Light-Werte existieren faktisch nie gestylt,
da alle Seiten Dunkel-Farben hartcodieren; Inkonsistenz vorhanden).

## `AppShell.xaml` (Navigation/Flyout)

Datei: `VideoPlayer.Maui/AppShell.xaml` (59 Zeilen)

- `SourceSectionTemplate` (`:14-21`): `TextColor` AppThemeBinding
  Gray600/Gray300 (dynamischer Sektionstitel „Quellen").
- `SourceErrorTemplate` (`:26-31`): `TextColor="Red"` (klickbarer
  Neuversuch-Eintrag bei Ladefehlschlag).
- `SourceInfoTemplate` (`:35-40`): `TextColor` AppThemeBinding
  Gray500/Gray400 (Statuszeilen Laden/leer).
- Fünf statische `FlyoutItem`s; Shell-Chrome-Farben kommen aus dem impliziten
  `Shell`-Style in `Styles.xaml`.

## Hartcodierte Farben in Seiten und Steuerelementen (XAML)

Verifiziert per `grep -niE '#[0-9a-f]{3,8}'` über `VideoPlayer.Maui/**/*.xaml`
(ohne `Colors.xaml`):

| Datei | Zeilen | Farbwerte |
|-------|--------|-----------|
| `Controls/MediaCarousel.xaml` | 28, 54, 55, 62 | `#23232a` (Karte), `#FFB84D` (Progress), `#40404a` (Progress-Bg), `#AA000000` (Platzhalter-Badge) |
| `Pages/HomePage.xaml` | 17, 30 | `#D9A029` (Offline-Banner), `#B06020` (Storage-Banner) |
| `Pages/MediaDetailsPage.xaml` | 18, 46, 70, 73, 103, 124, 133, 180, 195 | `#FFB84D` (ActivityIndicator), `#FFDDAA` (Titel/Hervorhebungen), `#BBBBBB` (Meta/Notiz), `#23232a` (Episoden-Karte) |
| `Pages/DownloadsPage.xaml` | 18, 26, 37, 65, 73, 79, 88 | `#BBBBBB`, `#D9A029` (Offline-Banner), `#FFB84D` (Indicator+Progress), `#23232a` (Karte), `#FFDDAA` (Titel) |
| `Pages/FavoritesPage.xaml` | 21, 32, 63, 74, 98 | `#D9A029`, `#FFB84D`, `#23232a` (Karte), `#17171c` (Poster-Bg), `#FFDDAA` (Titel) |
| `Pages/ProfilePage.xaml` | 15, 22, 35, 43, 59, 67 | `#FFB84D`, `#D9A029`, `#23232a` (Karten ×2) |
| `Pages/SourceOverviewPage.xaml` | 33, 36, 41, 61, 98, 120 | `#23232a` (Genre-Chip), `#FFDDAA`, `#FFB84D` (Chip selektiert + Indikatoren) |
| `Pages/PlayerPage.xaml` | 26, 34, 41, 48, 68, 72 | `#FFB84D`, `#80000000` (Kopf-Overlay), `#FFDDAA`, `#9CCC65` („Angesehen"), `#80404040` (Fehler-Overlay), `#FF8A80` (Fehlertext) |

Zusätzlich **benannte Farbliterale** in Seiten/Shell (keine Hexwerte, aber
ebenfalls verstreute Farbwerte):

- `TextColor="Red"`: `AppShell.xaml:30`, `LoginPage.xaml:37`,
  `SettingsPage.xaml:15`, `ServerSetupPage.xaml:72`,
  `MediaDetailsPage.xaml:24`, `DownloadsPage.xaml:43,92`,
  `FavoritesPage.xaml:38`, `SourceOverviewPage.xaml:68,111`.
- `TextColor="Gray"`: `LoginPage.xaml:16`, `ServerSetupPage.xaml:33,56`.
- `TextColor="Black"` (Text auf Warnbanner): `HomePage.xaml:20`,
  `DownloadsPage.xaml:29`, `FavoritesPage.xaml:24`, `ProfilePage.xaml:25`;
  selektierter Genre-Chip `SourceOverviewPage.xaml:48`.
- `TextColor="White"`: `HomePage.xaml:34`, `MediaCarousel.xaml:42,71`,
  `FavoritesPage.xaml:80`.
- `BackgroundColor="Black"` (Videofläche): `PlayerPage.xaml:10`.
- `<Shadow Brush="Black" …/>`: `MediaCarousel.xaml:33`,
  `DownloadsPage.xaml:67`, `FavoritesPage.xaml:65`, `ProfilePage.xaml:37,61`.

## Hartcodierte Farben in Code-Behind

- `Pages/MediaDetailsPage.xaml.cs:143`:
  `DownloadNoticeLabel.TextColor = state.DownloadNoticeIsError ? Colors.Red : Color.FromArgb("#7BD88F");`
  (Fehler- vs. Erfolgshinweis der Download-Anforderung — einzige Fundstelle;
  verifiziert über `grep -niE 'Color|Brush' --include='*.cs'`).

## Radien-Ist-Stand

- Globaler `Button`-Style: `CornerRadius="8"` (Web: 4).
- Karten-`Border`s: `RoundRectangle 8` (DownloadsPage, FavoritesPage,
  MediaDetailsPage-Episoden, ProfilePage, MediaCarousel, Banner) — entspricht
  Web `radius` 8px.
- Genre-Chips (`SourceOverviewPage.xaml:32`): `RoundRectangle 14` — annähernd
  pill-förmig, aber nicht 999.
- Icon-/Glyph-Buttons (`MediaDetailsPage`: Favorite ☆, Download ⤓,
  Episoden ▶/⤓): erben `CornerRadius 8` vom Button-Style — Web: rund.
- Poster-Thumbnail in FavoritesPage: `RoundRectangle 6`.
- Platzhalter-Badge MediaCarousel: `RoundRectangle 4`.

## Plattform-Ressourcen

- `Platforms/Android/Resources/values/colors.xml`: Template-Lila
  (`colorPrimary #512BD4`, `colorPrimaryDark`/`colorAccent #2B0B98`).
  Android ist laut Anforderung kein Ziel, das Target `net10.0-android`
  existiert aber im csproj und wird mitgebaut.

## Web-Design-Tokens (Zielvorgabe, `web-design-recherche.md`)

`--vp-bg #131313`, `--vp-bg-deep #0e0e0e`, `--vp-surface-low #1c1b1b`,
`--vp-surface #201f1f`, `--vp-surface-high #2a2a2a`, `--vp-surface-bright
#393939`, `--vp-text #e5e2e1`, `--vp-text-muted #bda9a6`, `--vp-text-soft
#e9bcb6`, `--vp-border rgba(255,255,255,0.1)`, `--vp-border-strong
rgba(255,180,170,0.42)`, `--vp-primary #e50914`, `--vp-primary-hover
#c0000c`, `--vp-primary-soft #ffb4aa`, `--vp-secondary #00a8e8`,
`--vp-secondary-soft #83cfff`, `--vp-danger #ffb4ab`, `--vp-radius-sm 4px`,
`--vp-radius 8px`; Chips/Badges/Icon-Buttons pill-förmig (999px/50%).
