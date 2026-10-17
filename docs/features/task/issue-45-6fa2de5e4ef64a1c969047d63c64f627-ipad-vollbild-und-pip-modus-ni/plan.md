# Umsetzungsplan: iPad — Vollbild- und PiP-Schaltflächen der nativen Wiedergabesteuerung frei erreichbar (Issue #45)

## Übersicht

Auf dem iPad blockieren die App-Overlays im oberen Bereich des
`PlayerRoot`-Grids die native Transportleiste des
`AVPlayerViewController` (`MediaElement` mit
`ShouldShowPlaybackControls="True"`): Das `OverlayChrome`-Elternlayout
trägt kein `InputTransparent` (nur der innere `Border`), und die
interaktive `PlaylistNavigationBar` liegt im oberen rechten Bereich, in
dem iOS die Vollbild-/PiP-Buttons einblendet. Umgesetzt wird eine reine
Layout-/Hit-Test-Korrektur in `PlayerPage.xaml` plus einer
`#if IOS`-Positionsanpassung in `PlayerPage.xaml.cs`; MVU-Kern, Dienste
und das Windows-Verhalten bleiben unverändert. Der Nachweis erfolgt über
erweiterte Contract-Tests plus einen dokumentierten manuellen
iPad-Prüfschritt (TestFlight) — iOS-UI-Automatisierung existiert im
Repository nicht.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Hit-Test-Blocker `OverlayChrome` | `InputTransparent="True"` am `VerticalStackLayout` selbst (zusätzlich zum bestehenden `InputTransparent` am inneren `Border`) | Unter iOS hit-testet das Elternlayout trotz transparentem Inhalt in seinen eigenen Bounds und fängt die Eingaben der nativen Leiste ab. Das Band enthält ausschließlich `Label`s — die Teilbaum-Deaktivierung, die `InputTransparent` unter Windows bewirkt, ist hier ohne Funktionsverlust. |
| `PlaylistNavigationBar` (interaktiv) | Unter iOS per `#if IOS` im Konstruktor tiefer positioniert: `PlaylistNavigationBar.Margin = new Thickness(0, 96, 16, 0)`; der XAML-Wert `0,64,16,0` bleibt der Windows-/Standard-Pfad | Die Bar muss klickbar bleiben — sie darf weder `InputTransparent` werden noch im eingabedurchlässigen Band liegen. Damit die obere rechte Ecke (Vollbild-/PiP-Buttons der nativen Leiste, typ. ~50–70 pt hoch) frei bleibt, wird sie unter iOS unter die Leiste geschoben. Der konservative Wert `96` deckt die Leistenhöhe inkl. Sicherheitsabstand ab; Feinjustage erfolgt in der Geräteprüfung. |
| Plattform-Mechanismus | Kompilierzeit-Verzweigung `#if IOS` im Code-Behind — kein `OnPlatform`/`OnIdiom` im XAML, keine `DeviceInfo.Idiom`-Laufzeitprüfung | `#if`-Blöcke sind das etablierte Muster im Projekt (`#if WINDOWS` in `PlayerPage.xaml.cs`, `#if IOS \|\| MACCATALYST` in `AppStorageLayoutProvider.cs`); `OnPlatform`/`OnIdiom`/`DeviceInfo.Idiom` kommen nirgends vor und würden eine zweite Konvention einführen. |
| Geltungsbereich | Korrektur gilt plattformweit für iOS (iPad und iPhone) | Der `AVPlayerViewController` blendet die obere Leiste auf beiden Idioms ein; die Eingabedurchlässigkeit des Bands ist auf dem iPhone ebenso korrekt, und die tiefer positionierte `PlaylistNavigationBar` ist dort optisch neutral. Eine Idiom-Unterscheidung brächte Komplexität ohne Mehrwert. |
| Eingriffstiefe | Reine MAUI-Layout-/Hit-Test-Korrektur — **kein** eigener MediaElement-Mapper/-Handler am `AVPlayerViewController` | Die genannte Ursache (Bounds-Hit-Test der App-Layouts) liegt vollständig auf MAUI-Ebene; ein Handler-Eingriff wäre invasiver Plattformcode ohne erkennbaren Zusatznutzen. Bleibt die Geräteprüfung hinter dem Ziel zurück, ist der Handler-Mapper der dokumentierte Eskalationspfad (Follow-up, nicht Teil dieser Umsetzung). |

## Programmabläufe

### Overlay-Rendering und Bedienpfad auf iOS (iPad)

