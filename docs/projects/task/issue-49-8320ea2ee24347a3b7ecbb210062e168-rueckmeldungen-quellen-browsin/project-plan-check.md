# Projektplan-Gegenprüfung

## Ergebnis

**Status:** Projektplan vollständig

## Abgleich Anforderung ↔ Entwicklungsschritte

| Anforderungspunkt | Abgedeckt durch Schritt(e) | Status |
|--------------------|-----------------------------|--------|
| FA-1: Genre-Filter in der Serien-Übersicht — Sichtbarkeit der Leiste mit „Alle"-Eintrag und Server-Genres | Schritt 1 (Soll: Leiste sichtbar, enthält „Alle" + Genres der Quelle) | Abgedeckt |
| FA-1: Genre-Auswahl filtert Trefferliste (Neu-Laden ab Seite 0); „Alle" hebt Filter auf | Schritt 1 | Abgedeckt |
| FA-1: Ursache diagnostizieren und dokumentieren (leere Serverantwort / Ladefehler / fehlende Leiste); Korrektur behebt die festgestellte Ursache | Schritt 1 (Diagnose als Teil des Schritts, Dokumentation, bei leerer Serverliste keine funktionslose Zeile) | Abgedeckt |
| FA-1: Bestehende Chip-Leiste bleibt Soll-Form, keine neue Bedienform | Schritt 1 + Vorgehensentscheidung „Bedienkonzept" | Abgedeckt |
| FA-2: Unbestätigte Sucheingabe bleibt über beliebige Modell-Aktualisierungen (Sync-/Fortschritts-/Netzwerkereignisse) erhalten | Schritt 2 | Abgedeckt |
| FA-2: Abschicken über Such-/Return-Taste löst Suche aus und lädt Trefferliste ab Seite 0 | Schritt 2 | Abgedeckt |
| FA-2: Abgeschickter Suchtext bleibt im Feld sichtbar (z. B. bei Rückkehr zur Seite) | Schritt 2 | Abgedeckt |
| FA-2: Keine Live-Suche pro Tastenschlag (bestehendes Suchmuster unverändert) | Schritt 2 + Vorgehensentscheidung „Bedienkonzept" | Abgedeckt |
| FA-3: Scrollposition beim automatischen Nachladen der Folgeseite bleibt erhalten; Einträge erscheinen angehängt | Schritt 3 | Abgedeckt |
| FA-3: Verhalten gilt für ungefilterte Liste sowie Such-/Genre-Treffer (gleicher Pfad) | Schritt 3 | Abgedeckt |
| FA-3: Erstladen, Genre-Wechsel und neue Suche zeigen Liste weiterhin von Anfang an | Schritt 3 | Abgedeckt |
| FA-3: Technische Umsetzungsform frei (Ergebnis zählt) | Schritt 3 | Abgedeckt |
| FA-4: Tipp auf Weiterschauen-Eintrag (Film/Episode) öffnet Titel-Detailansicht mit Wiedergabe; „Player schließen" endet auf der Detailansicht statt der Startliste (Parität zur Serveranwendung) | Schritt 4 | Abgedeckt |
| FA-4: Titelinformationen auf der Detailansicht ohne Umweg über die Quelle einsehbar | Schritt 4 | Abgedeckt |
| FA-4: Serienkontext-Funktion (Fortsetzungsliste der Folge-Episoden) bleibt erhalten | Schritt 4 | Abgedeckt |
| FA-4: Nicht direkt abspielbare Einträge (Serie, Staffel, Kollektion) behalten Detail-Navigation | Schritt 4 | Abgedeckt |
| FA-4: Playlist-gebundene Einträge, Offline-Einträge, Einträge ohne nutzbaren Kontext — Verhalten festgelegt | Schritt 4 (Playlist-Direktstart bleibt; Offline mit Kontext-Ids → Detailansicht mit Wiedergabe; ohne Kontext → bisheriges Verhalten) | Abgedeckt |
| FA-5: Verfrühtes Ende-Ereignis löst weder Auto-Vorlauf/Folgevideo-Start noch Angesehen-Markierung/End-Fortschrittsmeldung aus | Schritt 5 | Abgedeckt |
| FA-5: Plausibilisierung gegen Wiedergabestand (Position vs. Gesamtdauer oder vergleichbarer Ansatz); Regel/Schwelle nicht vorgegeben | Schritt 5 | Abgedeckt |
| FA-5: Tatsächliches Ende lässt bestehenden Abschluss-Ablauf unverändert weiterlaufen (Angesehen, Fortschrittsmeldung, Auto-Fortsetzung bei aktiviertem Schalter) | Schritt 5 | Abgedeckt |
| FA-5: Prüfung einheitlich für Serien-/Einzelkontext und Playlist-Kontext | Schritt 5 | Abgedeckt |
| Betroffener Bereich: Quellen-Browsing / `SourceOverviewPage` (Genre-Leiste, Suchfeld, Trefferliste) | Schritte 1–3 (Betroffene Bereiche: Quellen-Browsing, UI-Synchronisation der Übersichtsseite) | Abgedeckt |
| Betroffener Bereich: Startseite / Weiterschauen-Liste und Titel-Detailansicht (`HomePage`, `MediaDetailsPage`, Navigations- und Wiedergabe-Einstieg) | Schritt 4 (Betroffene Bereiche: Startseite, Navigation, Titel-Detailansicht, Wiedergabe-Einstieg, Serienkontext) | Abgedeckt |
| Betroffener Bereich: Wiedergabe / `PlayerPage`-Ende-Erkennung inkl. Fortschritts-/Angesehen-Meldung und Auto-Fortsetzung | Schritt 5 (Betroffene Bereiche: Wiedergabe, Ende-Erkennung, Fortschritts-/Angesehen-Meldung, Auto-Fortsetzung) | Abgedeckt |
| Betroffener Bereich: MVU-Kern (`VideoPlayer.Mvu`), Effekt-Handler (`ShellAppEffectHandler`) und API-Client (nur Diagnose FA-1, keine Client-Änderung) | Schritte 1–5 (fachliche Ladepfad-/Ablaufbeschreibungen), Vorgehensentscheidung „Server und API", Schritt 1 (Diagnose des Genre-Ladepfads, serverseitige Ursache nur dokumentieren) | Abgedeckt |
| Ablauf FA-1 (Ist/Soll): Genre-Ladepfad inkl. „Alle"-Fallback; Soll: Leiste sichtbar und Auswahl funktional, Fehlerpfad nach Diagnose beheben | Schritt 1 | Abgedeckt |
| Ablauf FA-2 (Ist/Soll): Modell-Tick überschreibt unbestätigte Eingabe; Soll: Eingabe bleibt, Submit übernimmt Text und lädt ab Seite 0 | Schritt 2 | Abgedeckt |
| Ablauf FA-3 (Ist/Soll): Listenendenerkennung → Nachladen → Listen-Austausch → Scrollsprung; Soll: gleicher Lade-Flow, Position bleibt | Schritt 3 | Abgedeckt |
| Ablauf FA-4 (Ist/Soll): Tipp → Direktwiedergabe → Schließen zur Startliste; Soll: Tipp → Detailansicht mit Wiedergabe → Schließen zur Detailansicht | Schritt 4 | Abgedeckt |
| Ablauf FA-5 (Ist/Soll): Ende-Ereignis → ungeprüfter Abschluss-Flow; Soll: erst Plausibilisierung, dann Abschluss-Flow; spurious Ereignisse lösen nichts aus | Schritt 5 | Abgedeckt |
| Konfiguration: kein neues konfigurierbares Verhalten; bestehende Einstellungen (Auto-Fortsetzung, Restlaufzeit-/Angesehen-Fenster) bleiben unverändert wirksam | Vorgehensentscheidung „Konfiguration" + Schritt 5 (Schwelle als fester Wert, Auto-Fortsetzung „bei aktiviertem Schalter") | Abgedeckt |
| Nicht-Ziel: keine Server-/API-Vertragsänderungen; serverseitige Ursache nur dokumentieren | Vorgehensentscheidung „Server und API" + Schritt 1 | Abgedeckt |
| Nicht-Ziel: keine Umgestaltung des Bedienkonzepts (keine Live-Suche, kein neues Filter-/Player-UI) | Vorgehensentscheidung „Bedienkonzept" + Schritte 1, 2 | Abgedeckt |
| Nicht-Ziel: Auto-Fortsetzung nicht entfernen/neu ausrichten; bestehendes Ende-Verhalten bleibt | Schritt 5 | Abgedeckt |
| Nicht-Ziel: Kontextmenü der Weiterschauen-Einträge („Ausblenden"/„Überspringen") unverändert; kein Rückbau der Direktwiedergabe an anderen Stellen | Schritt 4 | Abgedeckt |
| Nicht-Ziel: keine gebündelte Umsetzung erzwungen; getrennte Bearbeitung/Abnahme zulässig | Vorgehensentscheidung „Aufteilung" (5 eigenständige Schritte) + Offene Punkte Nr. 5 (einzelne Schritt-Branches, getrennte Abnahme, gemeinsamer PR) | Abgedeckt |

