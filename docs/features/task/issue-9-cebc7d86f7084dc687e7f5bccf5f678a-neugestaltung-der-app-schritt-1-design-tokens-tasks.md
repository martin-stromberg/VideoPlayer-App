# Tasks: Einheitliches Erscheinungsbild nach Web-Vorbild (Schritt 1)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Styling | `Colors.xaml`: Template-Palette entfernen, 18 `Vp*`-Tokens + `VpOverlay`/`VpShadow`/`VpShadowBrush` einführen | Offen | — |
| 2 | Styling | `Styles.xaml`: alle `AppThemeBinding`-Setter auf `Vp*`-Tokens umstellen | Offen | — |
| 3 | Styling | `Styles.xaml`: `Button.CornerRadius` 4 + `PointerOver` `VpPrimaryHover` | Offen | — |
| 4 | Styling | `Styles.xaml`: `Page`/`Shell`/`NavigationPage`/`TabbedPage`-Hintergründe auf `VpBg`/`VpBgDeep` | Offen | — |
| 5 | Styling | `Styles.xaml`: `IconButton`-Style (CornerRadius 999, `VpOverlay`, `VpPrimarySoft`) | Offen | — |
| 6 | Styling | `AppShell.xaml`: drei Quellen-Templates auf `VpTextMuted`/`VpDanger` umstellen | Offen | — |
| 7 | Styling | `App.xaml.cs`: `UserAppTheme = AppTheme.Dark` setzen | Offen | — |
| 8 | UI | `HomePage.xaml`: Banner + `White` auf Tokens umstellen | Offen | — |
| 9 | UI | `MediaCarousel.xaml`: Karte/Progress/Badge/Shadow auf Tokens, Badge-Radius 999 | Offen | — |
| 10 | UI | `MediaDetailsPage.xaml`: alle Hex/Literale auf Tokens; Icon-Buttons mit `IconButton`-Style | Offen | — |
| 11 | UI | `MediaDetailsPage.xaml.cs`: `Colors.Red`/`#7BD88F` durch `VpDanger`/`VpSecondary`-Ressourcen ersetzen | Offen | — |
| 12 | UI | `DownloadsPage.xaml`: Banner/Karte/Indicator/Progress/Farben auf Tokens | Offen | — |
| 13 | UI | `FavoritesPage.xaml`: Farben auf Tokens, Poster-Radius 6→8 | Offen | — |
| 14 | UI | `ProfilePage.xaml`: Farben auf Tokens | Offen | — |
| 15 | UI | `SourceOverviewPage.xaml`: Genre-Chip pill (999), Farben auf Tokens | Offen | — |
| 16 | UI | `PlayerPage.xaml`: Videofläche/Overlays/Farben auf Tokens | Offen | — |
| 17 | UI | `LoginPage.xaml`, `SettingsPage.xaml`, `ServerSetupPage.xaml`: `Red`/`Gray` → Tokens | Offen | — |
| 18 | Plattform | `Platforms/Android/Resources/values/colors.xml` + `csproj` Icon-/Splash-Farben auf Web-Werte | Offen | — |
| 19 | Tests | `DesignTokensContractTests` anlegen (Token-Pins, Farb-Verbotsliste, Radien-Pins, Dark-Pin, Ressourcen-Auflösung) | Offen | — |
| 20 | Verifikation | Build Debug+Release, volle Testsuite, `dotnet format whitespace --verify-no-changes` | Offen | — |
