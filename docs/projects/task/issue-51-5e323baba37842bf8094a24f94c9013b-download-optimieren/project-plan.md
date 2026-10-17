# Projektplan: Download optimieren (Issue #51)

## Übersicht

Die Video-Downloads der App sollen zuverlässig und transparent werden:
Downloads laufen auf iOS auch bei Hintergrundausführung weiter bzw. werden
abgeschlossen; verwaiste und unvollständige Dateireste werden erkannt und
bereinigt, sodass die angezeigte Speicherbelegung mit dem tatsächlichen Stand
übereinstimmt; der Anwender erhält Größenanzeige pro Download sowie die
Möglichkeit, gezielt einzelne oder alle Downloads zu löschen. Betroffen sind
die Download-Ausführung (Warteschlange/Transfer), die iOS-Plattformschicht,
der Abgleich zwischen Download-Manifest und Dateisystem sowie die
Downloads-Seite der App.

## Grobe Vorgehensentscheidungen

| Bereich | Entscheidung | Begründung |
|---------|-------------|------------|
| Hintergrund-Übertragung | Auf iOS wird die eigentliche Dateiübertragung an eine systemseitige Hintergrund-Übertragung delegiert (iOS-Background-URL-Session); die bestehende Auftragsverwaltung (Manifest, Warteschlange, Kontingent, WLAN-Bedingung) bleibt führend. Auf anderen Plattformen bleibt der bisherige prozessinterne Transfer. | iOS suspendiert App-Prozesse; nur eine systemverwaltete Übertragung läuft zuverlässig im Hintergrund weiter. Die bewährte Auftrags-/Anzeige-Logik bleibt unverändert nutzbar. |
| Verwaiste Dateien | Dateien im Video-Ablageort ohne zugehörigen Manifest-Eintrag sowie verwaiste Teildateien werden durch einen Abgleich zwischen Dateisystem und Manifest erkannt. Vom Anwender nicht erreichbare Restdaten bleiben auf der Downloads-Seite sichtbar und werden per expliziter Bereinigungs-Aktion entfernt; zweifelsfrei überholte Teildateien (Reste abgebrochener/fehlgeschlagener Transfers zum selben Medium) entfernt der Abgleich automatisch. | Entscheidung des Stakeholders: Kontrolle bleibt beim Anwender, ohne dass bekannte Trümmer liegen bleiben. Behebt das beobachtete Fehlerbild (14 GB belegt bei fast leerer Liste) an der Wurzel. |
| Anwenderkontrolle | Die Downloads-Seite zeigt Größe je Eintrag und den tatsächlich belegten Speicher; einzelne und alle Downloads sowie erkannte Restdaten können gezielt entfernt werden. | Direkt geforderte Transparenz und Kontrolle; baut auf dem Abgleich auf. |
| Reihenfolge | Erst Datenhygiene (Abgleich/Bereinigung), dann Anzeige/Kontrolle, dann die iOS-Hintergrund-Übertragung. | Die Hintergrund-Übertragung erzeugt neue Vollendungs-/Abgleichspfade (Vervollständigung während Suspendierung); sie profitiert von der zuvor vorhandenen Manifest↔Dateisystem-Abgleichslogik. |

## Entwicklungsschritte

### Schritt 1: Verwaiste und unvollständige Download-Daten erkennen und bereinigen

