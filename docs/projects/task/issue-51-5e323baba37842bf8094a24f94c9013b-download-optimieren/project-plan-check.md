# Projektplan-Gegenprüfung

## Ergebnis

**Status:** Projektplan vollständig

## Abgleich Anforderung ↔ Entwicklungsschritte

| Anforderungspunkt | Abgedeckt durch Schritt(e) | Status |
|--------------------|-----------------------------|--------|
| Downloads laufen auf iOS im Hintergrund weiter bzw. werden abgeschlossen (Background Execution / Background URL Sessions) | 3 | Abgedeckt |
| Verwaiste/unvollständige Download-Daten werden erkannt | 1 | Abgedeckt |
| Verwaiste Daten werden bereinigt; belegter Speicher stimmt mit Downloadliste überein | 1 (Erkennung + automatisches Entfernen überholter Teildateien), 2 (anwendergesteuerte Bereinigung erkannter Restdaten, konsistente Belegungsanzeige) | Abgedeckt |
| Anzeige des tatsächlich belegten Speichers pro Download und gesamt | 2 | Abgedeckt |
| Einzelne Downloads gezielt löschen | Bestehende Funktion; in Schritt-2-Beschreibung als weiterhin wirksam genannt | Abgedeckt |
| Alle Downloads gezielt löschen | 2 („Alle Downloads löschen" mit Rückfrage) | Abgedeckt |
| Umgang mit/Bereinigung verwaister Dateien als Anwenderkontrolle | 2 (Bereinigungs-Aktion mit Größenangabe) | Abgedeckt |
| Bestehende Einstellungen bleiben wirksam (Kontingent, WLAN, Parallelität) | 3 (für Hintergrund-Transfers explizit), 1–2 (unveränderte Regeln) | Abgedeckt |
| Ablauf: Abgleich bei App-Start/Seitenaufruf | 1 | Abgedeckt |
| Ablauf: Zustandsabgleich nach Hintergrund-Transfer bei Rückkehr/Neustart | 3 | Abgedeckt |
| Nicht-Ziel: kein Ausbau des Server-Protokolls (keine Range-Requests) | Kein Schritt enthält Server-Änderungen | Abgedeckt |
| Nicht-Ziel: kein Altbestand `VideoWebPlayer.Maui` | Schritte adressieren ausschließlich die aktive App | Abgedeckt |
| Nicht-Ziel: keine anderen Plattformen nachweispflichtig | Schritt 3 begrenzt auf iOS, übrige Plattformen unverändert | Abgedeckt |
| Jede Schrittbeschreibung taugt unverändert als Lifecycle-Eingabe (eigenständig lesbar, mit Akzeptanzkriterien) | 1–3 je mit Beschreibung + Akzeptanzkriterien | Abgedeckt |

## Abhängigkeitsprüfung

| Prüfpunkt | Befund |
|-----------|--------|
| Zyklen in Abhängigkeiten | Keine gefunden |
| Reihenfolge konsistent zu inhaltlichen Abhängigkeiten | Ja — Schritt 2 (Anzeige/Bereinigung erkannter Restdaten) und Schritt 3 (Transfer-Stand-Abgleich nach Hintergrund-Vervollständigung) bauen auf der Abgleichslogik aus Schritt 1 auf; beide verweisen auf Schritt 1 |

## Fehlende oder unvollständige Punkte

—

## Hinweise

- Schritt 3 ist iOS-spezifisch und in dieser Windows-Entwicklungsumgebung nicht
  auf Gerät verifizierbar; die Lifecycle-Durchläufe sollten die
  plattformneutrale Auftrags-/Abgleichslogik durch Tests absichern und den
  iOS-Plattformcode separat reviewen.
- Die Stakeholder-Entscheidungen zu den drei ehemals offenen Punkten sind in
  die Schrittbeschreibungen eingearbeitet; der Abschnitt „Offene Punkte" ist
  leer.
