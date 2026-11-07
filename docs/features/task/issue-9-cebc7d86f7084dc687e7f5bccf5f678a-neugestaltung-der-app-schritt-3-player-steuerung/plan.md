# Umsetzungsplan: Videoplayer — Steuerelemente in der Vollansicht bedienbar (Schritt 3)

## Übersicht

Auf der `PlayerPage` blockiert das App-Overlay `OverlayChrome` die
nativen Transportsteuerungen des `MediaElement`, weil das
`VerticalStackLayout` ohne `VerticalOptions` per Default `Fill` das
gesamte Wiedergabe-`Grid` belegt und Eingaben abfängt. Umgesetzt wird
der kleinste saubere Eingriff: Das Overlay wird auf sein sichtbares
Kopfband geschrumpft (`VerticalOptions="Start"`), zusätzlich bekommt
der nicht-interaktive Rest des Overlays `InputTransparent`, und ein
Contract-Test pinnt die Korrektur.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| `OverlayChrome` (Hit-Test-Blocker) | `VerticalOptions="Start"` am `VerticalStackLayout`, plus `InputTransparent="True"` auf dem `Border` (Kopfband) mit `InputTransparent="False"` am `PipButton` | `VerticalOptions="Start"` allein schrumpft die Bounds auf das Kopfband — die Videofläche darunter ist frei. `InputTransparent` auf dem `Border` mit Ausnahme des `PipButton` ist eine zusätzliche Absicherung: Selbst innerhalb des Kopfbands sollen nur die tatsächlich interaktiven Elemente (Schalter) Eingaben beanspruchen, nicht die Titelfläche — so bleibt auch der obere Rand der Videofläche (iOS: dort sitzen Teile der nativen Steuerung) durchlässig. Beides zusammen ist eine rein deklarative Layoutkorrektur ohne Code-Behind-Änderung. |
| Alternative `InputTransparent` allein am `VerticalStackLayout` | verworfen | Würde das Kopfband zwar klickdurchlässig machen, der Container bliebe aber weiter vollflächig — fehleranfällig bei zukünftigen Overlay-Erweiterungen und weniger ausdrucksstark. |
| Alternative „Overlay nur bei Bedarf einblenden" | verworfen | Größerer Eingriff (Auto-Hide-Logik, Gesten/Timer), nicht gefordert; der Mini-Player-Pfad bleibt ohnehin über `IsVisible` gesteuert. |

## Programmabläufe

### Wiedergabe in der Vollansicht

1. `PlayerPage` wird über die Route `player` geöffnet, `MediaElement`
   spielt mit nativen Transportsteuerungen (`ShouldShowPlaybackControls`).
2. `OverlayChrome` belegt nur noch das Kopfband am oberen Rand
   (`VerticalOptions="Start"`); der übrige Videobereich ist nicht
   mehr von einem Layout belegt → Zeigeeingaben erreichen die nativen
   Steuerelemente (Play/Pause, Seek, Lautstärke, Vollbild/PiP auf iOS).
3. Innerhalb des Kopfbands ist nur der `PipButton` eingabefähig
   (`InputTransparent="False"`); Titel/`WatchedBadge`-Fläche leitet
   Eingaben an das `MediaElement` weiter.
4. `PipButton.Clicked` → `OnPipClicked` →
   `PictureInPictureRequestedMessage` → `TogglePictureInPictureEffect`
   → Mini-Player; `UpdateUi` blendet `OverlayChrome` dort wie bisher
   aus (`IsVisible = !IsPictureInPicture`) — unverändert.

Beteiligte Klassen/Komponenten: `PlayerPage` (XAML), `MediaElement`,
`PipButton`, `ShellAppEffectHandler` (unverändert),
`PictureInPictureService` (unverändert).

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `PlayerPage` (XAML-Seite, `VideoPlayer.Maui/Pages/PlayerPage.xaml`)

- **Geändertes Element `OverlayChrome` (`VerticalStackLayout`):**
  Attribut `VerticalOptions="Start"` ergänzen — das Layout belegt nur
  noch die Höhe seines Kopfbands am oberen Rand statt das gesamte
  Grid.
- **Geändertes Element `Border` (Kopfband innerhalb `OverlayChrome`):**
  Attribut `InputTransparent="True"` ergänzen — die Titelfläche ist
  für Eingaben durchlässig.
- **Geändertes Element `PipButton`:** Attribut
  `InputTransparent="False"` ergänzen — explizit eingabefähig trotz
  transparentem Eltern-`Border` (MAUI vererbt `InputTransparent` auf
  Kinder; die explizite Ausnahme sichert die Schalter-Funktion auch
  gegen künftige Vererbungsänderungen).