1. `PlayerPage`-Konstruktor: `InitializeComponent` baut das
   `PlayerRoot`-Grid; `PipButton.IsVisible =
   pictureInPicture.IsToggleSupported` bleibt — unter iOS `false`, der
   unsichtbare Button hit-testet nicht und steht der nativen Leiste
   nicht im Weg.
2. Unter `#if IOS` setzt der Konstruktor
   `PlaylistNavigationBar.Margin` auf `new Thickness(0, 96, 16, 0)` —
   die interaktive Bar liegt damit unterhalb der nativen Top-Leiste des
   `AVPlayerViewController`.
3. `UpdateUi` schaltet die Sichtbarkeiten unverändert:
   `OverlayChrome.IsVisible = !player.IsPictureInPicture`,
   `PlaylistNavigationBar.IsVisible = player.IsPlaylistPlayback &&
   !player.IsPictureInPicture` — Mini-Player-/Fehler-Regeln bleiben wie
   sie sind.
4. `OverlayChrome` (`VerticalStackLayout`, `VerticalOptions="Start"`,
   jetzt `InputTransparent="True"`) zeigt weiterhin Titel,
   `PlaylistContextLabel` und `WatchedBadge`, reicht aber Zeiger-Eingaben
   in seinen gesamten Bounds an das `MediaElement` bzw. den nativen
   `AVPlayerViewController` durch — die eingeblendeten Vollbild- und
   PiP-Buttons sind erreichbar und bedienbar.
5. `PlaylistNavigationBar` bleibt voll interaktiv (eigenes
   `PlayerRoot`-Kind außerhalb eingabedurchlässiger Eltern) und
   überlappt die native Leiste nicht mehr.
6. Windows-Pfad unverändert: `#if WINDOWS`-Tap auf `PlayerRoot` →
   `TransportControls.Show()` in `OnPlayerTapped`, sichtbarer
   `PipButton`, Mini-Player-Schaltung über
   `TogglePictureInPictureEffect` → `IPictureInPictureService.ToggleAsync()`
   → `PictureInPictureChangedMessage` → Overlay-Ausblendung in
   `UpdateUi`.

Beteiligte Klassen/Komponenten: `PlayerPage` (XAML + Code-Behind),
`MvuHost`/`AppModel.Player` (`PlayerState` — unverändert),
`IPictureInPictureService`/`PictureInPictureService` (unverändert),
`MediaElement`/`AVPlayerViewController` (nativ, kein eigener Handler).

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `PlayerPage` — XAML (`VideoPlayer.Maui/Pages/PlayerPage.xaml`)

- **`OverlayChrome` (`VerticalStackLayout`):** neues Attribut
  `InputTransparent="True"` — das Elternlayout wird in seinen gesamten
  Bounds eingabedurchlässig (nicht nur der innere `Border`).
- **Kommentarblock (Zeilen 30–37):** um die iOS-Begründung ergänzen —
  unter iOS hit-testet ein Layout auch bei eingabedurchlässigem Inhalt
  in den eigenen Bounds weiter und fängt die Eingaben der nativen
  Transportleiste ab; `InputTransparent` gehört daher ans Elternlayout.
  Die Vorgabe „keine interaktiven Elemente im Band" bleibt bestehen.
- **`PlaylistNavigationBar`:** bleibt XAML-seitig unverändert
  (`Margin="0,64,16,0"` als Windows-/Standard-Wert, `HorizontalOptions="End"`,
  `VerticalOptions="Start"`); die iOS-Abweichung erfolgt im Code-Behind —
  `OnPlatform` wird im Projekt bewusst nicht eingeführt.
- **`PipButton`, `ErrorPanel`, `LoadingIndicator`, `VideoElement`:**
  unverändert (`ShouldShowPlaybackControls="True"` bleibt der geforderte
  Bedienweg).

### `PlayerPage` — Code-Behind (`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`)

- **Geänderter Konstruktor `PlayerPage(MvuHost, IPictureInPictureService)`:**
  nach `InitializeComponent()`/`PipButton.IsVisible`-Zuweisung ein
  `#if IOS`-Block, der `PlaylistNavigationBar.Margin` auf
  `new Thickness(0, 96, 16, 0)` setzt (mit erläuterndem Kommentar: iOS
  blendet die native Top-Leiste mit Vollbild-/PiP-Buttons im oberen
  Randbereich ein; die interaktive Bar muss darunter liegen, da sie
  nicht eingabedurchlässig sein darf). Windows- und sonstige Pfade
  bleiben unverändert.
