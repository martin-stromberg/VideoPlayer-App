# Projektplan: Design-Angleichung an Webanwendung (Kundenfeedback)

Quelle: Kundenrückmeldung Issue #9 (`requirement.md`, FA-1 bis FA-7).
Design-Referenz: deployte Blazor-Webanwendung (`web-design-recherche.md`).
Ist-Stand: `inventory.md`.

## Übersicht

Die .NET-MAUI-App (Windows + iOS) soll in Aussehen und Bedienung der Webanwendung
entsprechen: tiefschwarzer Hintergrund statt Dunkelgrau, Web-Design-Tokens für Farben,
Flächen und Eckenradien, Hero-Kopfbereich auf der Startseite mit Einstellung,
Vollbreiten-Header auf den Detailseiten und ein Angesehen-Symbol bei Episoden.
Zusätzlich sind drei gemeldete Laufzeitfehler zu beheben: Flickern der Download-Seite
bei Fortschrittsanzeige, nicht klickbare Player-Steuerelemente in der Vollansicht und
ein Absturz (COMException) bei der Zweitwiedergabe.

Betroffene Bereiche: app-weites Styling (zentrale Stil-Ressourcen, alle Seiten und
Steuerelemente, App-Navigation), Startseite, Einstellungen, Detailansichten
(Serien/Staffeln/Episoden/Filme/Kollektionen), Episodenlisten und Datenübernahme vom
Server, Downloads-Übersicht, Wiedergabe. Die Blazor-Webanwendung selbst bleibt
unverändert; die Server-API wird nicht geändert.

## Grobe Vorgehensentscheidungen

| Bereich | Entscheidung | Begründung |
|---------|-------------|------------|
| Styling | Zuerst werden die Web-Design-Tokens als zentrale Stil-Ressourcen der App eingeführt und sämtliche hartcodierten Farben/Radien in den Seiten und Steuerelementen darauf umgestellt. | Fundament für alle folgenden Design-Schritte: Hero, Detail-Header und Angesehen-Badge greifen auf dieselben Tokens zu, statt erneut hartcodierte Werte einzuführen, die später erneut angefasst werden müssten. |
| Design-Referenz | Maßgeblich ist ausschließlich der dokumentierte, deployte Stand der Webanwendung (`web-design-recherche.md`). Die Webanwendung wird nicht verändert; die Server-API bleibt unverändert (nur öffentliche Endpunkte). | Explizite Nicht-Ziele der Anforderung; die Recherche enthält die verifizierten Token-Werte und Markup-Muster. |
| „Runde Buttons" | Normale Buttons erhalten den 4px-Radius der Webanwendung; pill-förmig (999px) sind nur Chips, Badges und runde Icon-Schaltflächen. | Kundenzitat „sämtliche Buttons genauso rund wie in der Webanwendung" — die Recherche zeigt, dass die Web-Buttons selbst nur 4px-Radius haben; vollständig runde Elemente sind dort auf Chips/Badges beschränkt. |
| Fehlerbehebungen | Die drei Laufzeitfehler (Downloads-Flickern, Player-Bedienbarkeit, Absturz bei Zweitwiedergabe) werden als eigenständige, frühe Schritte ohne Abhängigkeit zum Design-Umbau umgesetzt. | Die Defekte sind fachlich unabhängig vom Design und vom Kunden als eigene Störungen gemeldet; sie können parallel zum Design-Arbeitsstrang geliefert werden und verbessern die App sofort. |
| Gesehen-Status | Der vom Server bereits mitgelieferte Gesehen-Zeitpunkt wird rein clientseitig ins Datenmodell der App übernommen und gerendert. | Bestandsaufnahme: Der Server sendet das Feld bereits; die App ignoriert es bislang still. Keine Server-Änderung nötig. |
| Teststrategie | Es bleibt bei der etablierten Konvention aus MVU- und Contract-Tests; für die manuell beobachtbaren Punkte (Flickern, Klickbarkeit, Zweitwiedergabe) wird das Ergebnis zusätzlich durch manuelle Prüfung bzw. einen Workflow-Nachweis abgesichert. Kein Test wird abgeschwächt oder deaktiviert. | Es existieren keine Rendering-/E2E-Tests; die geforderten Ergebnisse (FA-5 bis FA-7) sind primär Laufzeitverhalten. |

## Entwicklungsschritte

### Schritt 1: Einheitliches Erscheinungsbild nach Web-Vorbild

