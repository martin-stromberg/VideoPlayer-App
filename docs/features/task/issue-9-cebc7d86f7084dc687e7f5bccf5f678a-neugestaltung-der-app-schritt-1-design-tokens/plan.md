# Umsetzungsplan: Einheitliches Erscheinungsbild nach Web-Vorbild (Schritt 1)

## Übersicht

Die neue MAUI-App `VideoPlayer.Maui` wird vollständig auf die Design-Tokens der
Webanwendung umgestellt: `Colors.xaml` erhält die dokumentierten `--vp-*`-Werte
als zentrale Ressourcen, `Styles.xaml` wird auf diese Tokens umgestellt
(inkl. Button-Radius 4, Seitenhintergrund `#131313`, Shell/Flyout in
Web-Farbgebung), und alle ~50 hartcodierten Hexfarben sowie benannten
Farbliterale in den Seiten, `AppShell.xaml`, `Controls/MediaCarousel.xaml` und
`MediaDetailsPage.xaml.cs` werden durch `StaticResource`-Verweise ersetzt.
Reine Design-Umstellung ohne Verhaltensänderung. `VideoWebPlayer.Maui`
(Legacy) bleibt unangetastet.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Token-Benennung | Schlüssel spiegeln die CSS-Variablen: `VpBg`, `VpBgDeep`, `VpSurfaceLow`, `VpSurface`, `VpSurfaceHigh`, `VpSurfaceBright`, `VpText`, `VpTextMuted`, `VpTextSoft`, `VpBorder`, `VpBorderStrong`, `VpPrimary`, `VpPrimaryHover`, `VpPrimarySoft`, `VpSecondary`, `VpSecondarySoft`, `VpDanger` | 1:1-Nachvollziehbarkeit zur Web-Referenz (`web-design-recherche.md`); eindeutig pinnbar in Contract-Tests; kein Namenskonflikt mit der zu entfernenden Template-Palette. |
| `rgba`-Tokens | Als ARGB-Hex `Color`-Ressourcen: `VpBorder` = `#1AFFFFFF` (α 0,1), `VpBorderStrong` = `#6BFFB4AA` (α 0,42) | XAML/`Color.FromArgb`-Format; Werte exakt die Web-Alphas. |
| Theme-Festlegung | `UserAppTheme = AppTheme.Dark` im `App`-Konstruktor; sämtliche `AppThemeBinding` in `Styles.xaml`/`AppShell.xaml` werden durch `StaticResource` auf die `Vp*`-Tokens ersetzt | Die Webanwendung ist dunkel-only; die Anforderung verlangt „durchgängig" das dunkle Erscheinungsbild. Ohne Pin gäbe es unter Light-Theme ungestylte/halbdunkle Zustände; zwei Farbwelten zu pflegen ist nicht gefordert und würde Tokens doppeln. |
| Template-Palette | Alle bisherigen Schlüssel (`Primary`, `Gray*`, `OffBlack`, `Magenta`, `MidnightBlue`, `White`, `Black`, `*Brush` u. a.) werden aus `Colors.xaml` entfernt; jede Referenz wird auf `Vp*`-Tokens umgestellt | Akzeptanzkriterium „alle Tokens abgebildet, keine verstreuten Werte"; tote Schlüssel würden zukünftige Verwendung falscher Farben einladen. |
| Transluzente Overlays | Zusätzliches App-Token `VpOverlay` = `#80000000` (Scrim) für Player-Kopf-Overlay, Fehler-Overlay und Platzhalter-Badge (bisher `#80000000`/`#80404040`/`#AA000000`) sowie `VpShadow`/`VpShadowBrush` = `#000000` für Karten-Schatten | Die Web-Tokenliste enthält kein Overlay-/Schatten-Token; die App braucht transluzente Abdunklungen (Web-Badge `rgba(0,0,0,.68)` ist dasselbe Muster). Zentral definiert statt verstreut. |
| Warn-/Hinweisbanner | `VpSurfaceHigh`-Hintergrund, `Stroke` `VpBorderStrong`, Text `VpText` (bisher `#D9A029`/`#B06020` mit `Black`/`White`-Text) | Die Web-Tokenliste kennt kein Amber/Warnung; das Web-Muster für Hinweise sind abgesetzte Flächen mit betontem Rahmen. Vermeidet ein nicht dokumentiertes Zusatztoken. |
| Semantische Zuordnung | Akzent (bisher Orange `#FFB84D` bei Indikatoren/Fortschritt/Selektion/Überschriften-Akzent) → `VpPrimary`; hervorgehobene Titel `#FFDDAA` → `VpPrimarySoft`; gedämpfter Text `#BBBBBB`/`Gray` → `VpTextMuted`; Fehler `Red`/`#FF8A80` → `VpDanger`; Erfolg `#7BD88F`/`#9CCC65` → `VpSecondary`; Karten `#23232a` → `VpSurface`, `#17171c` → `VpSurfaceLow`, `#40404a` → `VpSurfaceBright`; Videofläche `Black` → `VpBgDeep`; Text `White`/`Black` → `VpText`/`VpBg` | Web-Farbrollen: Primärrot ist der Aktions-/Akzentfarbton, `primary-soft` ist die Akzent-Textfarbe (Meta/Titel im Web-Header), `danger` ist die sichtbare Fehlerfarbe auf dunklem Grund. Erfolgshinweise haben kein Web-Token — Sekundärblau ist der neutral-positive Akzent. |
| Runde Icon-Schaltflächen | Neuer benannter Style `IconButton` (`TargetType="Button"`, `BasedOn` impliziter Button-Style): `CornerRadius="999"`, `BackgroundColor` `VpOverlay`, `TextColor` `VpPrimarySoft`, `Padding 0`; angewendet auf die Glyphen-Schalter `FavoriteButton`/`DownloadButton` und die Episoden-Buttons ▶/⤓ in `MediaDetailsPage.xaml` | Web-Vorbild: `favorite-btn` ist eine kreisrunde, halbdunkle Schaltfläche mit `primary-soft`-Symbol — nicht ein voller Primär-Button. |
| Chips/Badges | Genre-Chip `RoundRectangle 14` → `999`, Platzhalter-Badge `RoundRectangle 4` → `999`; Poster-Thumbnail `RoundRectangle 6` → `8` (Kartenradius) | Web: Chips/Badges pill-förmig (999px), Karten 8px. |
| Button-Hover | `PointerOver`-VisualState des globalen Button-Styles bekommt `BackgroundColor` `VpPrimaryHover` | Web `btn-primary:hover` = `#c0000c`; bisher leerer VisualState — tokengetreue Abbildung, Windows-relevant. |
| Android-Plattformfarben | `Platforms/Android/Resources/values/colors.xml` auf `Vp`-Werte setzen (`colorPrimary #e50914`, `colorPrimaryDark #c0000c`, `colorAccent #00a8e8`) | Entfernt letzte Template-Lila-Reste; Android ist kein Ziel, das Target wird aber mitgebaut — konsistent statt verwaist. |

