# Abnahmeprüfung – Entwicklungsschritt 1

Prüfgegenstand: `git diff task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app...HEAD` (Commit `0eebdbe`), geprüft gegen die Schritt-Anforderung aus `project-plan.md`, nicht gegen die Lifecycle-Artefakte.

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

(keine)

## Hinweise

**Token-Werte (a):** `VideoPlayer.Maui/Resources/Styles/Colors.xaml` enthält alle geforderten Tokens exakt: `VpBg #131313`, `VpBgDeep #0e0e0e`, `VpSurfaceLow #1c1b1b`, `VpSurface #201f1f`, `VpSurfaceHigh #2a2a2a`, `VpSurfaceBright #393939`, `VpText #e5e2e1`, `VpTextMuted #bda9a6`, `VpTextSoft #e9bcb6`, `VpPrimary #e50914`, `VpPrimaryHover #c0000c`, `VpPrimarySoft #ffb4aa`, `VpSecondary #00a8e8`, `VpSecondarySoft #83cfff`, `VpDanger #ffb4ab`. Die rgba-Rahmen sind korrekt als ARGB abgebildet: `VpBorder #1AFFFFFF` (α 0x1A ≈ 0,102 ≈ 0,1) und `VpBorderStrong #6BFFB4AA` (α 0x6B ≈ 0,42, RGB 255/180/170). Die alte MAUI-Template-Palette ist vollständig entfernt. Zwei Hilfstokens (`VpOverlay #80000000`, `VpShadow #000000` + `VpShadowBrush`) sind zusätzlich definiert und fachlich begründet (Scrim/Karten-Schatten, Web-Muster rgba(0,0,0,.68)).

**Keine hartcodierten Farben (b):** Systematisches Greppen (`#[0-9a-fA-F]{3,8}`, benannte Farbliterale, `Colors.`/`Color.From*`/`new Color(`) über ganz `VideoPlayer.Maui` (inkl. `*.xaml.cs`): In allen Seiten, `AppShell.xaml` (inkl. der dynamischen Quellen-Templates), `Controls/MediaCarousel.xaml` und `MediaDetailsPage.xaml.cs` keine einzige hartcodierte Farbe mehr; alle Farben kommen per `{StaticResource Vp*}` bzw. aus den globalen Styles. Verbleibende Hexwerte sind fachlich begründet: die Token-Definitionen selbst, `VideoPlayer.Maui.csproj` (MauiIcon `#131313`/Splash `#0e0e0e` — Build-Zeit-Plattformressourcen, die keine XAML-Ressourcen referenzieren können), `Platforms/Android/Resources/values/colors.xml` (Token-Werte), die SVG-Bildassets `appicon.svg`/`appiconfg.svg`/`splash.svg` (Icon-/Splash-Grafik, keine Seiten/Steuerelemente) und ein URL-Anker in `Entitlements.plist` (Falschpositiv).

**Radien (c):** Globaler `Button`-Style `CornerRadius 4` (`Styles.xaml:37`), neuer `IconButton`-Style `CornerRadius 999` (pill), in `MediaDetailsPage.xaml` auf die Symbol-Schalter (Download, Favorit, Episoden-Aktionen) angewendet. Karten durchgängig `RoundRectangle 8` (Downloads-, Favorites-, Profile-, MediaDetailsPage, MediaCarousel `CornerRadius="8"`); Genre-Chip (`SourceOverviewPage.xaml:32`) und Platzhalter-Badge (`MediaCarousel.xaml:68`) pill-förmig (999). `PointerOver`-Hover in `VpPrimaryHover` ist im Button-Style verankert.

**Hintergründe/Navigation (d):** `Page`-Style `VpBg`, `Shell.BackgroundColor VpBg`, `Shell.FlyoutBackgroundColor`/`TabBarBackgroundColor VpBgDeep`, `NavigationPage`/`TabbedPage` in Web-Farbgebung; `App.xaml.cs` pinnt `UserAppTheme = AppTheme.Dark`, sämtliche `AppThemeBinding`-Reste sind entfernt (einziger Treffer ist ein Kommentar).

**Contract-Tests echt (e):** `VideoWebPlayer.Maui.Tests/DesignTokensContractTests.cs` (10 Tests) pinnt alle Token-Werte, Radien, Hintergründe, Dark-Theme und verbietet verstreute Farben plus unbekannte `StaticResource`-Schlüssel. Pin-Nachweis: `VpPrimary` testweise auf `#e50915` geändert → `Colors_DefinesAllWebTokens` schlug erwartungsgemäß fehl („Expected #e50914, Actual #e50915"); Rückbau erfolgt, Arbeitsbaum danach sauber (`git status` zeigt nur die fremde untracked Datei `docs/features/task/issue-9-customer-feedback.md`, unverändert).

**Build/Tests:** `dotnet build VideoPlayer.App.sln` Debug: 0 Fehler (281 Warnungen, allesamt Altbestand in `VideoWebPlayer.Maui` u. a.); Release: 0 Fehler (339 Warnungen, ebenfalls Altbestand). `dotnet test VideoWebPlayer.Maui.Tests`: 657/657 grün (Baseline vor dem Schritt: 647 → +10 neue Contract-Tests, kein vorhandener Test angepasst oder deaktiviert).

**Gestalterische Mapping-Entscheidungen (keine Abweichung, zur Kenntnis):** Frühere Grün-Töne für Erfolg/„Angesehen" (`#7BD88F`, `#9CCC65`) wurden auf `VpSecondary` (`#00a8e8`) abgebildet; die früher gelben/orangen Offline-Banner (`#D9A029`, `#B06020`) sind jetzt dunkle Flächen (`VpSurfaceHigh`) mit betontem Rahmen (`VpBorderStrong`). Die Web-Palette kennt kein Grün/Gelb — die Zuordnung ist konsistent mit den Tokens und rein optisch; Verhalten unverändert. `VpTextSoft` und `VpSecondarySoft` sind definiert, aber aktuell ungenutzt — die Anforderung verlangt nur die Token-Definition.

**Prozess-Beobachtung (außerhalb der Fachanforderung):** Im Projektordner existiert keine `handover.md` (AGENTS.md §2); `steps.md` führt Schritt 1 weiterhin als „In Arbeit" — Statuspflege obliegt dem übergeordneten Ablauf. Doku ist vollständig: `docs/help/erscheinungsbild.md` neu, `docs/help/index.md`, `README.md`, `docs/RELEASE_NOTES.md` und `changes.log` aktualisiert.
