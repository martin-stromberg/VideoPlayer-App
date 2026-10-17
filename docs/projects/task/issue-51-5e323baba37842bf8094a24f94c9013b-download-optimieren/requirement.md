# Anforderung: Download optimieren (Issue #51)

Quelle: `issue.md` im Repository-Wurzelverzeichnis (GitHub-Issue #51, Plattform: iOS).

## Ziel

Video-Downloads der VideoPlayer-App sollen auf iOS zuverlässig und transparent
funktionieren: Downloads laufen auch bei Hintergrundausführung der App weiter bzw. werden
abgeschlossen, verwaiste bzw. unvollständige Download-Daten werden erkannt und bereinigt,
und der Anwender erhält Kontrolle und Transparenz über den tatsächlich belegten Speicher.

## Problemkontext (belegt aus der Anforderung)

- Downloads brechen ab, sobald die App auch nur kurz in den Hintergrund wechselt; sie
  müssen daher bei geöffneter, aktiver App laufen — bei großen Videodateien problematisch.
- Verwaiste Downloads: iOS-Einstellungen zeigen 14,24 GB „Dokumente & Daten" für die App,
  obwohl die Downloadliste nur einen Eintrag enthält (Serverdatei ca. 400 MB). Die
  Downloadliste meldet gleichzeitig 13,1 GB von 50 GB belegt.
- Der Anwender hat aktuell keine ausreichende Kontrolle und Transparenz über die
  heruntergeladenen Daten.

## Fachliche Anforderungen

1. **Hintergrund-Downloads (iOS):** Video-Downloads laufen auch bei
   Hintergrundausführung der App zuverlässig weiter bzw. werden abgeschlossen (z. B. über
   iOS Background Execution / Background URL Sessions).
2. **Bereinigung verwaister Daten:** Verwaiste bzw. unvollständige Download-Daten werden
   erkannt und bereinigt, damit der tatsächlich belegte Speicher mit der Downloadliste
   übereinstimmt.
3. **Kontrolle und Transparenz:** Der Anwender erhält mehr Kontrolle über
   heruntergeladene Daten, insbesondere:
   - Anzeige des tatsächlich belegten Speichers (pro Download und gesamt),
   - Möglichkeit, einzelne oder alle Downloads gezielt zu löschen,
   - Umgang mit / Bereinigung von verwaisten Dateien.

## Betroffene Bereiche (fachlich)

- Download-Ausführung und -Warteschlange (plattformneutral, aktuell `VideoPlayer.Mvu`,
  aktive App ist `VideoPlayer.Maui` — Annahme auf Basis der README-Projektstruktur).
- iOS-Plattformschicht von `VideoPlayer.Maui` (Hintergrund-Ausführung).
- Download-Manifest und Video-Ablageort (`VideoPlayer.Storage`), inkl. Abgleich
  Manifest ↔ Dateisystem.
- Downloadliste/-seite in der App (Anzeige und Aktionen).
- Einstellungen (Kontingent-Anzeige, ggf. Bereinigungsoptionen).
- Tests (Unit-/Verhaltenstests in `VideoWebPlayer.Maui.Tests`, ggf. E2E in
  `VideoPlayer.E2E.Tests` für Windows-UI-Flüsse).

## Abläufe

- Download starten → App in Hintergrund → Download läuft weiter und wird abgeschlossen
  bzw. korrekt fortgesetzt, statt abzubrechen.
- App-Start bzw. Downloadliste-Aufruf → verwaiste/unvollständige Dateien werden erkannt
  und bereinigt oder dem Anwender zur Bereinigung angeboten.
- Anzeige der real belegten Speichergröße je Download und insgesamt, konsistent zum
  Dateisystem.

## Konfiguration

- Bestehende Einstellungen (Speicherplatz-Kontingent, „Downloads nur im WLAN",
  automatische Downloads) bleiben wirksam; ob neue Schalter (z. B. automatische
  Bereinigung vs. manuell) nötig sind, ist in der Planung zu entscheiden.

## Akzeptanzkriterien

- Ein gestarteter Video-Download wird auf iOS auch dann abgeschlossen, wenn die App
  in den Hintergrund wechselt (soweit es iOS-Beschränkungen zulassen; ggf. über die
  vom System angebotene Hintergrund-Übertragung).
- Nach Bereinigung stimmen angezeigte Belegung und tatsächlich belegter Speicher
  überein; verwaiste Dateien bleiben nicht unbegrenzt liegen.
- Der Anwender kann einzelne und alle Downloads gezielt löschen und sieht den belegten
  Speicher pro Download und gesamt.

## Explizite Nicht-Ziele

- Keine Änderungen am Altbestand `VideoWebPlayer.Maui` (wird schrittweise abgelöst),
  sofern nicht anders entschieden.
- Kein Ausbau des Server-Protokolls (Range-Requests o. ä.) — nicht Teil der Anforderung.
- Keine neuen Download-Quellen/-Funktionen (Playlists, Auto-Downloads bleiben wie
  bestehend).
- Plattformen außer iOS: Das Problem ist iOS-spezifisch; Verbesserungen dürfen
  plattformneutral ausfallen, müssen aber nicht für andere Plattformen nachgewiesen
  werden.

## Offene Fragen

- Soll die Bereinigung verwaister Dateien automatisch (z. B. beim App-Start) erfolgen
  oder dem Anwender als Aktion angeboten werden? (Anforderung nennt beide Varianten:
  „erkannt und bereinigt" sowie „Umgang mit/Bereinigung von verwaisten Dateien" als
  Anwenderkontrolle.)
- Gelten die 50 GB in der Downloadliste als Kontingent aus den Einstellungen oder als
  geräteseitige Grenze? (Beobachteter Wert: 13,1 GB von 50 GB belegt.)

## Annahmen (keine belegten Vorgaben)

- Betroffene App ist `VideoPlayer.Maui` (aktive neue App laut README); der Altbestand
  `VideoWebPlayer.Maui` ist nicht gemeint.
- Die Reproduktionswerte (14,24 GB/13,1 GB/50 GB) stammen aus einer früheren
  App-Version und dienen als Fehlerbild, nicht als zu erreichende Zielwerte.
