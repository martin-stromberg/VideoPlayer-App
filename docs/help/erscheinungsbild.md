# Erscheinungsbild (Design-Angleichung an die Webanwendung)

Die neue App (`VideoPlayer.Maui`) übernimmt das dunkle Erscheinungsbild der
Webanwendung: tiefschwarze Hintergründe, abgestufte dunkle Flächen, die
rote Primärfarbe der Webanwendung und dieselben Abrundungen. Die App ist
durchgehend dunkel gestaltet — ein helles Farbschema gibt es wie im Web
nicht.

## Aus Anwendersicht

- **Hintergrund:** Seiten erscheinen auf einem sehr dunklen, fast
  schwarzen Grund; das Seitenmenü und die Videofläche nutzen den noch
  tieferen Schwarzton der Webanwendung.
- **Schaltflächen:** Normale Schaltflächen sind rot mit leicht
  abgerundeten Ecken (wie die Web-Buttons) und dunkeln bei
  Mauszeiger-Kontakt ab. Runde Symbol-Schaltflächen (Favorit, Download,
  Abspielen in der Episodenliste) sind kreisrund mit halbdunkler Fläche.
- **Karten und Chips:** Inhaltskarten (Medien, Downloads, Profil) haben
  die abgerundeten Ecken der Web-Karten; Genre-Chips und Hinweis-Badges
  sind pill-förmig.
- **Hinweise und Fehler:** Hinweis-Banner (z. B. Offline-Modus) sind
  abgesetzte dunkle Flächen mit betontem Rahmen; Fehlermeldungen
  erscheinen in der Fehlerfarbe der Webanwendung.
- **Bedienung:** Unverändert — dieser Schritt ändert ausschließlich das
  Aussehen, keine Abläufe.

## Für Entwickler

Alle Farben sind als zentrale Design-Tokens in
`VideoPlayer.Maui/Resources/Styles/Colors.xaml` definiert (Schlüssel
spiegeln die CSS-Variablen der Webanwendung):

| Token | Wert | Verwendung |
|-------|------|------------|
| `VpBg` | `#131313` | Seitenhintergrund |
| `VpBgDeep` | `#0e0e0e` | Seitenmenü, Tab-Leiste, Videofläche |
| `VpSurfaceLow`/`VpSurface`/`VpSurfaceHigh`/`VpSurfaceBright` | `#1c1b1b`/`#201f1f`/`#2a2a2a`/`#393939` | abgestufte Flächen (Karten, Banner, deaktivierte Zustände) |
| `VpText`/`VpTextMuted`/`VpTextSoft` | `#e5e2e1`/`#bda9a6`/`#e9bcb6` | Text normal / gedämpft / weich |
| `VpBorder`/`VpBorderStrong` | `#1AFFFFFF`/`#6BFFB4AA` | Rahmen (rgba-Werte des Webs als ARGB) |
| `VpPrimary`/`VpPrimaryHover`/`VpPrimarySoft` | `#e50914`/`#c0000c`/`#ffb4aa` | Primär-Aktion, Hover, Akzent-Text |
| `VpSecondary`/`VpSecondarySoft` | `#00a8e8`/`#83cfff` | Sekundär-Akzent (z. B. Erfolgshinweise) |
| `VpDanger` | `#ffb4ab` | Fehlermeldungen |
| `VpOverlay`/`VpShadow`/`VpShadowBrush` | `#80000000`/`#000000` | transluzente Abdunklung und Karten-Schatten (app-eigene Hilfstokens) |

`Resources/Styles/Styles.xaml` stellt alle globalen Control-Styles auf
die Tokens um: `Page`/`Shell`/`NavigationPage`/`TabbedPage` nutzen
`VpBg`/`VpBgDeep`, der globale `Button`-Style hat `CornerRadius` 4 und
einen `PointerOver`-Zustand in `VpPrimaryHover`, und der benannte Style
`IconButton` liefert pill-förmige (`CornerRadius` 999) Symbol-Schalter.
In `App.xaml.cs` ist `UserAppTheme = AppTheme.Dark` gesetzt, damit kein
System-Helldesign Restwerte aktiviert.

**Regel:** In Seiten und Steuerelementen dürfen keine hartcodierten
Hexfarben oder benannten Farbliterale mehr verwendet werden — jede Farbe
kommt per `{StaticResource Vp*}` aus den Tokens. Die Contract-Tests
`DesignTokensContractTests` (`VideoWebPlayer.Maui.Tests`) pinnen die
Token-Werte, die Radien (Buttons 4, Karten 8, Chips/Badges/Icon-Buttons
999) und verbieten verstreute Farbwerte.
