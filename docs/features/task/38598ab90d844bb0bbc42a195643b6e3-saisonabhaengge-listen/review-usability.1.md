# Usability-Review: Saisonabhängige Listen

**Status:** Keine Befunde

## Grundlage und Unabhängigkeit

Fachliche Grundlage ausschließlich `requirement.md`. `plan.md`, `review.md` und `review-code.md` wurden nicht gelesen. Geprüft wurden die tatsächlichen mobilen UI-/Navigationsänderungen gegenüber `staging` sowie die Webänderungen im isolierten Servercheckout gegenüber `eae60a6003ac0619958331351d21c5d983cb175f`.

Das im Lifecycle genannte Modell `opus-4.8` ist nicht verfügbar; das verfügbare geerbte Modell wurde als Ersatz eingesetzt.

## Befunde

Keine.

## Geprüfte Benutzerperspektive

- Die mobile Startseite verwendet die vorhandene Poster-Karussellgestaltung mit verständlichen serverseitigen Genre-Überschriften und sichtbaren Medientiteln. Die bisherigen Bereiche bleiben erhalten; die saisonalen Listen sind im vertikal scrollbaren Home-Inhalt erreichbar.
- Die Karten verwenden denselben Auswahlhandler wie bestehende Home-Medien. Filme, Sammlungen und Serien werden mit ihrem jeweiligen Medientyp an die vorhandene Detailnavigation übergeben. Ein fehlendes Poster erhält den bestehenden Titel-Fallback.
- Leere, ungültige und abgelaufene Sektionen werden vor dem Rendern ausgefiltert. Ein leeres Ergebnis hinterlässt keine leeren Überschriften oder Karussells.
- Ein Abruffehler entfernt gültige Offlineinhalte nicht. Der absolute Server-Ablaufzeitpunkt wird dagegen auch bei Fehlern, auf geöffneter Startseite und bei Resume/Neustart berücksichtigt. Benutzer müssen dafür keine Saison konfigurieren oder ein Datum berechnen.
- Die Webansicht behält die bisherige horizontale Kartengestaltung, Titel-/Bild-Fallbacks, Erledigt-Markierungen und echte Links zu Film-, Sammlungs- und Seriendetails bei. Sektionen haben beschriftete semantische Bereiche; abgelaufene Bereiche verschwinden ohne Navigation.
- Die sechs vorhandenen mobilen Saison-E2E prüfen sichtbare Karten, exakte Film-/Sammlungsziele, Reihenfolge, leere Antworten, Fehler-/Neustartcache und Ablauf einschließlich Offline-Neustart. Die vier Web-Playwright-Szenarien prüfen sichtbare berechtigte Inhalte, Film-/Sammlungs-/Serien-/Singletonnavigation und Ablauf auf geöffneter Seite.

## Prüfumfang und Grenzen

Dieses Review basiert auf UI-Code, Ereignisverdrahtung, bestehenden Gestaltungskomponenten und den konkreten E2E-Assertions. Es wurden keine neuen Tests gestartet und keine eigene visuelle Laufzeitprüfung oder Bildschirmfoto-Abnahme vorgenommen. Aus den geprüften Änderungen ergibt sich kein konkreter zusätzlicher Bedienungsbefund; die unabhängige Testausführung erfolgt im gesonderten Testschritt.