- Keine weiteren Änderungen: `UpdateUi`-Sichtbarkeitsregeln,
  `OnPlayerTapped`-Workaround (`#if WINDOWS`), alle Event-Handler und
  MVU-Dispatches bleiben wie sie sind.

Keine Änderungen an `PlayerState`, `AppUpdate`,
`ShellAppEffectHandler`, `PictureInPictureService` oder
`Platforms/iOS/Info.plist` (`UIBackgroundModes=audio` ist bereits
deklariert).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Windows-Hit-Test:** `InputTransparent="True"` am
  `OverlayChrome`-Elternlayout deaktiviert unter Windows den gesamten
  Teilbaum des Bands — das Band enthält nur `Label`s, es gibt nichts zu
  verlieren; der `Border` war ohnehin bereits eingabedurchlässig.
  Streng genommen wird eine ohnehin transparente Fläche nur noch
  robuster — keine Regression.
- **`PlaylistNavigationBar` unter iOS:** nur bei Playlist-Wiedergabe
  sichtbar; der tiefere Top-Margin (`96` statt `64`) ist eine rein
  optische Verschiebung — die Bedienbarkeit bleibt voll erhalten.
- **iPhone:** die plattformweite Korrektur trifft auch das iPhone —
  eingabedurchlässiges Band und tiefer liegende Playlist-Navigation sind
  dort ohne Nachteil; kein separates Idiom-Verhalten nötig.
- **`ErrorPanel` (unten):** kann theoretisch die untere native Leiste
  (Scrubber) überdecken — nur im Fehlerfall sichtbar und nicht Teil der
  Anforderung; bleibt unverändert.
- **Fallback-Risiko:** reicht die Layout-Korrektur in der Geräteprüfung
  nicht (Restüberdeckung der nativen Leiste), wäre ein eigener
  iOS-Mapper am `AVPlayerViewController` des Toolkit-`MediaElement`
  nötig — als Eskalationspfad dokumentiert, bewusst nicht Teil dieser
  Umsetzung.
- **Bestehende Tests:** der Contract-Pin
  `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` wird um eine
  Assertion erweitert, nicht gebrochen; die Windows-E2E-Suite
  (`PlaylistPlaybackTests` u. a.) steuert die Buttons per DevApi statt
  über physische Zeiger und bleibt unberührt (Positionsänderung gilt nur
  iOS).

## Umsetzungsreihenfolge

1. **`InputTransparent` am `OverlayChrome`-Elternlayout setzen und
   Kommentar ergänzen** (`PlayerPage.xaml`)
   - Voraussetzungen: keine (bestehendes XAML).
   - Beschreibung: `InputTransparent="True"` am
     `VerticalStackLayout x:Name="OverlayChrome"` ergänzen; den
     Kommentarblock um die iOS-Hit-Test-Begründung erweitern.

2. **iOS-Positionierung der `PlaylistNavigationBar`**
   (`PlayerPage.xaml.cs`)
   - Voraussetzungen: keine.
   - Beschreibung: `#if IOS`-Block im Konstruktor —
     `PlaylistNavigationBar.Margin = new Thickness(0, 96, 16, 0)` mit
     erläuterndem Kommentar (native Top-Leiste bleibt frei; Bar bleibt
     interaktiv außerhalb eingabedurchlässiger Eltern).

