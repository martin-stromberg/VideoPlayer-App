# Inventur: UI und Design-Rahmen

## Episoden-Template (`MediaDetailsPage.xaml`)

`EpisodesCollection` (Z. 156-233), `DataTemplate x:DataType="mvu:EpisodeItem"`:

- Karten-`Border` (`RoundRectangle 8`, `VpSurface`), `DataTrigger` auf
  `IsPlaceholder` dimmt den Eintrag (Opacity 0,55).
- Inhalt: `Grid ColumnDefinitions="110,*,Auto,Auto"` —
  `controls:CachedPosterImage` (110×62, `AspectFill`, `PictureId`-Binding),
  Textspalte (`DisplayTitle` in `VpPrimarySoft`, `Plot`, Platzhalter-Hinweis),
  Download- und Play-`Button` (jeweils per Trigger bei `IsPlaceholder`
  ausgeblendet).
- Das Poster ist direktes Grid-Kind — für ein Overlay-Badge muss es in ein
  eigenes `Grid` gehüllt werden.
- **Kein Watched-Badge vorhanden**; `IsVisible`-Bindungen auf `EpisodeItem`
  funktionieren im Template (`ShowUnavailable` wird bereits so gebunden).

Vergleichbares Overlay-Muster im Projekt: `Controls/MediaCarousel.xaml:60-70`
(`PlaceholderBadge`: `Border` mit `RoundRectangle 999`, `VpOverlay`-Grund,
`IsVisible`-Binding über einem Bild-`Grid`).

## Web-Referenz (verifiziert am Server-Quellstand)

`WatchedIndicator.razor` — SVG-Auge, `viewBox="0 0 24 24"`, Titel „Gesehen",
`data-testid="watched-indicator"`:

- Augenkontur: `M2 12C4 7 8 4 12 4C16 4 20 7 22 12C20 17 16 20 12 20C8 20 4 17 2 12Z`,
  `fill="none"`, `stroke="currentColor"`, `stroke-width="1.6"`.
- Pupille: Kreis `(12, 12)`, `r=3,2`, `fill="currentColor"`.
- Glanzpunkt: Kreis `(13,3; 10,9)`, `r=0,9`, `#ffffff`, Opacity 0,9.

`app.css` `.watched-indicator` (+ Episoden-/Links-Varianten):

| Eigenschaft | Wert |
|-------------|------|
| Position | absolut, `top: 0.6rem`, `left: 0.6rem` (`-image-left`), `z-index: 3` |
| Größe | `2rem` × `2rem` (`-episode`), `padding: 0.22rem` |
| Farbe | `color: var(--vp-primary-soft)` |
| Rand | `1px solid rgba(255,255,255,0.28)` |
| Radius | `999px` (Kreis) |
| Hintergrund | `rgba(0,0,0,0.68)` + Schatten `0 2px 8px rgba(0,0,0,.42)` |
| `pointer-events` | `none` |

Einbindung: `TVShowDetails.razor:151` — in der `episode-box` direkt nach dem
Poster, nur bei `episode.WatchedAt.HasValue`.

## Design-Tokens (`Resources/Styles/Colors.xaml`)

Vorhanden: `VpBg`, `VpBgDeep`, `VpSurfaceLow`…`VpSurfaceBright`, `VpText`,
`VpTextMuted`, `VpTextSoft`, `VpBorder` (#1AFFFFFF), `VpBorderStrong`,
`VpPrimary`, `VpPrimaryHover`, `VpPrimarySoft` (#ffb4aa), `VpSecondary`,
`VpSecondarySoft`, `VpDanger`, `VpOverlay` (#80000000, Scrim/Badge-Grund),
`VpShadow`/`VpShadowBrush`, `VpHero*` (Gradienten Schritt 5).

Für das Badge fehlen: Hintergrund `rgba(0,0,0,.68)` ≙ `#AD000000` (`VpOverlay`
ist mit #80000000 heller und per `DesignTokensContractTests` gepinnt) sowie
der feine Rand `rgba(255,255,255,.28)` ≙ `#47FFFFFF`. Der Glanzpunkt (weiß)
kann `VpText` (#e5e2e1) nutzen — Farbliteral `White` ist in Seiten-XAML per
Contract-Test verboten.

## Contract-Rahmen (`DesignTokensContractTests`)

- `Colors_DefinesAllWebTokens`/`Colors_DefinesOverlayAndShadowTokens` pinnen
  die dokumentierten Token-Werte (inkl. `VpOverlay` = #80000000) — neue Tokens
  sind zulässig, müssen `Vp*` heißen (`Colors_ContainsNoTemplatePalette`).
- `PagesAndControls_ContainNoHardcodedColors`: keine `#…`-Hexfarben und keine
  Farbliterale (u. a. `White`, `Black`) in `Pages/`, `Controls/`,
  `AppShell.xaml`.
- `CodeBehind_ContainsNoHardcodedColors`: keine `Colors.*`/`Color.From*`/
  `new Color(` in Code-Behind von Pages/Controls/Shell.