**Beschreibung:** Die App soll durchgängig das dunkle Erscheinungsbild der Webanwendung
übernehmen — der Kunde beanstandet ausdrücklich das „hässliche Dunkelgrau" und fordert
den tiefschwarzen Hintergrund sowie die Rundungen der Webanwendung. Umzusetzen ist:
Die zentralen Stil-Ressourcen der App werden auf die dokumentierten Design-Tokens der
Webanwendung umgestellt — Seitenhintergrund `#131313` bzw. `#0e0e0e` je Fläche,
Flächenfarben `#1c1b1b` bis `#393939` abgestuft, Textfarben `#e5e2e1` (normal),
`#bda9a6` (gedämpft), `#e9bcb6` (weich), Rahmen `rgba(255,255,255,0.1)` bzw.
`rgba(255,180,170,0.42)`, Primärfarbe `#e50914` mit Hover `#c0000c` und Soft-Variante
`#ffb4aa`, Sekundärfarbe `#00a8e8` mit Soft-Variante `#83cfff`, Danger `#ffb4ab`.
Eckenradien: normale Buttons 4px (wie die Web-Buttons), Karten 8px, Chips, Badges und
runde Icon-Schaltflächen pill-förmig. Alle bislang hartcodierten Hexfarben in den
Seiten und Steuerelementen (Startseite, Detailseite, Downloads, Favoriten, Profil,
Quellenübersicht, Player, Login, Einstellungen, Server-Einrichtung, Medien-Carousel)
werden durch die zentralen Ressourcen ersetzt, sodass keine verstreuten Farbwerte mehr
existieren. Die App-Navigation (Shell/Flyout) erhält ebenfalls die Web-Farbgebung.
Das bisherige Verhalten der App bleibt unverändert — es ist eine reine
Design-Umstellung. Akzeptanzkriterien: Der App-Hintergrund entspricht dem Schwarzton
der Webanwendung; alle dokumentierten Tokens sind in den Stilen abgebildet; normale
Buttons haben 4px-Radius, Chip-/Badge-Elemente sind pill-förmig; in den Seiten und
Steuerelementen sind keine hartcodierten Hexfarben mehr vorhanden; die Bedienung ist
auf Windows und iOS unverändert. Alle bestehenden Tests bleiben grün; es gilt die
Definition of Done (Build in Debug und Release fehlerfrei, volle Testsuite grün).

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** App-weites Styling (zentrale Stil-Ressourcen), alle Seiten und
Steuerelemente, App-Navigation

### Schritt 2: Downloads-Übersicht ohne Flackern bei der Fortschrittsanzeige

**Beschreibung:** Ist ein Titel in den Downloads eingeplant oder läuft ein Download,
flackert laut Kundenrückmeldung die gesamte Downloads-Seite, während sich der
Fortschrittsbalken verändert. Gefordert ist: Während laufender oder geplanter
Downloads aktualisiert sich der Fortschrittsbalken flüssig, ohne dass die übrige
Anzeige sichtbar neu gezeichnet wird oder ruckelt — es darf sich nur der betroffene
Listeneintrag bzw. nur dessen Fortschrittsanzeige verändern. Die Bestandsaufnahme hat
die Ursache bereits eingegrenzt: Die Seite baut bei jeder Fortschrittsmeldung die
komplette Liste neu auf, obwohl bereits granular gemeldet wird, welcher einzelne
Download-Eintrag sich geändert hat. Gefordert ist ein Aktualisierungspfad, der diese
granularen Änderungen nutzt, statt die gesamte Liste bei jedem Fortschritts-Tick neu
zu setzen. Akzeptanzkriterien: Bei eingeplantem/laufendem Download verändert sich
sichtbar nur der Fortschrittsbalken des betroffenen Eintrags; kein Flackern oder
Neuzeichnen der übrigen Anzeige (manuell beobachtbar). Die bestehenden
Download-Übergänge (Laden, Einreihen, Abbrechen, Löschen, Fortsetzen, automatische
Downloads) funktionieren unverändert. Alle bestehenden Tests bleiben grün; es gilt die
Definition of Done (Build in Debug und Release fehlerfrei, volle Testsuite grün).

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Downloads-Übersicht, Download-Fortschrittsmeldungen

### Schritt 3: Videoplayer — Steuerelemente in der Vollansicht bedienbar