## Programmabläufe

Es gibt keine neuen oder geänderten Programmabläufe — reine
Design-Umstellung. Einziger Code-Eingriff:

### Download-Hinweisfarbe aus Theme-Ressource

1. `MediaDetailsPage.UpdateUi` setzt `DownloadNoticeLabel.TextColor` je nach
   `state.DownloadNoticeIsError`.
2. Statt `Colors.Red`/`Color.FromArgb("#7BD88F")` werden die `Color`-
   Ressourcen `VpDanger`/`VpSecondary` aus `Application.Current.Resources`
   gelesen (Schlüssel durch Contract-Test gepinnt).

Beteiligte Klassen/Komponenten: `MediaDetailsPage`, `Application.Resources`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `DesignTokensContractTests` | Testklasse (`VideoWebPlayer.Maui.Tests`) | Contract-Tests, die die Token-Werte in `Colors.xaml`, die Abwesenheit hartcodierter Farben in Seiten/Steuerelementen/Code-Behind, die Radius-Pins (Button 4, Chips/Badges/Icon-Buttons 999, Karten 8) und den Dark-Theme-Pin sichern (Muster: `SourcesContractTests`/`VideoPlayerMauiContractTests` über `RepositoryRoot`). |

## Änderungen an bestehenden Klassen

### `Colors.xaml` (Ressourcen)

- Entfernen der kompletten Template-Palette (`Primary`, `PrimaryDark`,
  `PrimaryDarkText`, `Secondary`, `SecondaryDarkText`, `Tertiary`, `White`,
  `Black`, `Magenta`, `MidnightBlue`, `OffBlack`, `Gray100`–`Gray950`,
  sämtliche `*Brush`-Schlüssel).
- Neu: 18 `Vp*`-`Color`-Tokens gemäß Tabelle oben (`#131313`, `#0e0e0e`,
  `#1c1b1b`, `#201f1f`, `#2a2a2a`, `#393939`, `#e5e2e1`, `#bda9a6`,
  `#e9bcb6`, `#1AFFFFFF`, `#6BFFB4AA`, `#e50914`, `#c0000c`, `#ffb4aa`,
  `#00a8e8`, `#83cfff`, `#ffb4ab`) sowie `VpOverlay` `#80000000`,
  `VpShadow` `#000000` und `VpShadowBrush` (`SolidColorBrush` auf
  `VpShadow`).

