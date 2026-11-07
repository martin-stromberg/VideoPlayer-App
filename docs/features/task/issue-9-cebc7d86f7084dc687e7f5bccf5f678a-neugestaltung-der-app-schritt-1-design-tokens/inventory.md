# Bestandsaufnahme: Einheitliches Erscheinungsbild nach Web-Vorbild (Schritt 1)

Analysiert wurde der Styling-Bereich der neuen MAUI-App `VideoPlayer.Maui`
bezogen auf die Schritt-1-Anforderung (Umstellung auf die Web-Design-Tokens,
keine hartcodierten Farben in Seiten/Steuerelementen, Web-Farbgebung der
Shell-Navigation).

## Zusammenfassung

- `Colors.xaml` enthält ausschließlich die unveränderte MAUI-Template-Palette
  (`Primary #512BD4`, `OffBlack #1f1f1f`, Graustufen) — kein Web-Token.
- `Styles.xaml` referenziert die Palette durchgehend per `AppThemeBinding`;
  globaler `Button`-Radius ist 8 (Web: 4), Page-/Shell-Hintergrund Dark ist
  `OffBlack #1f1f1f` (Web: `#131313`/`#0e0e0e`).
- 8 XAML-Dateien in `Pages/` + `Controls/MediaCarousel.xaml` enthalten
  ~50 hartcodierte Hexfarben; zusätzlich benannte Farbliterale (`Red`,
  `Gray`, `Black`, `White`) an ~20 Stellen und eine Hexfarbe in
  `MediaDetailsPage.xaml.cs:143` (`#7BD88F`, daneben `Colors.Red`).
- `AppShell.xaml` definiert die drei Templates der dynamischen
  Quellen-Menüeinträge mit `Gray*`-/`Red`-Farben; die Shell-Chrome-Farben
  kommen aus dem impliziten `Shell`-Style in `Styles.xaml`.
- Keine `UserAppTheme`-Festlegung — die App folgt dem Systemtheme.
- `Platforms/Android/Resources/values/colors.xml` hält noch Template-Lila
  (Android kein Ziel, wird aber mitgebaut).
- Die Legacy-App `VideoWebPlayer.Maui` ist nicht Teil dieses Schritts.

Test-Ausgangszustand: `dotnet test VideoWebPlayer.Maui.Tests` — **647/647
bestanden** (Nachweis: [inventory/tests.md](inventory/tests.md),
Log unter `inventory/test-results/baseline-dotnet-test.log`).

## Details

- [Styling / Theme-Ressourcen, Seiten- und Shell-Farben, Radien](inventory/styles.md)
- [Tests und Test-Ausgangszustand](inventory/tests.md)
