# Tasks: Einheitliches Erscheinungsbild nach Web-Vorbild (Schritt 1)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Styling | `Colors.xaml`: Template-Palette entfernen, 18 `Vp*`-Tokens + `VpOverlay`/`VpShadow`/`VpShadowBrush` einführen | Erledigt | `DesignTokensContractTests.Colors_DefinesAllWebTokens`, `Colors_DefinesOverlayAndShadowTokens`, `Colors_ContainsNoTemplatePalette` |
| 2 | Styling | `Styles.xaml`: alle `AppThemeBinding`-Setter auf `Vp*`-Tokens umstellen | Erledigt | `DesignTokensContractTests.PagesAndControls_ContainNoHardcodedColors` + grep auf `AppThemeBinding` (0 Treffer) |
| 3 | Styling | `Styles.xaml`: `Button.CornerRadius` 4 + `PointerOver` `VpPrimaryHover` | Erledigt | `DesignTokensContractTests.Styles_PinsButtonAndCardRadius` |
| 4 | Styling | `Styles.xaml`: `Page`/`Shell`/`NavigationPage`/`TabbedPage`-Hintergründe auf `VpBg`/`VpBgDeep` | Erledigt | `DesignTokensContractTests.Styles_PinsWebBackgrounds` |
| 5 | Styling | `Styles.xaml`: `IconButton`-Style (CornerRadius 999, `VpOverlay`, `VpPrimarySoft`) | Erledigt | `DesignTokensContractTests.Styles_PinsPillShapes` |
| 6 | Styling | `AppShell.xaml`: drei Quellen-Templates auf `VpTextMuted`/`VpDanger` umstellen | Erledigt | `DesignTokensContractTests.PagesAndControls_ContainNoHardcodedColors` |
| 7 | Styling | `App.xaml.cs`: `UserAppTheme = AppTheme.Dark` setzen | Erledigt | `DesignTokensContractTests.App_ForcesDarkTheme` |
| 8 | UI | `HomePage.xaml`: Banner + `White` auf Tokens umstellen | Erledigt | `DesignTokensContractTests.PagesAndControls_ContainNoHardcodedColors` |
| 9 | UI | `MediaCarousel.xaml`: Karte/Progress/Badge/Shadow auf Tokens, Badge-Radius 999 | Erledigt | `DesignTokensContractTests.PagesAndControls_ContainNoHardcodedColors`, `Styles_PinsPillShapes`, `Styles_PinsButtonAndCardRadius` |
| 10 | UI | `MediaDetailsPage.xaml`: alle Hex/Literale auf Tokens; Icon-Buttons mit `IconButton`-Style | Erledigt | `DesignTokensContractTests.PagesAndControls_ContainNoHardcodedColors`, `Xaml_UsesOnlyKnownResourceKeys` |
| 11 | UI | `MediaDetailsPage.xaml.cs`: `Colors.Red`/`#7BD88F` durch `VpDanger`/`VpSecondary`-Ressourcen ersetzen | Erledigt | `DesignTokensContractTests.CodeBehind_ContainsNoHardcodedColors`, `Colors_DefinesAllWebTokens` |
| 12 | UI | `DownloadsPage.xaml`: Banner/Karte/Indicator/Progress/Farben auf Tokens | Erledigt | `DesignTokensContractTests.PagesAndControls_ContainNoHardcodedColors` |
| 13 | UI | `FavoritesPage.xaml`: Farben auf Tokens, Poster-Radius 6→8 | Erledigt | `DesignTokensContractTests.PagesAndControls_ContainNoHardcodedColors`, `Styles_PinsButtonAndCardRadius` |
| 14 | UI | `ProfilePage.xaml`: Farben auf Tokens | Erledigt | `DesignTokensContractTests.PagesAndControls_ContainNoHardcodedColors` |
| 15 | UI | `SourceOverviewPage.xaml`: Genre-Chip pill (999), Farben auf Tokens | Erledigt | `DesignTokensContractTests.Styles_PinsPillShapes`, `PagesAndControls_ContainNoHardcodedColors` |
| 16 | UI | `PlayerPage.xaml`: Videofläche/Overlays/Farben auf Tokens | Erledigt | `DesignTokensContractTests.PagesAndControls_ContainNoHardcodedColors` |
| 17 | UI | `LoginPage.xaml`, `SettingsPage.xaml`, `ServerSetupPage.xaml`: `Red`/`Gray` → Tokens | Erledigt | `DesignTokensContractTests.PagesAndControls_ContainNoHardcodedColors` |
| 18 | Plattform | `Platforms/Android/Resources/values/colors.xml` + `csproj` Icon-/Splash-Farben auf Web-Werte | Erledigt | Kein direkter Test (Plattform-Artefakte; Werte aus den dokumentierten Web-Tokens übernommen) |
| 19 | Tests | `DesignTokensContractTests` anlegen (Token-Pins, Farb-Verbotsliste, Radien-Pins, Dark-Pin, Ressourcen-Auflösung) | Erledigt | Die 10 Tests selbst, alle grün |
| 20 | Verifikation | Build Debug+Release, volle Testsuite, `dotnet format whitespace --verify-no-changes` | Erledigt | `test-results.md` — Debug/Release 0 Fehler, 657/657 Tests grün, Format-Check sauber |
