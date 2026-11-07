# Usability-Review: Episodenlisten — Angesehen-Symbol bei gesehenen Episoden

Basisbranch: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`
(namensbasiert, ohne Suffix `-schritt-7-angesehen-symbol`).

## Ergebnis

**Status:** Keine Befunde

## Geprüfte Aspekte

| Aspekt | Befund | Bewertung |
|--------|--------|-----------|
| Wahrnehmbarkeit | Badge erscheint ohne Zutun über dem Poster, sobald `WatchedAt` gesetzt ist — keine Eingabe, kein Vorwissen nötig | Erfüllt |
| Deutbarkeit | Augen-Motiv entspricht exakt dem Web-`WatchedIndicator` (Kontur + Pupille + Glanzpunkt), Tooltip „Gesehen" (`ToolTipProperties.Text`) erklärt das Symbol bei Berührung/Hover | Erfüllt |
| Position/Größe | Oben links über dem Still (Web `.watched-indicator-image-left`, `top/left 0.6rem` ≙ `Margin 6,6`), 26 px auf dem 62 px hohen Episoden-Still proportioniert zum 2rem-Web-Badge | Erfüllt |
| Keine Fehlbeschriftung | `IsVisible="{Binding IsWatched}"` — ungesehene Episoden zeigen bewusst gar kein Symbol statt eines „ungesehen"-Zustands (Deckung mit Web) | Erfüllt |
| Bedienbarkeit nicht beeinträchtigt | `InputTransparent="True"` — das Badge verschluckt keine Taps auf Poster/Episode/Download-Schalter | Erfüllt |
| Lesbarkeit auf Postern | Dunkler transluzenter Grund (`#AD000000` ≙ `rgba(0,0,0,.68)`) + feiner heller Rand — Symbol bleibt auf hellen und dunklen Stills erkennbar | Erfüllt |
| In-Session-Aktualisierung | Nach Ende/Watched-Fenster erscheint das Badge in der geöffneten Detailansicht ohne Seitenreload (`MarkEpisodeWatchedInDetails` → `ItemsSource`-Neuzuweisung) | Erfüllt |
| Offline | Snapshot-Listen tragen denselben Stand (`CachedShowEpisode.WatchedAt`); alte Caches ohne Feld zeigen schlicht kein Badge | Erfüllt |

## Hinweise (keine Blocker)

- Fremdgerätige Watched-Änderungen (`ContinueWatchingUpdated`) aktualisieren
  eine geöffnete Detailseite erst beim nächsten Laden — deckt sich mit dem
  geforderten Umfang (eigene Wiedergabe aktualisiert sofort).
- Die Badge-Größe ist proportioniert zum App-Still gewählt (26 px statt
  exakt 2rem/32 px); Motiv, Position und Farben folgen dem Web-Vorbild —
  dokumentierte Designentscheidung.

## Geprüfte Interaktionen

- „Episode anschauen → Angesehen-Symbol erkennen" → unauffällig
- „Ungesehene Episode → kein Symbol" → unauffällig
- „Nach dem Ansehen in die Episodenliste zurückkehren" → Badge sichtbar ohne Neuladen → unauffällig
- „Auf Poster/Episode tippen (trotz Overlay)" → `InputTransparent` → unauffällig

## Geprüfte Dateien

- `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml`
- `VideoPlayer.Maui/Resources/Styles/Colors.xaml`

## Nächster Schritt

Schritt 9 — Code-Review.