3. **Contract-Tests anpassen/ergänzen** (`PlayerContractTests`)
   - Voraussetzungen: Schritte 1–2 umgesetzt.
   - Beschreibung:
     `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` um die
     Assertion `InputTransparent="True"` am `OverlayChrome`-Layout
     selbst erweitern; neuen Test
     `PlayerPage_PlaylistNavigationBarClearsNativeControlsOnIos`
     anlegen (siehe Abschnitt „Tests").

4. **Build und Testsuite verifizieren**
   - Voraussetzungen: Schritte 1–3 umgesetzt.
   - Beschreibung: `dotnet test
     VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
     --configuration Release` (grün) und `dotnet build
     VideoPlayer.App.sln` in Debug und Release fehlerfrei
     (Akzeptanzkriterium 5).

5. **Manuelle iPad-Verifikation per TestFlight**
   - Voraussetzungen: Schritte 1–4 abgeschlossen; TestFlight-Build des
     iOS-Targets.
   - Beschreibung: Dokumentierter Geräte-Prüfschritt (Szenarien siehe
     Abschnitt „Tests" → manueller E2E-Prüfschritt). Zeigt die Prüfung
     Restblockaden, Margin feinjustieren bzw. den dokumentierten
     Handler-Fallback als Follow-up eskalieren.

6. **Hilfedokumentation anpassen** (Lifecycle-Schritt Dokumentation)
   - Voraussetzungen: Schritte 1–5 abgeschlossen.
   - Beschreibung: `docs/help/wiedergabe-pip.md` — Abschnitt „Bedienung
     in der Vollansicht" um die iOS-Eingabedurchlässigkeit des
     Elternlayouts und die tiefer positionierte Playlist-Navigation
     ergänzen; die iOS-Verifikationsgrenze (keine Automatisierung,
     TestFlight-Prüfung) im Muster früherer iOS-Fixe dokumentieren.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` (Erweiterung) | `PlayerContractTests` | Zusätzlich zu den Bestands-Assertions: `InputTransparent="True"` am `OverlayChrome`-`VerticalStackLayout` selbst (Elternlayout, nicht nur am inneren `Border`) |
| `PlayerPage_PlaylistNavigationBarClearsNativeControlsOnIos` (neu) | `PlayerContractTests` | Der `#if IOS`-Block im `PlayerPage`-Konstruktor setzt `PlaylistNavigationBar.Margin`; die Bar ist direktes `PlayerRoot`-Kind (nicht im eingabedurchlässigen Band) und trägt selbst kein `InputTransparent` — interaktiv bleibt sie garantiert |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` (`PlayerContractTests`) | Erweiterung um die `InputTransparent`-Assertion am Elternlayout; alle bestehenden Assertions (`VerticalOptions="Start"`, `InputTransparent` am `Border`, kein `Button` im Band, `PipButton` als eigenes Grid-Element) bleiben gültig |

### E2E-Tests (primärer Funktionsnachweis)

Kein neuer automatisierter E2E-Test ist planbar: Die Anforderung betrifft
ausschließlich das Zusammenspiel der App-Overlays mit der nativen
`AVPlayerViewController`-Transportleiste unter iOS — im Repository
existiert keine iOS-UI-Automatisierung (dokumentierte Einschränkung), und
die Windows-E2E-Suite (FlaUI/UIA3) erreicht die native iOS-
Steuerungsebene grundsätzlich nicht. Der Funktionsnachweis erfolgt daher
über (a) die erweiterten Contract-Tests, die die Hit-Test-Anordnung
quelltextbasiert pinnen, und (b) den dokumentierten manuellen
TestFlight-Prüfschritt auf dem iPad — dieselbe Verifikationsgrenze wie
bei früheren iOS-Fixes (z. B. `IosInfoPlist_DeclaresAudioBackgroundMode`,
iOS-Resume-Seek-Fix).

**Manueller E2E-Prüfschritt (Ersatz für den nicht automatisierbaren
iOS-Nachweis):**

| Priorität | Szenario | Abgedecktes Akzeptanzkriterium |
|-----------|----------|-------------------------------|
| Pflicht | iPad: Wiedergabe starten, native Leiste einblenden, Vollbild-Button antippen → Vollbild öffnet sich; zurückkehren funktioniert | AK 1 |
| Pflicht | iPad: PiP-Button der nativen Leiste antippen → PiP startet (Hintergrund-Audio-Modus greift) | AK 1 |
| Pflicht | iPad: Playlist-Wiedergabe → „Zurück"/„Weiter" der `PlaylistNavigationBar` auslösbar; Titel, `PlaylistContextLabel`, `WatchedBadge` sichtbar | AK 2, 3 |
| Pflicht | Windows: Mini-Player-Schalter und Transportsteuerung unverändert bedienbar | AK 4 |

**Angepasste bestehende E2E-Tests:** Keine —
`PlaylistPlaybackTests` steuern `PlayerPreviousButton`/`PlayerNextButton`
per `DevApi.InvokeAsync`, nicht über physische Zeiger; die
Positionsänderung gilt ohnehin nur unter iOS, das Windows-Layout bleibt
unverändert.

## Offene Punkte

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
| 1 | Die exakte Höhe/Position der nativen Vollbild-/PiP-Buttons relativ zur `PlaylistNavigationBar` ist ohne Gerät nicht verlässlich bestimmbar — der gewählte iOS-Top-Margin (`96`) ist eine konservative Schätzung. | TestFlight-Geräteprüfung vor Abschluss (Schritt 5): zeigt sie Restüberdeckung, den Margin-Wert anpassen oder die Bar weiter unter das Kopfband verlegen; erst bei ausbleibendem Erfolg den dokumentierten Fallback (eigener iOS-Mapper am `AVPlayerViewController`) als separates Follow-up angehen. |