**Beschreibung:** Die App soll Dateireste im Video-Ablageort zuverlässig
erkennen und entfernen, die keinem Eintrag im Download-Manifest mehr
zugeordnet sind oder zu abgebrochenen/fehlgeschlagenen Aufträgen gehören.
Beim App-Start und beim Öffnen der Downloads-Seite wird der Ablageort mit dem
Manifest abgeglichen: Dateien ohne Manifest-Eintrag werden als verwaiste
Restdaten erkannt und gesondert erfasst (Größe und Anzahl), aber nicht
automatisch entfernt — sie bleiben für die Bereinigungs-Aktion des Anwenders
verfügbar (Schritt 2). Zweifelsfrei überholte Teildateien — Reste
abgebrochener oder fehlgeschlagener Transfers zum selben Medium — entfernt
der Abgleich dagegen automatisch. Manifest-Einträge im Zustand „läuft", zu
denen kein laufender Transfer existiert, gelten als unterbrochen und folgen
der bestehenden Wiederaufnahme-Logik; Einträge, deren Datei fehlt, obwohl sie
als „fertig" geführt werden, werden als fehlerhaft bzw. erneut einplanbar
behandelt. Die Belegungsanzeige („x GB von y GB belegt") soll die tatsächlich
belegten Daten einschließlich erkannter Restdaten ausweisen, sodass Anzeige
und Dateisystem übereinstimmen. Der Abgleich muss protokolliert werden und
darf niemals Dateien entfernen, die einem bestehenden Manifest-Eintrag in
einem aktiven oder abgeschlossenen Zustand zugeordnet sind.
Akzeptanzkriterien: (1) Eine von außen in den Ablageort gelegte oder durch
Prozessabbruch zurückgebliebene Datei ohne Manifest-Eintrag wird beim
Abgleich als Restdaten erkannt (kein stilles Löschen, kein unsichtbarer
Speicherverbrauch). (2) Ein „fertig" geführter Eintrag ohne Datei wird als
nicht mehr vorhanden behandelt und kann erneut eingeplant werden. (3) Die
angezeigte Belegung entspricht dem tatsächlichen Dateibestand.
(4) Laufende oder wartende Downloads werden durch den Abgleich nicht
beeinträchtigt.

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Download-Auftragsverwaltung, lokale Ablage,
Downloads-Seite (Belegungsanzeige)

### Schritt 2: Speichertransparenz und Löschkontrolle in der Download-Verwaltung

**Beschreibung:** Die Downloads-Seite soll dem Anwender volle Kontrolle und
Transparenz über die heruntergeladenen Daten geben. Je Download-Eintrag wird
die tatsächliche Dateigröße angezeigt (bei laufenden Downloads zusätzlich zum
Fortschritt die bislang belegte Größe; bei fertigen Downloads die
Dateigröße). Die Gesamtanzeige der Belegung soll zwischen verwalteten
Downloads und — sofern vorhanden — erkannten Restdaten unterscheiden bzw.
nach erfolgter Bereinigung den realen Stand zeigen. Der Anwender kann
weiterhin jeden Download einzeln löschen und erhält zusätzlich eine Aktion
„Alle Downloads löschen" (mit Rückfrage), die alle Aufträge abbricht, alle
Dateien entfernt und das Manifest leert. Erkannte Restdaten ohne Zuordnung
sollen über eine erkennbare Bereinigungs-Aktion (z. B. „Speicher bereinigen"
mit Angabe der freizugebenden Größe) entfernbar sein. Akzeptanzkriterien:
(1) Jeder Listeneintrag zeigt eine Größenangabe. (2) „Alle Downloads
löschen" leert Liste und Ablageort vollständig und fragt vorher nach.
(3) Restdaten sind erkennbar und per Aktion entfernbar. (4) Die Funktionen
sind auch offline verfügbar (lokale Daten).

**Abhängigkeiten:** 1

**Betroffene Bereiche:** Downloads-Seite (Anzeige und Aktionen),
Download-Auftragsverwaltung, lokale Ablage

### Schritt 3: Video-Downloads unter iOS im Hintergrund abschließen

**Beschreibung:** Auf iOS sollen gestartete Video-Downloads auch dann
zuverlässig zu Ende geführt werden, wenn die App in den Hintergrund wechselt
oder suspendiert wird. Die Dateiübertragung wird dafür an die vom System
angebotene Hintergrund-Übertragung delegiert (iOS-Background-URL-Session),
sodass iOS den Download prozessunabhängig fortführt und die App beim
Abschluss oder bei Rückkehr in den Vordergrund über das Ergebnis informiert
wird. Die bestehenden fachlichen Regeln bleiben wirksam: Warteschlange mit
begrenzter Parallelität (die App-seitige Grenze von maximal zwei aktiven
Transfers gilt auch für systemseitige Hintergrund-Übertragungen),
Speicherplatzkontingent, WLAN-Bedingung („Downloads nur im WLAN" wird auf die
Hintergrund-Übertragung übertragen, sodass diese nur über ungedrosseltes Netz
läuft), Abbruch und Löschen laufender Downloads sowie die Zustandsanzeige in
der Downloadliste. Kehrt die App aus
dem Hintergrund zurück oder startet neu, wird der Transfer-Stand mit dem
Manifest abgeglichen: im Hintergrund vollendete Downloads erscheinen als
„fertig", vom System abgebrochene als „Fehler" mit verständlichem Hinweis,
unvollständige Teildateien werden gemäß der Abgleichslogik behandelt. Auf
den übrigen Plattformen (Windows, Android, MacCatalyst) bleibt der bisherige
Ablauf unverändert. Akzeptanzkriterien: (1) Ein laufender Download wird nach
dem Wechsel der App in den Hintergrund fortgeführt und die Datei
vollständig abgelegt; der Eintrag steht anschließend auf „fertig".
(2) Wartende Downloads rücken auch nach einer Hintergrund-Vervollständigung
nach bzw. werden beim nächsten Vordergrund-/Start-Ereignis gestartet.
(3) Abbruch und Löschen wirken auch auf systemseitig laufende Transfers.
(4) Die fachlichen Regeln (Kontingent, WLAN-Bedingung, max. zwei parallel)
gelten unverändert. (5) Der Zustand der Downloadliste stimmt nach
App-Neustart mit dem Ergebnis der Hintergrund-Transfers überein.

**Abhängigkeiten:** 1

**Betroffene Bereiche:** Download-Transfer (iOS-Plattformschicht),
Download-Auftragsverwaltung, App-Lebenszyklus iOS

## Übersichtstabelle

| # | Titel | Abhängigkeiten | Betroffene Bereiche |
|---|-------|-----------------|----------------------|
| 1 | Verwaiste und unvollständige Download-Daten erkennen und bereinigen | Keine | Download-Auftragsverwaltung, lokale Ablage, Downloads-Seite |
| 2 | Speichertransparenz und Löschkontrolle in der Download-Verwaltung | 1 | Downloads-Seite, Download-Auftragsverwaltung, lokale Ablage |
| 3 | Video-Downloads unter iOS im Hintergrund abschließen | 1 | Download-Transfer (iOS), Auftragsverwaltung, App-Lebenszyklus |

## Offene Punkte

Keine. (Die drei ursprünglich offenen Punkte wurden vom Stakeholder
entschieden: sichtbare Restdaten mit Bereinigungs-Aktion plus automatisches
Entfernen zweifelsfrei überholter Teildateien; WLAN-Bedingung gilt auch für
Hintergrund-Transfers; die Parallelitätsgrenze von zwei bleibt führend.)