Keine Änderungen im Code-Behind (`PlayerPage.xaml.cs`), am MVU-Kern
oder an Services.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Kopfband-Bedienung:** `PipButton` bleibt klickbar (explizites
  `InputTransparent="False"`); Titel und „Angesehen"-Badge sind reine
  Anzeige, Verlust ihrer (ohnehin nicht vorhandenen) Interaktivität
  ist beabsichtigt.
- **iOS:** `OverlayChrome` ist dort ebenfalls sichtbar (Titelband);
  die Schrumpfung auf `Start` verbessert auch dort die Erreichbarkeit
  der nativen Steuerung (u. a. PiP-Schaltfläche in der
  Transportsteuerung). `PipButton` ist auf iOS ohnehin ausgeblendet
  (`IsToggleSupported == false`) — kein Funktionsverlust.
- **Mini-Player:** `OverlayChrome.IsVisible = !IsPictureInPicture`
  bleibt unverändert; im CompactOverlay ist das Overlay weiter
  ausgeblendet, die dortige Bedienung ist unberührt.
- **Design-Tokens:** Es werden keine Farben angefasst; die
  `Vp*`-Token-Verwendung bleibt unverändert
  (`DesignTokensContractTests` bleiben grün).

## Umsetzungsreihenfolge

1. **XAML-Korrektur `PlayerPage.xaml`**
   - Voraussetzungen: Keine.
   - Beschreibung: `VerticalOptions="Start"` an `OverlayChrome`,
     `InputTransparent="True"` am Kopfband-`Border`,
     `InputTransparent="False"` am `PipButton`.

2. **Contract-Test-Pin in `PlayerContractTests`**
   - Voraussetzungen: Schritt 1 (der Test pinnt den neuen Zustand;
     zum Nachweis der Wirksamkeit wird er vor der XAML-Änderung
     einmal rot geprüft).
   - Beschreibung: Neuer Test in
     `VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`, der das
     XAML per `XDocument` parst, das Element `OverlayChrome` findet
     und `VerticalOptions="Start"` sowie das `InputTransparent`-
     Muster (Kopfband transparent, `PipButton` ausgenommen) pinnt.
     Zusätzlich pinnen, dass `PipButton` weiter `Clicked="OnPipClicked"`
     trägt.

3. **Build + volle Testsuite**
   - Voraussetzungen: Schritte 1–2.
   - Beschreibung: `dotnet build VideoPlayer.App.sln` in Debug und
     Release fehlerfrei; `dotnet test VideoWebPlayer.Maui.Tests`
     vollständig grün.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` | `PlayerContractTests` | `OverlayChrome` trägt `VerticalOptions="Start"` (nicht `Fill`), das Kopfband-`Border` ist `InputTransparent="True"` und `PipButton` ist davon ausgenommen (`InputTransparent="False"`); `Clicked="OnPipClicked"` bleibt verdrahtet. Negativprobe: Test schlägt auf dem Ausgangsstand fehl. |

### Betroffene bestehende Tests

Keine.

### E2E-Tests (primärer Funktionsnachweis)

Die Anforderung betrifft einen Benutzerfluss (Bedienung der
Transportsteuerung). Im Repository existiert jedoch **keine**
UI-Automatisierungs-/E2E-Infrastruktur für die MAUI-App — diese
Einschränkung ist projektweit dokumentiert (u. a. `PlayerContractTests`
Klassenkommentar: „Echte UI-End-to-End-Automatisierung ist im
Repository derzeit nicht vorhanden"). Der native Transport des
`MediaElement` ist zudem Plattform-UI des Betriebssystems und im
Quelltext nicht adressierbar.

Ersatzweise (und als projektübliches Vorgehen) wird der Funktionsnachweis
über den Contract-Test erbracht, der die verursachende Struktur pinnt:
Das Overlay belegt nicht mehr die ganze Videofläche und ist außerhalb
seines Schalters eingabedurchlässig. Die visuelle Endabnahme erfolgt
auf dem Gerät (Windows-Vollansicht), wie in den Vorgängerschritten.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Steuerelemente in Vollansicht bedienbar | `PlayerContractTests.PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` (Contract-Pin statt E2E — keine E2E-Infrastruktur vorhanden) | Play/Pause und Seek in Vollansicht sofort bedienbar; App-Overlay funktionsfähig | E2E nicht möglich (kein UI-Test-Stack); Contract-Test pinnt die Blockade-Ursache strukturell. |

Bestehende E2E-Tests angepasst: Keine (nicht vorhanden).

## Offene Punkte

Keine.
