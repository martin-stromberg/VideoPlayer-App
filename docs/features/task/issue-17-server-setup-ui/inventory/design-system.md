# Detail: Design-System und wiederverwendbare UI-Muster

## Farbtokens (`VideoPlayer.Maui/Resources/Styles/Colors.xaml`)

| Token | Wert | Verwendung |
|-------|------|------------|
| `VpBg` / `VpBgDeep` | `#131313` / `#0e0e0e` | Seiten-/Shell-Hintergrund |
| `VpSurfaceLow` / `VpSurface` / `VpSurfaceHigh` / `VpSurfaceBright` | `#1c1b1b` / `#201f1f` / `#2a2a2a` / `#393939` | Abgestufte Flächen (Karten, Banner) |
| `VpText` / `VpTextMuted` / `VpTextSoft` | `#e5e2e1` / `#bda9a6` / `#e9bcb6` | Text |
| `VpBorder` / `VpBorderStrong` | `#1AFFFFFF` / `#6BFFB4AA` | Rahmen normal/akzentuiert |
| `VpPrimary` / `VpPrimaryHover` / `VpPrimarySoft` | `#e50914` / `#c0000c` / `#ffb4aa` | Primärakzent (rot) |
| `VpSecondary` / `VpSecondarySoft` | `#00a8e8` / `#83cfff` | Sekundärakzent (blau) |
| `VpDanger` | `#ffb4ab` | Fehlertexte |
| `VpOverlay` / `VpShadow` (+`VpShadowBrush`) | `#80000000` / `#000000` | Scrim/Badge, Schatten |
| `VpBadgeBg` / `VpBadgeBorder` | `#AD000000` / `#47FFFFFF` | Badge-Grund/Rand |
| `VpHero*` | transluzente Ableitungen | Hero-Gradienten der Startseite |

## Implizite Styles (`Resources/Styles/Styles.xaml`)

- `Border` (Z. 19–23): `Stroke=VpBorder`, `StrokeShape=Rectangle`, `StrokeThickness=1` — Karten brauchen explizit `StrokeShape="RoundRectangle 8"`.
- `Button` (Z. 31–59): `VpPrimary`-Grund, Radius 4, `MinimumHeight/WidthRequest=44`, Hover `VpPrimaryHover`, Disabled `VpSurfaceHigh`/`VpTextMuted`.
- `IconButton` (Z. 64–86): pill-förmig (999), `VpOverlay`-Grund, `VpPrimarySoft`-Text.
- `Entry` (Z. 149–169): transparent, `VpText`, Placeholder `VpTextMuted`, 44er-Mindestmaße.
- `Label` (Z. 193–210): `VpText`, 14pt; `Headline`/`SubHeadline` (Z. 212–224).
- `ActivityIndicator` (Z. 10–12): `VpPrimary`.
- `Page`/`Shell`-Hintergründe: `VpBg`/`VpBgDeep` (Pin `Styles_PinsWebBackgrounds`).
- Dark-only: `App.xaml.cs` `UserAppTheme = AppTheme.Dark` (Pin `App_ForcesDarkTheme`).

## Etablierte Muster

| Muster | Fundstelle | Merkmale |
|--------|-----------|----------|
| Karte/Banner | `HomePage.xaml` Z. 113–138 (`OfflineBanner`, `StorageNoticeBanner`) | `Border` + `RoundRectangle 8` + `Stroke=VpBorderStrong` + `BackgroundColor=VpSurfaceHigh` + `Padding=12,8` |
| Kicker-Badge | `HomePage.xaml` Z. 86–96 | `Border` + `RoundRectangle 4` + `Stroke=VpBorder` + `BackgroundColor=VpOverlay` + `Padding=10,4`, Label 11pt |
| Hero-Gradient | `HomePage.xaml` Z. 35–73 | `Border.Background` = `LinearGradientBrush`/`RadialGradientBrush` mit `VpHero*`-Tokens |
| Trennlinie | `HomePage.xaml` Z. 77–79 | `BoxView Color=VpBorder HeightRequest=1` |
| Sektionen | `SettingsPage.xaml` Z. 18–92 | `Label` Bold 16pt als Abschnittstitel, `Spacing=12`, `Padding=20` |
| Karten-Templates | `DownloadsPage.xaml`, `FavoritesPage.xaml`, `MediaDetailsPage.xaml`, `ProfilePage.xaml` | `RoundRectangle 8` (Pin `Styles_PinsButtonAndCardRadius`) |

## Verbote/Pins (DesignTokensContractTests)

- Keine Hexfarben (`#[0-9a-fA-F]{3,8}`) und keine benannten Farbliterale in `Pages/*.xaml`, `Controls/*.xaml`, `AppShell.xaml`.
- Jeder `{StaticResource …}`-Schlüssel muss in den XAML-Dateien der App definiert sein.
- Keine `Colors.*`/`Color.From*`/`new Color(` in Page-/Control-Codebehind.
- Kartenradius 8, Button-Radius 4, Chips/Pills 999.