**Beschreibung:** Wird ein Video in der normalen großen Ansicht gestartet, sind laut
Kundenrückmeldung die Videosteuerelemente nicht klickbar; erst über den Mini-Player
lässt sich das Video etwa pausieren. Gefordert ist: In der normalen Vollansicht der
Wiedergabe sind alle Steuerelemente — mindestens Play/Pause und Seek sowie die
übrigen nativen Transportfunktionen — unmittelbar bedienbar, ohne dass zuvor der
Mini-Player (Bild-in-Bild) geöffnet werden muss. Die Bestandsaufnahme hat die
plausible Ursache eingegrenzt: Eine Überlagerung der App (Kopfzeile mit Titel,
Zurück- und Mini-Player-Schaltfläche) erstreckt sich unsichtbar über die gesamte
Wiedergabefläche und fängt die Eingaben ab, obwohl nur ihr oberer Bereich sichtbaren
Inhalt zeigt; im Mini-Player ist die Überlagerung ausgeblendet, weshalb die Bedienung
dort funktioniert. Die App-Überlagerung muss funktionsfähig bleiben, darf aber die
nativen Steuerelemente nicht blockieren. Akzeptanzkriterien: Play/Pause und Seek sind
in der Vollansicht unter Windows sofort bedienbar; Zurück- und Mini-Player-Schalter
der App funktionieren weiterhin; der Mini-Player bleibt unverändert nutzbar. Alle
bestehenden Tests bleiben grün; es gilt die Definition of Done (Build in Debug und
Release fehlerfrei, volle Testsuite grün).

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Wiedergabe-Ansicht

### Schritt 4: Videoplayer — wiederholte Wiedergabe ohne Absturz

**Beschreibung:** Nachdem einmal ein Video abgespielt wurde, lässt sich laut
Kundenrückmeldung kein weiteres abspielen — die App stürzt mit einer unbehandelten
COMException ab (`HResult 0x8001010E`, RPC_E_WRONG_THREAD, beim Zugriff auf das
native Player-Element aus einem Suchlauf auf einem Threadpool-Thread). Gefordert
ist: Nach erfolgreicher Wiedergabe eines Videos muss ein weiteres Video abspielbar
sein, beliebig wiederholbar, ohne unbehandelte Ausnahme. Zu beheben ist die Ursache,
nicht nur das Symptom: Die Wiedergabesteuerung (insbesondere Positionssprünge und
Wiedergabebefehle) darf das native Player-Element nur auf dem UI-Thread anfassen, und
bereits verlassene Wiedergabe-Seiten dürfen nicht weiter auf Modelländerungen
reagieren — die Bestandsaufnahme zeigt, dass die Wiedergabe-Seite bei jedem Start
neu erzeugt wird, sich aber nie von Modelländerungen abmeldet, sodass verwaiste
Seiten auf ein losgelöstes Player-Element zugreifen können. Liegt die Ausnahme im
verwendeten Wiedergabe-Baustein selbst, ist ein app-seitiger Workaround zulässig,
sofern das beobachtbare Verhalten stimmt. Akzeptanzkriterien: Mindestens zwei
Wiedergaben hintereinander laufen ohne Absturz (Nachweis durch Test oder
dokumentierten Workflow); die beschriebene COMException tritt nicht mehr auf; die
Fortschrittsmeldung an den Server und das Gesehen-Markieren funktionieren
unverändert. Alle bestehenden Tests bleiben grün; es gilt die Definition of Done
(Build in Debug und Release fehlerfrei, volle Testsuite grün).

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Wiedergabe-Ansicht, Wiedergabe-Steuerung

### Schritt 5: Startseite — Hero-Kopfbereich mit ausblendbarer Einstellung