### `Styles.xaml` (Ressourcen)

- Alle `AppThemeBinding`-Setter auf `StaticResource Vp*` umstellen:
  `ActivityIndicator.Color` → `VpPrimary`; `IndicatorView` →
  `VpSurfaceBright`/`VpText`; `Border.Stroke` → `VpBorder`; `BoxView` →
  `VpSurfaceBright`; `Button` → Text `VpText`, Bg `VpPrimary`,
  `CornerRadius 4`, `PointerOver` Bg `VpPrimaryHover`, Disabled Text
  `VpTextMuted`/Bg `VpSurfaceHigh`; `CheckBox.Color` → `VpPrimary`;
  Eingabecontrols (`DatePicker`, `Editor`, `Entry`, `Picker`, `SearchBar`,
  `SearchHandler`, `TimePicker`, `RadioButton`) → Text `VpText`,
  Placeholder/Titel `VpTextMuted`, Disabled `VpTextMuted`; `Label` →
  `VpText` (+ `Headline`/`SubHeadline` → `VpText`); `ProgressBar` →
  `VpPrimary`; `RefreshView` → `VpPrimary`; `Shadow` → `VpShadowBrush`;
  `Slider` → `VpPrimary`/`VpSurfaceHigh`; `SwipeItem` → `VpSurfaceHigh`;
  `Switch` → On `VpPrimary`, Thumb `VpText`, Disabled/Off `VpTextMuted`;
  `Page.BackgroundColor` → `VpBg`; `Shell` → `BackgroundColor` `VpBg`,
  `FlyoutBackgroundColor` `VpBgDeep` (neu), `ForegroundColor`/`TitleColor`
  `VpText`, `DisabledColor`/`UnselectedColor` `VpTextMuted`,
  `TabBarBackgroundColor` `VpBgDeep`, `TabBarForegroundColor`/
  `TabBarTitleColor` `VpPrimary`, `TabBarUnselectedColor` `VpTextMuted`;
  `NavigationPage` → Bar `VpBg`/`VpText`; `TabbedPage` → `VpBgDeep`/
  `VpText`/`VpTextMuted`/`VpPrimary`.
- Neuer benannter Style `IconButton` (s. Designentscheidungen).

### `AppShell.xaml` (Shell)

- `SourceSectionTemplate.TextColor` → `VpTextMuted` (statt Gray600/Gray300).
- `SourceErrorTemplate.TextColor` → `VpDanger` (statt `Red`).
- `SourceInfoTemplate.TextColor` → `VpTextMuted` (statt Gray500/Gray400).

### `App.xaml.cs` (App)

- `UserAppTheme = AppTheme.Dark` im Konstruktor setzen (mit kurzem
  Kommentar: App ist dunkel-only wie die Webanwendung).

### Seiten/Controls (XAML)

Ersetzung aller hartcodierten Farben und Farbliterale gemäß Mapping-Tabelle
(s. Designentscheidungen). Konkret:

- `HomePage.xaml`: Offline-/Storage-Banner → `VpSurfaceHigh` + `Stroke`
  `VpBorderStrong`, Text `VpText`; `TextColor="White"` → `VpText`.
- `MediaCarousel.xaml`: Karte `#23232a` → `VpSurface`; Progress
  `#FFB84D`/`#40404a` → `VpPrimary`/`VpSurfaceBright`; Badge `#AA000000`
  → `VpOverlay`, Radius 4 → 999; `White` → `VpText`; `Shadow Brush` →
  `VpShadowBrush`.
- `MediaDetailsPage.xaml`: Indicator `#FFB84D` → `VpPrimary`; `Red` →
  `VpDanger`; `#FFDDAA` → `VpPrimarySoft`; `#BBBBBB` → `VpTextMuted`;
  Episoden-Karte `#23232a` → `VpSurface`; `FavoriteButton`,
  `DownloadButton`, Episoden-Buttons → `Style="{StaticResource IconButton}"`.
- `DownloadsPage.xaml`: `#BBBBBB` → `VpTextMuted`; Banner →
  `VpSurfaceHigh`/`VpBorderStrong`/`VpText`; Indicator+Progress `#FFB84D` →
  `VpPrimary`; Karte → `VpSurface`; `#FFDDAA` → `VpPrimarySoft`; `Red` →
  `VpDanger`; `Shadow Brush` → `VpShadowBrush`.
