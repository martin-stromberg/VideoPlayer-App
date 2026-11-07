# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

Hinweis zum Prozess: Unteragenten standen in diesem Lauf nicht zur
Verfügung; das Review wurde sequenziell vom selben Agenten geführt
(Abweichung zum Lifecycle dokumentiert in `todo.md`).

## Umgesetzte Planelemente

- [x] `Colors.xaml` — Template-Palette vollständig entfernt; alle 18
  `Vp*`-Web-Tokens mit exakten Werten plus `VpOverlay` (`#80000000`),
  `VpShadow` (`#000000`) und `VpShadowBrush` vorhanden.
- [x] `Styles.xaml` — sämtliche `AppThemeBinding`-Setter durch
  `StaticResource`-Verweise auf `Vp*`-Tokens ersetzt (kein
  `AppThemeBinding` mehr im Projekt).
- [x] `Button`-Style — `CornerRadius` 4, `PointerOver` →
  `VpPrimaryHover`, Disabled → `VpTextMuted`/`VpSurfaceHigh`
  (`Styles.xaml`, Zeilen 31–57).
- [x] `IconButton`-Style — `CornerRadius` 999, `VpOverlay`-Fläche,
  `VpPrimarySoft`-Text (`Styles.xaml`, Zeilen 61–86); angewendet auf
  `FavoriteButton`, `DownloadButton` und die Episoden-Schalter ▶/⤓ in
  `MediaDetailsPage.xaml` (Zeilen 56, 64, 140, 156).
- [x] `Page`/`Shell`/`NavigationPage`/`TabbedPage` — Hintergründe auf
  `VpBg`/`VpBgDeep`, Shell/Flyout/TabBar in Web-Farbgebung
  (`Styles.xaml`, Zeilen 417–447).
- [x] `AppShell.xaml` — `SourceSectionTemplate`/`SourceInfoTemplate` →
  `VpTextMuted`, `SourceErrorTemplate` → `VpDanger` (Zeilen 20, 30, 39).
- [x] `App.xaml.cs` — `UserAppTheme = AppTheme.Dark` (Zeile 28) mit
  Kommentar.
- [x] `HomePage.xaml` — beide Banner auf `VpSurfaceHigh` +
  `VpBorderStrong`, Texte `VpText`.
- [x] `MediaCarousel.xaml` — Karte `VpSurface`, Progress
  `VpPrimary`/`VpSurfaceBright`, Badge `VpOverlay` mit Radius 999,
  Schatten `VpShadowBrush`, Texte `VpText`.
- [x] `MediaDetailsPage.xaml` — alle Hexwerte/Literale ersetzt,
  Icon-Buttons mit `IconButton`-Style, Episoden-Karte `VpSurface`.
- [x] `MediaDetailsPage.xaml.cs` — `Colors.Red`/`Color.FromArgb` durch
  `VpDanger`/`VpSecondary` aus `Application.Current.Resources` ersetzt
  (Zeilen 145–148).
- [x] `DownloadsPage.xaml` — Banner, Karte, Indikator, Progress und
  Fehlerfarben auf Tokens.
- [x] `FavoritesPage.xaml` — Farben auf Tokens, Poster-Radius 6 → 8.
- [x] `ProfilePage.xaml` — Farben auf Tokens, Überschriften
  `VpPrimarySoft`.
- [x] `SourceOverviewPage.xaml` — Genre-Chip pill-förmig (999), Farben
  auf Tokens inkl. Selektions-Trigger (`VpPrimary`/`VpText`).
- [x] `PlayerPage.xaml` — Videofläche `VpBgDeep`, Overlays `VpOverlay`,
  Indikator `VpPrimary`, „Angesehen" `VpSecondary`, Fehler `VpDanger`.
- [x] `LoginPage.xaml`, `SettingsPage.xaml`, `ServerSetupPage.xaml` —
  `Red` → `VpDanger`, `Gray` → `VpTextMuted`.
- [x] `Platforms/Android/Resources/values/colors.xml` —
  `colorPrimary` `#e50914`, `colorPrimaryDark` `#c0000c`,
  `colorAccent` `#00a8e8`.
- [x] `VideoPlayer.Maui.csproj` — `MauiIcon` `Color="#131313"`,
  `MauiSplashScreen` `Color="#0e0e0e"`.
- [x] `DesignTokensContractTests` (`VideoWebPlayer.Maui.Tests`) —
  10 Testmethoden, alle wie geplant vorhanden und grün:
  `Colors_DefinesAllWebTokens`, `Colors_DefinesOverlayAndShadowTokens`,
  `Colors_ContainsNoTemplatePalette`,
  `PagesAndControls_ContainNoHardcodedColors`,
  `CodeBehind_ContainsNoHardcodedColors`, `Styles_PinsButtonAndCardRadius`,
  `Styles_PinsPillShapes`, `Styles_PinsWebBackgrounds`,
  `App_ForcesDarkTheme`, `Xaml_UsesOnlyKnownResourceKeys`.
- [x] Verifikation — `dotnet build` Debug und Release jeweils 0 Fehler,
  `dotnet test` 657/657 grün, `dotnet format whitespace
  --verify-no-changes` ohne Befund.

## Hinweise

- `VideoWebPlayer.Maui` (Legacy-App) wurde nicht verändert — der Diff
  gegen den Basisbranch enthält außer den Lifecycle-Artefakten nur
  Dateien unter `VideoPlayer.Maui/` und das neue Testfile.
- Verhaltensänderung bewusst gewollt und im Plan begründet: Die App ist
  jetzt dunkel-only (`UserAppTheme = AppTheme.Dark`), das Web-Vorbild
  kennt kein helles Theme.