**Beschreibung:** Die Startseite soll wie die Webanwendung oberhalb der Inhalte einen
Hero-Kopfbereich zeigen — der Kunde fordert ausdrücklich: „Der Kopfbereich mit dem
generierten Bild soll auch in der App gezeigt werden. Er soll in den Einstellungen
ausblendbar sein. Die Einstellung muss positiv gestaltet sein, also so, dass der
Kopfbereich aktivierbar ist und standardgemäß aktiviert ist." Umzusetzen ist: Die
Startseite lädt das servergenerierte Hero-Bild über den öffentlichen Endpunkt
`api/pictures/hero-background` (Authentifizierung über den bestehenden
Bearer-Mechanismus der App; der Server liefert ein generiertes JPEG bzw. einen
Placeholder) und zeigt es vollbreit oberhalb der bisherigen Inhaltslisten an —
Cover-Darstellung wie `background-size: cover` im Web, Höhe responsiv (Web-Referenz:
`clamp(260px, 42vw, 520px)`), mit geschichteten Dunkelgradienten (seitlicher
Verlauf, Bottom-Fade in den Seitenhintergrund, dezente Akzente in Primär- und
Sekundärfarbe) und darauf platziertem Inhalt: Kicker-Chip, Titel und
Beschreibungstext entsprechend dem Web-Markup. Das Hero-Bild wird nicht dauerhaft
gespeichert (flüchtige Zwischenablage genügt). Bei fehlender Verbindung oder
Fehlern verhält sich die Startseite stabil (Placeholder des Servers bzw. sauberes
Auslassen ohne Layout-Bruch). In den Einstellungen gibt es einen positiv
formulierten Schalter (Sinnbild „Kopfbereich anzeigen"), Standardwert aktiviert,
der den Hero wirksam ein-/ausblendet; der Wert wird wie die übrigen
App-Einstellungen persistiert und überlebt einen App-Neustart. Die Einstellung wird
app-lokal gehalten (Annahme aus der Anforderung). Akzeptanzkriterien: Startseite
zeigt den Hero mit servergeneriertem Bild im Web-Erscheinungsbild; der Schalter
ist positiv formuliert, standardmäßig aktiv und blendet den Hero wirksam aus/ein;
der Wert überlebt einen Neustart; ohne Verbindung kein Layout-Bruch. Alle
bestehenden Tests bleiben grün; es gilt die Definition of Done (Build in Debug und
Release fehlerfrei, volle Testsuite grün).

**Abhängigkeiten:** 1

**Betroffene Bereiche:** Startseite, Einstellungen, Server-Bildbezug

### Schritt 6: Detailseiten — Vollbreiten-Header mit Hintergrundgrafik

**Beschreibung:** Die Detailseiten sollen ihren Kopfbereich wie die Webanwendung
gestalten — Kundenzitat: „Die Hintergrundgrafik soll auf kompletter Breite gezeigt
werden. Darauf sind dann die Serien-, Staffel-, oder Episodeninformationen
platziert." Umzusetzen ist: Serien-, Staffel-, Episoden-, Film- und
Kollektions-Detailseiten zeigen statt des bisherigen gepaddeten Posterbereichs einen
Header mit Hintergrundgrafik auf kompletter Breite (Cover-Darstellung, Höhe
responsiv — Web-Referenz `clamp(360px, 52vw, 620px)`). Auf der Grafik liegt ein
Dunkelgradient-Overlay (seitlicher Verlauf plus Bottom-Fade in den Seitenhintergrund
wie im Web); unten links auf dem Overlay sind die jeweiligen Informationen platziert:
Titel, Meta (Jahr, Genres in der weichen Primärfarbe) und Plot, sowie die Aktions-
und Favoriten-Schaltflächen entsprechend dem Web-Header-Muster. Bildquelle: Bei
Serien, Staffeln, Filmen und Kollektionen das jeweilige Backdrop über die bestehende
Bild-API (`api/pictures/{id}`) unter Beachtung der Web-Fallbackkette (generiertes
Hintergrundbild, dann Banner, dann Poster); bei Episoden der Episoden-Still über den
Endpunkt `api/episodes/{id}/background-image`. Wie im Web wechselt der Header der
Serien-Detailseite auf das Still der jeweils ausgewählten Episode. Darunter bleiben
die bisherigen Inhalte (Staffel-Auswahl, Episodenliste, Kollektions-Filme) erhalten.
Akzeptanzkriterien: Alle genannten Detailseiten zeigen je einen Vollbreiten-Header
mit Hintergrundgrafik, Overlay und darauf platziertem Titel/Meta/Plot entsprechend
dem Web-Muster; die Episoden-Auswahl wechselt das Header-Bild auf das Episoden-Still;
ohne verfügbares Bild greift die Fallbackkette bzw. der Header bricht nicht
zusammen. Alle bestehenden Tests bleiben grün; es gilt die Definition of Done
(Build in Debug und Release fehlerfrei, volle Testsuite grün).

**Abhängigkeiten:** 1

**Betroffene Bereiche:** Detailansichten (Serien, Staffeln, Episoden, Filme,
Kollektionen), Server-Bildbezug

### Schritt 7: Episodenlisten — Angesehen-Symbol bei gesehenen Episoden

**Beschreibung:** Bei den Episoden fehlt laut Kundenrückmeldung das
Angesehen-Symbol. Gefordert ist: In Episodenlisten (Staffel-Ansicht der
Detailseite) wird pro Episode ein Angesehen-Symbol angezeigt, wenn die Episode als
gesehen markiert ist — wie in der Webanwendung: ein rundes, dunkles Badge mit
Augen-Symbol in der weichen Primärfarbe, über dem Episoden-Poster positioniert
(oben links über dem Bild wie im Web-Muster), gerendert nur bei gesetztem
Gesehen-Zeitpunkt; ungesehene Episoden zeigen kein Symbol. Der Server liefert den
Gesehen-Zeitpunkt (`WatchedAt`) bereits mit den Einträgen mit — die App übernimmt
das bislang ignorierte Feld in ihr Datenmodell und nutzt es für die Anzeige. Es ist
keine Server-Änderung erforderlich; die bisherige Gesehen-Logik (automatisches
Markieren am Ende der Wiedergabe, Fortschrittsmeldung) bleibt unverändert, der
Schritt betrifft nur Übernahme und Darstellung des vorhandenen Status.
Akzeptanzkriterien: Episoden mit gesetztem Gesehen-Zeitpunkt zeigen das
Augen-Badge nach Web-Vorbild; ungesehene Episoden zeigen keins; der Status
aktualisiert sich, wenn eine Episode gesehen wurde. Alle bestehenden Tests bleiben
grün; es gilt die Definition of Done (Build in Debug und Release fehlerfrei, volle
Testsuite grün).

**Abhängigkeiten:** 6

**Betroffene Bereiche:** Episodenlisten der Detailansicht, Datenübernahme vom Server

## Übersichtstabelle

| # | Titel | Abhängigkeiten | Betroffene Bereiche |
|---|-------|----------------|---------------------|
| 1 | Einheitliches Erscheinungsbild nach Web-Vorbild | Keine | App-weites Styling, alle Seiten, Navigation |
| 2 | Downloads-Übersicht ohne Flackern bei der Fortschrittsanzeige | Keine | Downloads-Übersicht, Fortschrittsmeldungen |
| 3 | Videoplayer: Steuerelemente in der Vollansicht bedienbar | Keine | Wiedergabe-Ansicht |
| 4 | Videoplayer: wiederholte Wiedergabe ohne Absturz | Keine | Wiedergabe-Ansicht, Wiedergabe-Steuerung |
| 5 | Startseite: Hero-Kopfbereich mit ausblendbarer Einstellung | 1 | Startseite, Einstellungen, Server-Bildbezug |
| 6 | Detailseiten: Vollbreiten-Header mit Hintergrundgrafik | 1 | Detailansichten, Server-Bildbezug |
| 7 | Episodenlisten: Angesehen-Symbol bei gesehenen Episoden | 6 | Episodenlisten, Datenübernahme vom Server |

## Offene Punkte

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
| 1 | **Hero-Einstellung: app-lokal oder mit der Webanwendung synchronisiert?** In der Webanwendung existiert die Einstellung zum Ein-/Ausblenden des Kopfbereichs bereits (Standard: an); ob sie serverseitig gehalten wird, ist unklar. | **Behandelt — Vorschlag übernommen:** App-lokale Einstellung, wie alle übrigen App-Einstellungen auch. Begründung: Die Anforderung selbst nennt diese Annahme; die App hat keinen Mechanismus zur Synchronisation von Einstellungen mit dem Server, und ein serverseitiger Abgleich wäre ein Funktionsausbau über die Rückmeldung hinaus. Bei späterem Klärungsbedarf kann synchronisiert nachgeliefert werden. |
| 2 | **Tolerierte Design-Abweichung bei der Navigation:** Die Webanwendung nutzt eine Sidebar mit Quelleneinträgen; die App nutzt die MAUI-Shell-Navigation (Flyout). Soll die Navigation zusätzlich an das Web-Layout angeglichen werden? | **Behandelt — Vorschlag übernommen:** Die Shell-Navigation bleibt strukturell bestehen und wird nur farblich an die Web-Tokens angeglichen (Teil von Schritt 1). Begründung: Der Kunde beanstandet Farben und Rundungen, nicht die Navigationsform; ein Sidebar-Umbau wäre ein Redesign über die Rückmeldung hinaus (Nicht-Ziel) und auf den Mobilzielen (iOS) unüblich. |