- `FavoritesPage.xaml`: analog; Poster-Thumb `#17171c` → `VpSurfaceLow`,
  Radius 6 → 8; `Shadow Brush` → `VpShadowBrush`.
- `ProfilePage.xaml`: analog; Sektionsüberschriften `#FFB84D` →
  `VpPrimarySoft`; `Shadow Brush` → `VpShadowBrush`.
- `SourceOverviewPage.xaml`: Genre-Chip `#23232a` → `VpSurface`, Radius 14
  → 999; Chip-Text `#FFDDAA` → `VpText`; selektiert: Bg `#FFB84D` →
  `VpPrimary`, Text `Black` → `VpText`; Indikatoren `#FFB84D` →
  `VpPrimary`; `Red` → `VpDanger`; `#FFDDAA` → `VpPrimarySoft`.
- `PlayerPage.xaml`: Videofläche `Black` → `VpBgDeep`; Indicator `#FFB84D`
  → `VpPrimary`; Kopf-Overlay `#80000000` → `VpOverlay`; `#FFDDAA` →
  `VpPrimarySoft`; „Angesehen" `#9CCC65` → `VpSecondary`; Fehler-Overlay
  `#80404040` → `VpOverlay`; Fehlertext `#FF8A80` → `VpDanger`.
- `LoginPage.xaml`/`SettingsPage.xaml`/`ServerSetupPage.xaml`: `Red` →
  `VpDanger`, `Gray` → `VpTextMuted`.
- `MediaDetailsPage.xaml.cs`: Zeile 143 — `Colors.Red` →
  `(Color)Application.Current!.Resources["VpDanger"]`,
  `Color.FromArgb("#7BD88F")` →
  `(Color)Application.Current!.Resources["VpSecondary"]`.

### `Platforms/Android/Resources/values/colors.xml`

- `colorPrimary` `#e50914`, `colorPrimaryDark` `#c0000c`,
  `colorAccent` `#00a8e8`.

### `VideoPlayer.Maui.csproj` (Projektdatei)

- `MauiIcon` `Color="#512BD4"` → `#131313` (App-Icon-Kachel in
  Seitenhintergrund-Schwarz statt Template-Lila).
- `MauiSplashScreen` `Color="#000000"` → `#0e0e0e` (`VpBgDeep`).
- Die Icon-/Splash-SVG-Grafiken selbst bleiben unverändert (Artwork ist
  kein Design-Token; Umbau wäre über den Schritt hinaus).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Light-Theme:** Die App wird durchgehend dunkel dargestellt — gewollt
  (Web-Vorbild ist dunkel-only). Wer ein helles Theme erwartet hätte, bekommt
  die dunkle App — entspricht der Anforderung „durchgängig dunkel".
- **Kontraste:** `#ffb4ab` (Danger) auf `#131313`/`VpOverlay` ist das im Web
  bewährte Paar; Banner-Text `VpText` auf `VpSurfaceHigh` analog zu
  Web-Hinweisflächen. Manuelle Sichtprüfung der Seiten unter Windows
  empfohlen (kein automatisierter Render-Test vorhanden).
- **Contract-Test-Empfindlichkeit:** Die neuen Tests pinnen Token-Werte und
  verbieten Farbliterale — künftige Seitenänderungen müssen die Tokens
  weiterverwenden (beabsichtigt).
- **`MediaDetailsPage.xaml.cs` Ressourcen-Zugriff:** Schlüssel sind durch
  Contract-Test gepinnt; Fehlschlüssel würde `KeyNotFoundException` werfen —
  Test `Colors_DefinesAllWebTokens` deckt Existenz ab.
- **Android:** `colors.xml`-Werte ändern natives Theme/Splash; kein
  Ziel-Feature, aber konsistent.

## Umsetzungsreihenfolge

1. **`Colors.xaml` auf Web-Tokens umstellen**
   - Voraussetzungen: Keine.
   - Beschreibung: Template-Palette entfernen, 18 `Vp*`-Tokens +
     `VpOverlay`/`VpShadow`/`VpShadowBrush` einführen.
2. **`Styles.xaml` umstellen + `IconButton`-Style**
   - Voraussetzungen: Schritt 1 (Tokens vorhanden).
   - Beschreibung: alle Setter auf `Vp*`-Tokens, `Button.CornerRadius` 4,
     `PointerOver` `VpPrimaryHover`, Shell/Page/Navigation auf
     `VpBg`/`VpBgDeep`, `IconButton`-Style neu.
3. **`App.xaml.cs`: `UserAppTheme = AppTheme.Dark`**
   - Voraussetzungen: Keine.
4. **`AppShell.xaml`-Templates auf Tokens umstellen**
   - Voraussetzungen: Schritt 1.
