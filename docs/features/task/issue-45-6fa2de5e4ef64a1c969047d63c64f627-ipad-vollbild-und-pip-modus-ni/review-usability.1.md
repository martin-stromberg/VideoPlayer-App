# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### PlayerPage.xaml (Wiedergabeseite / PlayerRoot)

- **Erreichbarkeit** — Betroffene Interaktion: „Auf dem iPad die Vollbild- und
  PiP-Schaltflächen der nativen Transportsteuerung antippen". Die Korrektur
  macht das `OverlayChrome`-Band per `InputTransparent="True"` am Elternlayout
  zwar eingabedurchlässig — die Buttons sind dadurch antippbar —, das Band
  zeichnet aber weiterhin **sichtbar über** dem oberen Streifen, in dem iOS die
  native Leiste einblendet: Der Scrim-Verlauf (`VpScrimStart` = `#B3000000`,
  ca. 70 % Schwarz am oberen Rand) dunkelt die weißen nativen Buttons deutlich
  ab, und das rechts im Band liegende `WatchedBadge` („Angesehen") sowie das
  über die volle Breite gezogene Kopfband können die Button-Glyphen direkt
  überlagern. MAUI-Overlay-Elemente liegen als Geschwister-Views oberhalb des
  `AVPlayerViewController` — genau daraus entstand ja das ursprüngliche
  Eingabe-Problem. Für eine Endanwenderin sind die Buttons damit abgedunkelt
  und teilweise verdeckt; die Anforderung verlangt ausdrücklich „überdeckt sie
  nicht" (nicht nur „fängt Eingaben nicht ab"). Die Entdeckbarkeit leidet:
  Wer die Buttons unter der Abdunklung nicht als bedienbar erkennt, tippt sie
  nicht an.

  Empfehlung: Den oberen Randbereich auf iOS auch **visuell** freihalten —
  z. B. `OverlayChrome` unter iOS analog zur `PlaylistNavigationBar` per
  `#if IOS` unter die native Leiste schieben (`Margin` oben), oder die
  Breite/das Band so begrenzen, dass die Ecke mit den nativen Buttons frei
  bleibt (Badge nicht an den rechten Rand legen). Das Band bleibt dabei
  sichtbar und erfüllt weiterhin seine Anzeigefunktion.

### PlayerPage.xaml.cs (Wiedergabeseite, iOS-Block im Konstruktor)

- **Erreichbarkeit** — Betroffene Interaktion: „Zurück"/„Weiter" der
  `PlaylistNavigationBar` sowie erneut die nativen Buttons. Der fixe iOS-
  Versatz `PlaylistNavigationBar.Margin = new Thickness(0, 96, 16, 0)`
  unterstellt eine konkrete Höhe der nativen iOS-Top-Leiste inklusive
  Safe-Area. Ist die Leiste auf einem Gerät/in einer Ausrichtung höher als
  96 px, liegen „Zurück"/„Weiter" teilweise **unter** der nativen Leiste
  (überdeckt/schwer treffbar); ist sie niedriger, bleibt zwar alles bedienbar,
  die Bar rückt aber näher an den Buttons-Bereich heran. Ob der Wert passt,
  ist nur per Geräteprüfung klärbar — die Anforderung selbst führt die genaue
  Blockierposition als offene Frage.

  Empfehlung: Den Versatz bei der manuellen iPad-Verifikation (TestFlight,
  ohnehin geforderter Prüfschritt) gegen die tatsächliche Leistenhöhe prüfen;
  im Zweifel an die Safe-Area koppeln oder großzügiger wählen, damit die Bar
  garantiert unterhalb der nativen Leiste liegt.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Video antippen, um die native Transportsteuerung einzublenden → unauffällig
  (`OverlayChrome` jetzt eingabedurchlässig, `PipButton` unter iOS nicht
  sichtbar, kein Windows-Tap-Workaround auf iOS aktiv)
- Native Vollbild-Schaltfläche antippen → Befund vorhanden (Eingabe jetzt
  durchlässig, aber weiterhin visuell durch Scrim/Badge überdeckt)
- Native PiP-Schaltfläche antippen → Befund vorhanden (gleiche Überdeckung)
- „Zurück"/„Weiter" in der Playlist-Navigation antippen → Befund vorhanden
  (bleibt interaktiv und als eigenes Element außerhalb des eingabedurchlässigen
  Bands, aber der fixe 96-px-iOS-Versatz ist eine unverifizierte Annahme über
  die Leistenhöhe)
- Titel, Playlist-Kontext und „Angesehen"-Badge ablesen → unauffällig (alle
  Anzeigen bleiben sichtbar; `IsVisible`-Logik unverändert)
- Windows: „Mini-Player"-Schalter und Transportsteuerung bedienen →
  unauffällig (unverändert; `PipButton` weiterhin `IsToggleSupported`-gesteuert,
  Tap-Workaround weiterhin `#if WINDOWS`)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `VideoPlayer.Maui/Pages/PlayerPage.xaml`
- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`
- `VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`
