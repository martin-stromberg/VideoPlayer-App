# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| App-Hintergrund = Schwarzton der Webanwendung (`#131313`/`#0e0e0e` je Fläche) | `Page.BackgroundColor` → `VpBg` `#131313`; `Shell.BackgroundColor` → `VpBg`, `FlyoutBackgroundColor`/`TabBar`/`TabbedPage` → `VpBgDeep` `#0e0e0e`; Videofläche `Black` → `VpBgDeep`; zusätzlich `UserAppTheme = AppTheme.Dark`, damit unter Light-Systemtheme keine Light-Restwerte greifen | `Styles_PinsWebBackgrounds`, `Colors_DefinesAllWebTokens`, `App_ForcesDarkTheme` | Abgedeckt |
| Alle dokumentierten Tokens in den Stilen abgebildet | 18 `Vp*`-Tokens in `Colors.xaml` (alle 16 Farbtokens inkl. beider `rgba`-Rahmen als ARGB + beide Radien-Werte via gepinnte Setter 4/8/999) | `Colors_DefinesAllWebTokens` (exakte Werte, XML-geparst), `Styles_PinsButtonAndCardRadius`, `Styles_PinsPillShapes` | Abgedeckt |
| Normale Buttons 4px; Karten 8px; Chips/Badges/Icon-Buttons pill | Globaler `Button`-Style `CornerRadius 4`; Karten bleiben `RoundRectangle 8` (+ Poster-Thumb 6→8); Genre-Chip 14→999, Platzhalter-Badge 4→999, `IconButton`-Style `CornerRadius 999` für ☆/⤓/▶-Glyphen-Schalter | `Styles_PinsButtonAndCardRadius`, `Styles_PinsPillShapes` | Abgedeckt |
| Keine hartcodierten Hexfarben in Seiten und Steuerelementen | Ersetzung aller ~50 Hexwerte + benannter Literale (`Red`/`Gray`/`Black`/`White`) + `MediaDetailsPage.xaml.cs:143` durch `StaticResource`/Ressourcen-Lesefzugriff; Template-Palette wird entfernt (keine Alt-Schlüssel mehr referenzierbar) | `PagesAndControls_ContainNoHardcodedColors` (Hex-Regex + Literale, inkl. `AppShell.xaml`), `CodeBehind_ContainsNoHardcodedColors`, `Colors_ContainsNoTemplatePalette`, `Xaml_UsesOnlyKnownResourceKeys` | Abgedeckt |
| App-Navigation (Shell/Flyout) in Web-Farbgebung inkl. dynamischer Quelleneinträge | `Shell`-Style komplett auf `Vp*` (Bg/Flyout/TabBar/Unselected); die drei Templates `SourceSectionTemplate`/`SourceErrorTemplate`/`SourceInfoTemplate` auf `VpTextMuted`/`VpDanger` | `Styles_PinsWebBackgrounds`, `PagesAndControls_ContainNoHardcodedColors` (scannt `AppShell.xaml`); bestehender Pin `AppShell_DefinesSourceSectionAndErrorTemplates` bleibt grün | Abgedeckt |
| Bedienung auf Windows und iOS unverändert | Reine Farb-/Radius-Umstellung; einziger Code-Eingriff ersetzt äquivalente Farbwerte (`Red`→`VpDanger`, `#7BD88F`→`VpSecondary`); kein Layout-/Handler-Eingriff | Gesamte bestehende Testsuite (647 Tests, Baseline grün) + `Xaml_UsesOnlyKnownResourceKeys` (keine toten Verweise → kein XAML-Laufzeitfehler) | Abgedeckt |
| Alle Tests grün; DoD (Debug+Release-Build, Testsuite, Format) | Umsetzungsschritt 8 = Verifikation: `dotnet build VideoPlayer.App.sln` Debug+Release, `dotnet test VideoWebPlayer.Maui.Tests`, `dotnet format whitespace --verify-no-changes` | Verifikationsschritt im Plan; Baseline-Nachweis in `inventory/tests.md` | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

(nicht zutreffend — Status „Plan vollständig")

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Kein neuer oder geänderter Benutzerfluss (reine Design-Umstellung; Bedienung bleibt identisch) | Keiner geplant; begründet im Plan-Abschnitt „E2E-Tests" | Nicht erforderlich mit Begründung — Anforderung berührt keinen über Nutzeraktion erreichbaren neuen Ablauf; Repo besitzt keine Rendering-/E2E-Infrastruktur für MAUI (dokumentierte Konvention). Sichtprüfung manuell empfohlen. |

## Fehlende oder unvollständige Planbestandteile

(nicht zutreffend — Status „Plan vollständig")

## Hinweise

- Explizit mitgeprüft und vom Plan erfasst: `csproj`-Icon-/Splash-Farben
  (`#512BD4`/`#000000`), `Platforms/Android/colors.xml`, benannte
  Farbliterale (nicht nur Hex), `MediaDetailsPage.xaml.cs:143`,
  `SourceOverviewPage`-Chip-Selektionsfarbe, `Shadow Brush="Black"`.
- Der Plan entfernt `AppThemeBinding` komplett und pinnt Dark-only —
  konsistent mit „durchgängig dunkles Erscheinungsbild"; dokumentierte
  Designentscheidung.
- `MediaDetailsPage.xaml.cs` liest die Tokens über
  `Application.Current.Resources` — Schlüssel-Existenz ist durch
  `Colors_DefinesAllWebTokens` gepinnt.
