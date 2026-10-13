# Übersetzte Anforderung: Sucheingabe der Quellen-Übersichtsseite übersteht Modell-Aktualisierungen

Quelle: Kundenanforderung, Entwicklungsschritt 2 des Projektplans zu Issue #49
(„Suche funktioniert nicht — Eingabe wird bei jedem Tastenschlag geleert").
`docs/features.md` existiert nicht — der optionale Projektkontext-Schritt
entfällt.

## Fachliche Zusammenfassung

Auf der Quellen-Übersichtsseite (`SourceOverviewPage`, `VideoPlayer.Maui`)
wird der Inhalt des Suchfelds `MediaSearchBar` bei jeder Modell-Aktualisierung
(`ModelChanged` → `UpdateUi`) mit dem zuletzt abgeschickten
`SourceBrowserState.SearchText` überschrieben. Da `SearchText` sich erst beim
Abschicken ändert (`SourceSearchSubmittedMessage`), wird eine vom Anwender
eingetippte, noch nicht abgeschickte Eingabe durch den nächsten beliebigen
Modell-Tick (Synchronisation, Fortschritt, Netzwerkereignisse, Ladezustände)
zerstört — die Suche ist praktisch nicht auslösbar.

Soll-Verhalten:

- Die eingegebene, noch nicht abgeschickte Eingabe bleibt über beliebige
  Modell-Aktualisierungen hinweg erhalten und wird nicht durch den
  Modell-Suchtext überschrieben.
- Das Abschicken über die Such-/Return-Taste löst die Suche mit dem
  eingegebenen Text aus und lädt die Trefferliste ab der ersten Seite
  (bestehender `ReloadBrowserItems`-Pfad).
- Der zuletzt abgeschickte Suchtext darf weiterhin im Feld sichtbar bleiben
  (z. B. bei Rückkehr auf die Seite), damit der aktive Filter erkennbar ist.
- Kein Live-Suchen pro Tastenschlag — das Suchmuster (Explizites Abschicken)
  bleibt fachlich unverändert.

## Betroffene Klassen und Komponenten

- UI: `SourceOverviewPage` (`VideoPlayer.Maui/Pages/SourceOverviewPage.xaml`
  und `.xaml.cs`) — Feldspiegelung in `UpdateUi`, ggf. `AutomationId` des
  Suchfelds für E2E-Erreichbarkeit.
- Zustand: `SourceBrowserState` (`VideoPlayer.Mvu/SourceBrowserState.cs`) —
  `SearchText` bleibt der abgeschickte Filter; ein zusätzlicher
  Entwurfs-Speicher ist nur nötig, wenn der gewählte Ansatz ihn erfordert.
- Messages: `SourceSearchSubmittedMessage` (bestehend, unverändert); kein
  Live-Such-Message-Fluss gefordert.
- DevApi-Sichtbarkeit: `DevApiModelSnapshot`
  (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`) ggf. um den abgeschickten
  `SearchText` erweitern, damit E2E-Tests den aktiven Filter ablesen können;
  Gegenstück `DevApiModels.cs` in `VideoPlayer.E2E.Tests`.
- Tests: `VideoWebPlayer.Maui.Tests` (Contract-Tests der Seitenverdrahtung
  quelltextseitig, Snapshot-Tests) und `VideoPlayer.E2E.Tests`
  (`SourceBrowsingTests`-Muster mit `MockVideoServer`).

## Implementierungsansatz

- `UpdateUi` darf `MediaSearchBar.Text` nicht mehr bei jeder Ungleichheit
  überschreiben, sondern nur noch, wenn sich der abgeschickte
  `browser.SearchText` tatsächlich geändert hat (Abschicken, Quellen-Wechsel,
  Zustands-Reset) — z. B. über einen seitenlokalen Merker des zuletzt
  gespiegelten Werts. Unbestätigte Eingaben bleiben damit über beliebige
  Ticks erhalten; bei Rückkehr auf die Seite (neue Seiten-Instanz) wird der
  aktive Filter wieder angezeigt.
- `OnSearchButtonPressed` und `UpdateSourceSearchSubmitted` bleiben fachlich
  unverändert (Trimmen, Guard bei unverändertem Text, Reload ab Seite 0).
- Keine neue Konfiguration, keine Datenbankmigration.

## Konfiguration

Keine — das Verhalten ist fest verdrahtet.

## Offene Fragen

- Keine fachlich offenen Fragen. Technische Alternativen (seitenlokaler
  Merker vs. Entwurfstext im `SourceBrowserState` à la
  `PlaylistFormState.Name`) werden in der Umsetzungsplanung abgewogen; die
  Anforderung ist mit beiden erfüllbar.