## Abhängigkeitsprüfung

| Prüfpunkt | Befund |
|-----------|--------|
| Zyklen in Abhängigkeiten | Keine gefunden. Nur Schritt 3 deklariert Abhängigkeiten (1, 2) — beide Verweise auf frühere Schritte, auflösbar. Schritte 1, 2, 4, 5 ohne Abhängigkeiten. |
| Reihenfolge konsistent zu inhaltlichen Abhängigkeiten | Ja. Schritt 3 hängt inhaltlich von den Schritten 1 und 2 ab (Abnahme von „Genre-Wechsel" und „neue Suche zeigen Liste von Anfang an" setzt funktionierende Genre-Filterung und Suche voraus; alle drei betreffen dieselbe Seite/UI-Synchronisation) — die Abhängigkeit ist deklariert und die Reihenfolge korrekt. Schritte 4 und 5 betreffen andere Seiten/Abläufe und haben keine inhaltliche Abhängigkeit zu den Schritten 1–3 oder untereinander; die fehlende Deklaration ist korrekt. |

## Hinweise

- Die Schrittbeschreibungen sind jeweils als vollständige Lifecycle-Eingabe tauglich: Ist-Verhalten, Soll-Verhalten, Akzeptanzbezug und Randbedingungen (Nicht-Ziele, Entscheidungen zu Varianten) sind je Schritt enthalten.
- Der Abschnitt „Offene Punkte" dokumentiert entschiedene Annahmen (u. a. Auto-Start der Wiedergabe in Schritt 4, Prüfkriterium/Verhalten bei unbekannter Dauer in Schritt 5, Behandlung leerer Serverlisten in Schritt 1) — diese wurden in die Schrittbeschreibungen eingearbeitet und gelten als geklärt.
- Die in der Anforderung unter „Betroffene Bereiche" genannte Anpassung/Ergänzung der Testsuite (`VideoWebPlayer.Maui.Tests`) ist im Plan nicht explizit benannt; die konkrete Testplanung je Schritt ist Aufgabe des jeweiligen Lifecycle-Durchlaufs (Testabdeckung auf Code-Ebene ist nicht Gegenstand dieser Prüfung).