5. **Seiten/Controls und `MediaDetailsPage.xaml.cs` umstellen**
   - Voraussetzungen: Schritte 1–2.
   - Beschreibung: alle Hexfarben/Farbliterale durch `StaticResource`
     ersetzen; `IconButton`-Style anwenden; Chip-/Badge-Radien auf 999.
6. **`Platforms/Android/Resources/values/colors.xml` und
   `VideoPlayer.Maui.csproj`-Icon/Splash-Farben aktualisieren**
   - Voraussetzungen: Keine.
7. **`DesignTokensContractTests` anlegen**
   - Voraussetzungen: Schritte 1–6 (Tests pinnen den Endzustand).
8. **Verifikation:** `dotnet build VideoPlayer.App.sln` Debug + Release,
   `dotnet test VideoWebPlayer.Maui.Tests`,
   `dotnet format whitespace --verify-no-changes` (DoD).
   - Voraussetzungen: Schritte 1–7.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Colors_DefinesAllWebTokens` | `DesignTokensContractTests` | Alle 18 Web-Tokens mit exaktem Wert in `Colors.xaml` (XML-geparst). |
| `Colors_DefinesOverlayAndShadowTokens` | `DesignTokensContractTests` | `VpOverlay`, `VpShadow`, `VpShadowBrush` vorhanden. |
| `Colors_ContainsNoTemplatePalette` | `DesignTokensContractTests` | Keine Schlüssel `Gray*`, `OffBlack`, `Magenta`, `MidnightBlue`, `Primary*`, `Secondary*`, `Tertiary`, `White`, `Black`, `*Brush` (außer `VpShadowBrush`). |
| `PagesAndControls_ContainNoHardcodedColors` | `DesignTokensContractTests` | Kein `#[0-9a-fA-F]{3,8}` und keine benannten Farbliterale (`Red`, `Black`, `White`, `Gray`, …) in `Pages/*.xaml`, `Controls/*.xaml`, `AppShell.xaml`. |
| `CodeBehind_ContainsNoHardcodedColors` | `DesignTokensContractTests` | Kein `Colors.`, `Color.FromArgb`/`FromRgb(a)`/`FromHex` in `Pages/*.xaml.cs`, `Controls/*.cs`, `AppShell.xaml.cs`. |
| `Styles_PinsButtonAndCardRadius` | `DesignTokensContractTests` | Impliziter `Button`-Style `CornerRadius` 4; Kartenradius 8 (Ressource/Setter oder geprüfte `RoundRectangle 8`-Verwendung in Karten-Templates). |
| `Styles_PinsPillShapes` | `DesignTokensContractTests` | `IconButton`-Style `CornerRadius` 999; Genre-Chip `RoundRectangle 999`; Platzhalter-Badge 999. |
| `Styles_PinsWebBackgrounds` | `DesignTokensContractTests` | `Page.BackgroundColor` → `VpBg`, `Shell.BackgroundColor` → `VpBg`, `Shell.FlyoutBackgroundColor` → `VpBgDeep`. |
| `App_ForcesDarkTheme` | `DesignTokensContractTests` | `UserAppTheme = AppTheme.Dark` in `App.xaml.cs`. |
| `Xaml_UsesOnlyKnownResourceKeys` | `DesignTokensContractTests` | Jeder `{StaticResource X}`-Verweis in `VideoPlayer.Maui`-XAML löst auf einen in `Colors.xaml`/`Styles.xaml` definierten Schlüssel auf (keine toten Verweise). |

### Betroffene bestehende Tests

Keine — die bestehenden Contract-Tests pinnen Struktur/Namen, nicht Farben;
die XAML-Bezeichner (`x:Name`, Bindings, Elemente) bleiben unverändert.

### E2E-Tests (primärer Funktionsnachweis)

Keine erforderlich — Begründung: Die Anforderung ist eine reine
Design-Umstellung ohne neuen oder geänderten Benutzerfluss; es gibt keine
neue Interaktion, deren Happy Path per E2E nachzuweisen wäre. Zudem existiert
im Repository keine Rendering-/E2E-Infrastruktur für MAUI (dokumentierte
Konvention: MVU-Tests + quelltextbasierte Contract-Tests). Der Funktionsnachweis
„Tokens angewendet, keine verstreuten Farben" wird über die
`DesignTokensContractTests` geführt; die Unverändertheit der Bedienung über
die bestehende Testsuite (647 Tests) und die DoD-Builds. Eine manuelle
Sichtprüfung auf Windows/iOS bleibt empfohlen (nicht automatisierbar).

## Offene Punkte

Keine.
