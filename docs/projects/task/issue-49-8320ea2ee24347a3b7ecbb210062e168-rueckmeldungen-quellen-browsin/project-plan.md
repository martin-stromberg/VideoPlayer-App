# Projektplan: Nutzerrückmeldungen Quellen-Browsing, Weiterschauen-Navigation und Wiedergabe-Ende

## Übersicht

Das Projekt behebt fünf fachlich unabhängige Nutzerrückmeldungen der Medien-App: drei Mängel der Übersichtsseite einer geöffneten Quelle (fehlender Genre-Filter, überschriebene Sucheingabe, Scrollsprung beim seitenweisen Nachladen), die Navigation der Weiterschauen-Liste der Startseite (Titel-Detailansicht mit Wiedergabe statt Direktwiedergabe — Parität zur Serveranwendung) sowie die unzuverlässige Erkennung des Wiedergabeendes (verfrühter Start des Folgevideos mitten in der laufenden Wiedergabe). Betroffen sind die fachlichen Bereiche Quellen-Browsing, Startseiten-Navigation samt Titel-Detailansicht und Wiedergabe inklusive Abschluss-/Fortsetzungsverhalten. Alle Punkte sind Korrekturen bestehender Abläufe; Server, API-Verträge und das Bedienkonzept bleiben unverändert.

## Grobe Vorgehensentscheidungen

| Bereich | Entscheidung | Begründung |
|---------|-------------|------------|
| Aufteilung | Die fünf Mängel werden als fünf eigenständige Entwicklungsschritte umgesetzt (1:1-Abbildung der fachlichen Anforderungen). | Die Anforderung erlaubt getrennte Bearbeitung ausdrücklich; die Mängel sind fachlich unabhängig, einzeln lieferbar und einzeln testbar. |
| Reihenfolge Quellen-Übersicht | Die drei Schritte der Quellen-Übersichtsseite (Schritte 1–3) werden in der angegebenen Reihenfolge abgearbeitet; Schritt 3 hängt fachlich von den Schritten 1 und 2 ab. | Alle drei betreffen dieselbe Seite und deren UI-Synchronisation — sequenzielle Umsetzung vermeidet Überschneidungen. Zudem verlangt die Abnahme von Schritt 3 eine funktionierende Auslösung von Genre-Wechsel und neuer Suche über die Oberfläche, die erst die Schritte 1 und 2 herstellen. |
| Übrige Schritte | Schritte 4 und 5 sind vollständig unabhängig und können parallel bzw. in beliebiger Position relativ zu den Schritten 1–3 eingeordnet werden. | Sie betreffen andere Seiten und Abläufe (Startseite/Titel-Detailansicht bzw. Wiedergabe) ohne fachliche Überschneidung mit dem Quellen-Browsing. |
| Server und API | Es werden keine Änderungen am Server oder an API-Verträgen vorgenommen; ergibt die Diagnose in Schritt 1 eine serverseitige Ursache, wird diese nur dokumentiert. | Explizites Nicht-Ziel der Anforderung. |
| Bedienkonzept | Die bestehenden Bedienformen bleiben unverändert: horizontale Genre-Chip-Leiste mit „Alle"-Eintrag, Suche ausschließlich über die Such-/Return-Taste, kein neues Filter- oder Player-UI, keine Live-Suche pro Tastenschlag. | Die Anforderung fordert nur Verhaltenskorrekturen, keine Umgestaltung des Bedienkonzepts. |
| Konfiguration | Neue Schwellenwerte (z. B. für die Ende-Plausibilisierung in Schritt 5) werden als feste Konstanten umgesetzt — keine neuen Anwendungseinstellungen. | Die Anforderung verlangt kein neues konfigurierbares Verhalten; die bestehenden Einstellungen (Auto-Fortsetzung, Restlaufzeit-/Angesehen-Fenster) bleiben unverändert wirksam. |

## Entwicklungsschritte

### Schritt 1: Genre-Filter in der Quellen-Übersicht bereitstellen

