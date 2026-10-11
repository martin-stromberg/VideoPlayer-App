# Erscheinungsbild (modernes minimalistisches Design)

Die App (`VideoPlayer.Maui`) ist durchgehend dunkel gestaltet — ein helles
Farbschema gibt es nicht. Das Erscheinungsbild folgt einem modernen,
minimalistischen System: ruhige Schwarztöne mit abgestuften Flächen,
eine rote Primärfarbe als Akzent, großzügige Abrundungen, weiche
Schatten und die moderne, lizenzfreie Hausschrift **Inter**.

## Aus Anwendersicht

- **Typografie:** Die gesamte Oberfläche nutzt Inter (SIL Open Font
  License) — klare Lesbarkeit in allen Größen, mit abgestuften
  Schriftschnitten für Titel (halbfett), Fließtext und Meta-Angaben.
  Versal-Kicker mit Laufweite kennzeichnen Abschnitte; Symbole stammen
  aus den Material Icons (gerundete Variante).
- **Hintergrund:** Seiten erscheinen auf einem sehr dunklen, fast
  schwarzen Grund; das Seitenmenü und die Videofläche nutzen den noch
  tieferen Schwarzton.
- **Listen:** Medieneinträge erscheinen als Karten mit Haarriss-Rand,
  großem Radius und weichem Schatten. Poster sind sauber in die Karten
  geschnitten; Titel und Meta-Angaben stehen in klarer Hierarchie darunter
  bzw. daneben. Zustände (z. B. Download-Status, Genre-Auswahl) werden
  als pill-förmige Chips mit getönter Akzentfläche gezeigt.
- **Schaltflächen:** Primäre Aktionen sind kräftig rot mit kräftigem
  Radius; Nebenaktionen nutzen die ruhige Sekundärfläche mit feinem
  Rand (`SecondaryButton`), drittrangige Aktionen sind rahmenlose
  Text-Schalter (`GhostButton`). Runde Symbol-Schaltflächen über Bildern
  (Favorit, Download, Abspielen) sind kreisrund mit halbdunkler Fläche
  und Material-Icons-Glyphen.
- **Eingaben:** Eingabefelder liegen in gefüllten, abgerundeten Rahmen;
  Einstellungen sind als Gruppenkarten mit Trennlinien gegliedert.
- **Hinweise und Fehler:** Hinweis-Banner (z. B. Offline-Modus) sind
  abgesetzte dunkle Flächen mit betontem Rahmen; Fehlermeldungen
  erscheinen in der Fehlerfarbe.
- **Menü:** Die Flyout-Einträge führen Icons aus der Material-Icons-
  Schrift; die dynamische Quellengruppe wird mit einem Versal-Kicker
  als Sektionstitel abgesetzt.

## Für Entwickler

### Farb-Tokens

Alle Farben sind als zentrale Design-Tokens in
`VideoPlayer.Maui/Resources/Styles/Colors.xaml` definiert (die `Vp*`-
Schlüssel gehen auf die CSS-Variablen der Webanwendung zurück):

| Token | Wert | Verwendung |
|-------|------|------------|
| `VpBg` | `#131313` | Seitenhintergrund |
| `VpBgDeep` | `#0e0e0e` | Seitenmenü, Tab-Leiste, Videofläche |
| `VpSurfaceLow`/`VpSurface`/`VpSurfaceHigh`/`VpSurfaceBright` | `#1c1b1b`/`#201f1f`/`#2a2a2a`/`#393939` | abgestufte Flächen (Karten, Banner, Eingaberahmen, deaktivierte Zustände) |
| `VpText`/`VpTextMuted`/`VpTextSoft` | `#e5e2e1`/`#bda9a6`/`#e9bcb6` | Text normal / gedämpft / weich |
| `VpBorder`/`VpBorderStrong` | `#1AFFFFFF`/`#6BFFB4AA` | Rahmen normal / betont |
| `VpPrimary`/`VpPrimaryHover`/`VpPrimarySoft` | `#e50914`/`#c0000c`/`#ffb4aa` | Primär-Aktion, Hover, Akzent-Text |
| `VpPrimaryTint` | `#2EE50914` | getönte Akzentfläche (ausgewählte Chips, Zustands-Badges) |
| `VpSecondary`/`VpSecondarySoft` | `#00a8e8`/`#83cfff` | Sekundär-Akzent (z. B. Erfolgshinweise) |
| `VpDanger` | `#ffb4ab` | Fehlermeldungen |
| `VpOverlay`/`VpShadow`/`VpShadowBrush` | `#80000000`/`#000000` | transluzente Abdunklung und Karten-Schatten |
| `VpBadgeBg`/`VpBadgeBorder` | `#AD000000`/`#47FFFFFF` | Angesehen-/Platzhalter-Badges über Bildern |
| `VpHero*` | transluzent | Hero-Gradienten der Startseite und des Detail-Kopfbereichs |
| `VpScrimStart`/`VpScrimEnd` | `#B3000000`/`#00000000` | Scrim-Verlauf des Player-Overlays |

### Typografie und Schriften

`VideoPlayer.Maui/Resources/Fonts/` enthält die statischen Inter-
Schnitte (`Inter-Regular`, `-Italic`, `-Medium`, `-SemiBold`, `-Bold`;
OFL-Lizenztext in `VideoPlayer.Maui/licenses/Inter-LICENSE.txt`) und
`MaterialIconsRound-Regular.otf` (Apache-2.0, Lizenztext in
`licenses/MaterialIcons-LICENSE.txt`). `MauiProgram` registriert die
Aliasse `Inter`, `InterItalic`, `InterMedium`, `InterSemiBold`,
`InterBold` und `MaterialIcons`. `FontAttributes="Bold"`/`"Italic"` auf
der Familie `Inter` löst über die RIBBI-Schnitte auf; Zwischenstufen
werden explizit über `FontFamily="InterSemiBold"`/`"InterMedium"`
gesetzt. Icon-Glyphen nutzen `FontFamily="MaterialIcons"` mit der
Codepoint-Entität (z. B. `&#xe2c4;`).

### Stile

`Resources/Styles/Styles.xaml` stellt alle Control-Styles auf die
Tokens um: `Page`/`Shell`/`NavigationPage`/`TabbedPage` nutzen
`VpBg`/`VpBgDeep`. Zentrale benannte Stile:

- **Typografie:** `PageTitle`, `SectionTitle`, `CardTitle`, `Caption`,
  `Kicker`, `ErrorText`.
- **Flächen:** `Card` (Radius 14, Haarriss-Rand, weicher Schatten),
  `Banner` (Radius 12, betonter Rand), `InputFrame` (Radius 10,
  gefüllt), `Chip` (999) und `KickerChip` (999 über Bildflächen).
- **Schaltflächen:** impliziter `Button`-Style (`CornerRadius` 10,
  `VpPrimary`, `PointerOver` → `VpPrimaryHover`), `SecondaryButton`,
  `GhostButton` und der pill-förmige `IconButton` (999).

In `App.xaml.cs` ist `UserAppTheme = AppTheme.Dark` gesetzt, damit kein
System-Helldesign Restwerte aktiviert.

**Regel:** In Seiten und Steuerelementen dürfen keine hartcodierten
Hexfarben oder benannten Farbliterale mehr verwendet werden — jede Farbe
kommt per `{StaticResource Vp*}` aus den Tokens. Die Contract-Tests
`DesignTokensContractTests` (`VideoWebPlayer.Maui.Tests`) pinnen die
Token-Werte, die Radien (Buttons 10, Karten 14, Poster 12,
Chips/Badges/Icon-Buttons 999) und verbieten verstreute Farbwerte.
