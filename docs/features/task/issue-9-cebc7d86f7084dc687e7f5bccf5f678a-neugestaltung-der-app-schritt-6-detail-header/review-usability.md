# Usability-Review: Detailseiten — Vollbreiten-Header mit Hintergrundgrafik

## Ergebnis

**Status:** Keine kritischen Probleme

## Geprüfte Aspekte

| Aspekt | Befund | Bewertung |
|--------|--------|-----------|
| Vollbreite | Wurzel-`Padding` entfernt; `HeaderSection` füllt die Breite, Unterinhalte behalten 16pt-Seitenrand | Erfüllt |
| Cover/Responsive Höhe | `AspectFill` + `Math.Clamp(Width*0.52, 360, 620)` über `SizeChanged` | Erfüllt (Web `clamp(360px,52vw,620px)`) |
| Lesbarkeit der Kopfzeilen | Seitlicher Gradient (`.98→.76→.18` über `VpBg`) + Bottom-Fade zu `VpBg`; Texte `VpText`/`VpPrimarySoft`/`VpTextSoft` auf dunkler Fläche (`VpSurfaceLow`-Fallback) | Erfüllt; Kontrast analog Web |
| Fallback ohne Bild | `HeaderImage` ohne `Source` bleibt leer; Gradienten über `VpSurfaceLow` tragen den Bereich; Titel/Meta immer gesetzt (Fallbacks im View) | Erfüllt; kein Layout-Kollaps (festes `HeightRequest`) |
| Offline | Kein Abruf ohne `Connected`; Snapshot zeigt Titel/Meta; Favoriten-Schalter deaktiviert statt ausgeblendet (bisheriges Muster); Platzhalter-Episoden bleiben | Erfüllt |
| Staffelwechsel | Erste Episode führt den Kopfbereich (Web `SelectSeason`); altes Still wird verworfen statt kurz falsch angezeigt | Erfüllt |
| Kollisionsgefahr | Inhalt unten links mit `Padding`-Reserve rechts (140pt) gegenüber den Aktions-Schaltern unten rechts; max. 820pt Breite | Erfüllt; Schmalbild-Randfälle durch Gradienten lesbar |
| Trennung zum Inhalt | `VpBorder`-Trennlinie + Bottom-Fade; darunter gepaddete Sektionen | Erfüllt |
| Favoriten-/Aktions-Schalter | Positionen wie Web (`favorite-btn` oben rechts, `play-button` unten rechts); vorhandene Sichtbarkeitslogik (Offline/IsPlayable) unverändert | Erfüllt |
| Plot-Länge | `MaxLines="4"` + `TailTruncation` — verhindert übergroße Header bei langen Handlungstexten | Erfüllt (Web nutzt `-webkit-line-clamp`) |

## Hinweise (keine Blocker)

- Bei sehr schmalen Fenstern kann die Metazeile in die Aktions-Schalter
  laufen — die 140pt-Reserve rechts deckt die üblichen Breiten ab; das
  Web-Layout hat denselben absoluten Overlay-Ansatz.
- Der Favoriten-Stern sitzt jetzt über dem Bild — der
  `IconButton`-Stil (dunkle transluzente Fläche) bleibt wie im Web
  lesbar.
- „Abspielen"/„Herunterladen" erscheinen weiterhin nur bei direkt
  abspielbaren Medientypen — bewusst unverändert.

## Nächster Schritt

Schritt 9 — Code-Review.