**Beschreibung:** In der Übersicht einer geöffneten Serien-Quelle (Quellen-Browsing) steht dem Anwender derzeit kein Genre-Filter zur Verfügung, obwohl die dafür vorgesehene horizontale Bedienleiste und der komplette Ladepfad (Abruf der Genres der Quelle vom Server, vorangestellter Eintrag „Alle") in der Anwendung bereits vorhanden sind. Als Teil dieses Schritts ist die Ursache des Fehlens zu diagnostizieren und zu dokumentieren — mögliche Ursachen sind eine leere Serverantwort, ein fehlgeschlagener Ladevorgang oder eine im Auslieferungsstand fehlende Leiste; die Korrektur behebt die festgestellte Ursache. Soll-Verhalten: Liefert der Server Genres für die Quelle, ist die Genre-Leiste sichtbar und enthält neben „Alle" die Genres der Quelle; die Auswahl eines Genres lädt die Trefferliste gefiltert ab der ersten Seite neu, und „Alle" hebt den Filter wieder auf. Es ist keine neue Bedienform gefordert — die vorhandene Chip-Leiste bleibt die Soll-Form. Server und API-Verträge werden nicht geändert; ergibt die Diagnose eine serverseitige Ursache (z. B. liefert der Endpunkt für Serien-Quellen grundsätzlich eine leere Genre-Liste), ist diese zu dokumentieren und die Leiste in diesem Fall so zu behandeln, dass keine leere beziehungsweise funktionslose Filterzeile dargestellt wird.

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Quellen-Browsing, Genre-Filterung, Diagnose/Protokollierung des Genre-Ladepfads

### Schritt 2: Sucheingabe der Quellen-Übersicht darf nicht durch Modell-Aktualisierungen überschrieben werden

**Beschreibung:** Wird in das Suchfeld der Quellen-Übersichtsseite ein Begriff eingetippt, wird die Eingabe bei jeder Modell-Aktualisierung (Synchronisations-, Fortschritts- oder Netzwerkereignisse) mit dem zuletzt abgeschickten Suchtext des Modells überschrieben — unbestätigte Eingaben gehen verloren, eine Suche ist praktisch nicht auslösbar. Soll-Verhalten: Die eingegebene, noch nicht abgeschickte Eingabe bleibt über beliebige Modell-Aktualisierungen hinweg erhalten und wird nicht durch den Modell-Suchtext überschrieben. Das Abschicken über die Such-/Return-Taste löst die Suche mit dem eingegebenen Text aus und lädt die Trefferliste ab der ersten Seite. Der zuletzt abgeschickte Suchtext darf weiterhin im Feld sichtbar bleiben (z. B. bei Rückkehr auf die Seite), damit der aktive Filter erkennbar ist. Das bestehende Suchmuster bleibt fachlich unverändert — es ist keine Live-Suche pro Tastenschlag gefordert.

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Quellen-Browsing, Suche, UI-Synchronisation der Übersichtsseite

### Schritt 3: Scrollposition beim seitenweisen Nachladen der Trefferliste erhalten

**Beschreibung:** Beim automatischen Nachladen der nächsten Ergebnisseite in der Quellen-Übersicht (annäherndes Erreichen des Listenendes) springt die Trefferliste an den Anfang, weil die an die Ansicht gebundene Liste beim Anhängen als Ganzes ausgetauscht wird — benutzerfreundliches Durchscrollen ist nicht möglich. Soll-Verhalten: Beim Nachladen einer Folgeseite bleibt die bisherige Scrollposition der Trefferliste erhalten; die nachgeladenen Einträge erscheinen am Ende angehängt. Dieses Verhalten gilt gleichermaßen für die ungefilterte Liste und für Such- beziehungsweise Genre-Treffer (derselbe Anzeige- und Nachladepfad). Erstladen, ein Genre-Wechsel und eine neue Suche zeigen die Liste weiterhin von Anfang an. Die technische Umsetzungsform ist frei (z. B. inkrementelle Erweiterung der gebundenen Liste statt Austausch der gesamten Liste); entscheidend ist nur das Ergebnis, dass die Ansicht nicht springt.

**Abhängigkeiten:** 1, 2

**Betroffene Bereiche:** Quellen-Browsing, Trefferliste und seitenweises Nachladen, UI-Synchronisation der Übersichtsseite

### Schritt 4: Weiterschauen-Eintrag öffnet die Titel-Detailansicht mit Wiedergabe

**Beschreibung:** Ein Tipp auf einen Eintrag der Weiterschauen-Liste der Startseite startet bei direkt abspielbaren Medientypen (Film, Serienepisode) derzeit unmittelbar die Wiedergabe, ohne dass Titelinformationen einsehbar sind; nach dem Schließen des Players landet der Anwender auf der Startliste. Soll-Verhalten (Parität zur Serveranwendung): Ein Tipp auf einen solchen Eintrag öffnet die Titel-Detailansicht des Titels mit Wiedergabe — die Wiedergabe liegt über der geöffneten Detailansicht, sodass „Player schließen" zur Detailansicht des Titels zurückkehrt statt zur Startliste und die Titelinformationen ohne Umweg über die Quellenansicht einsehbar bleiben. Die bisher für die Direktwiedergabe nachgeladene Serienkontext-Funktion (Fortsetzungsliste der Folge-Episoden) bleibt als Funktion erhalten. Das bestehende Verhalten nicht direkt abspielbarer Einträge (Serie, Staffel, Kollektion — Navigation zur Detailansicht) und das bewusst eingeführte Kontextmenü der Weiterschauen-Einträge („Ausblenden"/„Überspringen") bleiben unverändert; die Direktwiedergabe an anderen Stellen wird nicht zurückgebaut. Playlist-gebundene Weiterschauen-Einträge behalten den bisherigen direkten Playlist-Start (der Playlist-Kontext ginge auf einer Detailansicht verloren). Offline-Einträge mit vorhandenen Kontext-Kennungen öffnen die Titel-Detailansicht mit Wiedergabe; Einträge ohne nutzbaren Kontext (fehlende Serien-/Staffel-/Episoden-Kennungen, nicht wählbare Platzhalter) behalten das bisherige Verhalten.

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Startseite (Weiterschauen-Liste), Navigation, Titel-Detailansicht, Wiedergabe-Einstieg, Serienkontext für Folge-Episoden

### Schritt 5: Zuverlässige Erkennung des Wiedergabeendes

**Beschreibung:** Das Ende-Ereignis des Wiedergabe-Elements löst derzeit ungeprüft den Abschluss-Ablauf aus: Das Medium wird als angesehen markiert, die Endposition wird gemeldet und bei aktivierter Auto-Fortsetzung startet das Folgevideo. Feuert das Ereignis verfrüht (z. B. bei Stream-Abbruch, Formatwechsel oder Positionswechsel), startet das Folgevideo mitten in der laufenden Wiedergabe und das Medium wird fälschlich als vollständig angesehen gemeldet — eine Plausibilitätsprüfung oder ein Debounce fehlt an jeder Stelle der Kette. Soll-Verhalten: Ein Ende-Ereignis wird erst dann als tatsächliches Wiedergabeende gewertet, wenn es gegen den Wiedergabestand plausibilisiert ist (z. B. Position nahe/an der Gesamtdauer oder ein vergleichbarer Ansatz mit Zeit-Schwelle); nicht plausible Ereignisse lösen weder den Auto-Vorlauf/Folgevideo-Start noch die Angesehen-Markierung und End-Fortschrittsmeldung aus. Beim tatsächlichen Wiedergabeende läuft der bestehende Abschluss-Ablauf unverändert weiter (Angesehen-Markierung, Fortschrittsmeldung der Endposition, Auto-Fortsetzung bei aktiviertem Schalter). Die Prüfung gilt einheitlich für den Serien-/Einzelkontext und den Playlist-Kontext; die konkrete Regel und ihre Schwelle sind nicht vorgegeben und werden als fester Wert umgesetzt (kein neues konfigurierbares Verhalten).

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Wiedergabe, Ende-Erkennung, Fortschritts-/Angesehen-Meldung, Auto-Fortsetzung (Serien- und Playlist-Kontext)

## Übersichtstabelle

| # | Titel | Abhängigkeiten | Betroffene Bereiche |
|---|-------|----------------|----------------------|
| 1 | Genre-Filter in der Quellen-Übersicht bereitstellen | Keine | Quellen-Browsing, Genre-Filterung, Ladepfad-Diagnose |
| 2 | Sucheingabe der Quellen-Übersicht gegen Überschreiben schützen | Keine | Quellen-Browsing, Suche, UI-Synchronisation |
| 3 | Scrollposition beim seitenweisen Nachladen erhalten | 1, 2 | Quellen-Browsing, Trefferliste, seitenweises Nachladen |
| 4 | Weiterschauen-Eintrag öffnet Titel-Detailansicht mit Wiedergabe | Keine | Startseite, Navigation, Titel-Detailansicht, Wiedergabe-Einstieg |
| 5 | Zuverlässige Erkennung des Wiedergabeendes | Keine | Wiedergabe, Fortschritts-/Angesehen-Meldung, Auto-Fortsetzung |

## Offene Punkte

Keine verbleibenden offenen Punkte. Die im Planungslauf identifizierten Punkte wurden anhand der empfohlenen Vorschläge als Annahmen entschieden (Nutzerauftrag: eigenständige Bearbeitung bis zum Merge; Schweigen gilt nicht als Antwort, daher dokumentierte Annahmen):

| # | Behandelter Punkt | Gewählte Annahme/Entscheidung |
|---|-------------------|-------------------------------|
| 1 | Schritt 1 — Ursache des fehlenden Genre-Filters (leere Serverantwort / Ladefehler / fehlende Leiste) und Verhalten bei leerer Serverliste | Entschieden wie vorgeschlagen: Diagnose mit Protokoll im Rahmen von Schritt 1, Ursache dokumentieren und beheben; bei nachweislich leerer Serverliste wird die Leiste ausgeblendet (keine funktionslose „Alle"-nur-Zeile). Serverseitige Ursache nur dokumentieren. |
| 2 | Schritt 4 — Auto-Start der Wiedergabe vs. nur Detailansicht öffnen | Entschieden wie vorgeschlagen: Die Wiedergabe startet beim Tipp automatisch über der geöffneten Detailansicht (Parität zur Serveranwendung, „Detailansicht des Titels mit Player"); „Player schließen" kehrt zur Detailansicht zurück. |
| 3 | Schritt 4 — Playlist-gebundene Einträge, Offline-Pfad, Einträge ohne Detail-Kontext | Entschieden wie vorgeschlagen und in die Schrittbeschreibung eingearbeitet: Playlist-Einträge behalten den Playlist-Direktstart; Offline-Einträge mit Kontext-Ids öffnen die Detailansicht mit Wiedergabe; Einträge ohne nutzbaren Kontext bleiben unverändert. |
| 4 | Schritt 5 — Konkretes Prüfkriterium, Verhalten bei unbekannter Dauer, Playlist-Kontext | Entschieden wie vorgeschlagen: Plausibilitätscheck Position gegen Gesamtdauer mit festem Toleranzfenster (Orientierung am bestehenden Angesehen-Endfenster); bei unbekannter/0-Dauer kein Ende bzw. kurze Entprellung; einheitlich für Serien- und Playlist-Kontext; Konstante, keine neue Einstellung. |
| 5 | Aufteilung/Lieferung | Entschieden abweichend vom Vorschlag: Ein gemeinsamer Pull Request des Projekt-Basisbranches gegen `staging` (expliziter Nutzerauftrag). Die fünf Schritte werden dennoch einzeln auf eigenen Schritt-Branches entwickelt, abgenommen und per Merge-Commit in den Basisbranch integriert — die getrennte Abnehmbarkeit bleibt erhalten. |
