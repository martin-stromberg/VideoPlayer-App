# Projektplan: Neugestaltung der App „VideoPlayer" (Issue #9)

## Übersicht

Es wird eine neue .NET-MAUI-App als Frontend zur bestehenden Serveranwendung „VideoPlayer"
entwickelt; die bisherige Implementierung wird vollständig neu aufgebaut — einschließlich
des clientseitigen Zugriffs auf die öffentliche Server-API, der nicht aus dem Bestandscode
übernommen wird. Die Architektur folgt dem MVU-Muster auf Basis eines eigenen schlanken
MVU-Zyklus; UI und Bedienung orientieren sich an der bestehenden Webanwendung.
Zielplattformen sind iOS und Windows (Android-erweiterbar). Betroffene Bereiche:
API-Anbindung (Neuimplementierung), App-Grundgerüst und Navigation, Einstellungs- und
Datenhaltung inkl. Backup-Trennung, Multi-Server-Verwaltung, Verbindungs-/Start-Workflow,
Netzwerkstatus und Synchronisation, Startseite und Medienbrowsing, Wiedergabe mit
Picture-in-Picture, Downloads sowie Offlinebetrieb.

## Grobe Vorgehensentscheidungen

| Bereich | Entscheidung | Begründung |
|---------|-------------|------------|
| Bestandscode | Alles wird neu entwickelt — auch das bisherige API-Client-Projekt `VideoWebPlayer.Client` wird neu geschrieben (DTOs, Endpunkte, Authentifizierung). Der vorhandene Code dient nur als fachliche Referenz (welche Endpunkte existieren, welche DTO-Felder), wird aber nicht wiederverwendet | Verbindliche Stakeholder-Entscheidung; konsequenter Neubau ohne Übernahme von Altcode. |
| Architektur | MVU (Model–View–Update) über einen eigenen schlanken MVU-Zyklus in C#; kein externes Framework (kein `Fabulous`) | Verbindliche Stakeholder-Entscheidung; keine zusätzliche externe Abhängigkeit. |
| Design | Es wird kein Google-Stitch-Entwurf abgewartet; die UI richtet sich an der bestehenden Webanwendung aus. Ein späterer Stitch-Entwurf wird ggf. als Nacharbeit eingeplant | Der Entwurf liegt nicht vor und ist nicht mehr Umsetzungsvoraussetzung; die Webanwendung ist die verbindliche Gestaltungsreferenz. |
| Reihenfolge | Fundament zuerst (clientseitiger API-Zugriff, Gerüst, Einstellungen/Datenhaltung, Serververwaltung, Verbindung, Synchronisation), danach Oberflächen-Features (Startseite, Browsing, Wiedergabe), Downloads und Offlinebetrieb zuletzt | Jeder Schritt ist eigenständig liefer- und testbar; spätere Schritte bauen auf stabilen Grundlagen auf. |
| Serverbezug | Es wird ausschließlich die öffentliche Server-API genutzt; fehlende API-Möglichkeiten werden als Issues im Server-Repository gemeldet statt Workarounds in der App zu bauen | Explizite Anforderung und Nicht-Ziel. |
| Testbarkeit | Jeder Entwicklungsschritt liefert automatisierte Tests und ist lokal unter Windows verifizierbar; iOS-Nachweise erfolgen über die CI-Umgebung (macOS) | Die lokale Umgebung kann iOS-/MacCatalyst-Builds nicht ausführen; die CI nutzt bereits `macos-latest`. |
| Servererkennung | Das bestehende Discovery-Protokoll wird beibehalten (mDNS-Dienstsuche sowie UDP-Broadcast auf Port 5001 mit bekanntem Abfrage-/Antwortschema); die serverseitige Antwort ist zu verifizieren, fehlt sie, wird ein Issue im Server-Repository gestellt | Verbindliche Stakeholder-Entscheidung; konsistent mit dem Grundsatz „keine Workarounds". |
| Backup | Backup-Strategie primär für iOS; auf Windows wird die Ablagetrennung übernommen und ein Roaming-Äquivalent geprüft. Geheimnisse (Zugangsdaten, Anmelde-Token) gehen nicht ins Cloud-Backup; nach Wiederherstellung ist ggf. eine erneute Anmeldung erforderlich | Verbindliche Stakeholder-Entscheidung; plattformabhängige Restriktionen der sicheren Ablage. |
| Favoriten | Favoriten werden primär serverseitig geführt; die lokale Kopie ist ein nicht backup-pflichtiger Cache für den Offlinebetrieb | Verbindliche Stakeholder-Entscheidung; die Server-API deckt Favoriten ab. |
| Picture-in-Picture | Auf Windows ist PiP als kompaktes Overlay-/Mini-Player-Fenster ausgeprägt; die Machbarkeit mit der MediaElement-Komponente ist frühzeitig zu prüfen | Verbindliche Stakeholder-Entscheidung; PiP ist Pflichtanforderung auf beiden Zielplattformen. |
| Fehlerbehandlung | Retry-Strategie für fehlerhafte Serverantworten: drei Versuche mit exponentiellem Backoff (1 s, 2 s, 4 s); kein Retry bei eindeutigen Clientfehlern außer 401 (Token-Refresh), 408 und 429 | Verbindliche Stakeholder-Entscheidung; wird im clientseitigen API-Zugriff umgesetzt. |

## Entwicklungsschritte

### Schritt 1: Clientseitiger Zugriff auf die Server-API (Neuimplementierung)

**Beschreibung:** Im Rahmen der Neuentwicklung der App „VideoPlayer" — einer neuen
.NET-MAUI-App als Frontend zur bestehenden Serveranwendung „VideoPlayer", die
ausschließlich über die öffentliche Server-API kommuniziert — ist ein plattformneutraler,
clientseitiger Zugriff auf die Server-API neu zu implementieren. Der bisherige
API-Client-Code wird nicht übernommen; er dient lediglich als fachliche Referenz dafür,
welche Endpunkte existieren und welche Felder die übertragenen Daten umfassen. Der neue
Zugriff deckt die vom Serverprojekt angebotenen Endpunkte ab, darunter Anmeldung,
Erreichbarkeitsprüfung, Quellenübersicht, zuletzt hinzugefügte Inhalte („Neu im Programm"
über den Endpunkt für neue Inhalte), Weiterschauen inklusive Fortschrittsmeldung sowie
Ausblenden/Überspringen, Favoriten (Abruf, Markieren, Entfernen), Mediennachschlagen
(Filme, Serien, Staffeln, Episoden, Filmkollektionen, Genres) und Bildabrufe. Zu
berücksichtigen sind die Zugangskontrolle des Servers für App-Clients (Client-Gate per
API-Schlüssel, konfigurierbar über Build-Eigenschaft bzw. Umgebungsvariable) und die
tokenbasierte Anmeldung. Für fehlerhafte Serverantworten gilt eine feste
Wiederholungsstrategie: bis zu drei Versuche mit exponentiell wachsenden
Warteintervallen (1 s, 2 s, 4 s); keine Wiederholung bei eindeutigen Clientfehlern,
ausgenommen „nicht autorisiert" (führt zu einer einmaligen Erneuerung der Anmeldung per
Token-Refresh), „Zeitüberschreitung der Anfrage" und „zu viele Anfragen".
Akzeptanzkriterien: Der neu implementierte Zugriff deckt die genannten Endpunkte ab und
ist automatisiert testbar (z. B. gegen die API des lokal im IIS eingerichteten
Webprojekts „VideoPlayer" oder einen simulierten Server); die Wiederholungsstrategie
greift exakt wie definiert; die Anmeldung inklusive tokenbasierter Folgeanfragen und
Erneuerung bei „nicht autorisiert" funktioniert; kein Code des bisherigen Clients wird
wiederverwendet.

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** API-Anbindung, Authentifizierung, Fehlerbehandlung

### Schritt 2: App-Grundgerüst mit MVU-Struktur und Navigation

**Beschreibung:** Im Rahmen der Neuentwicklung der App „VideoPlayer" — einer neuen
.NET-MAUI-App als Frontend zur bestehenden Serveranwendung „VideoPlayer", die
ausschließlich über die öffentliche Server-API kommuniziert — ist das Grundgerüst der
App zu erstellen. Die bisherige App-Implementierung wird nicht übernommen; die
Architektur folgt dem MVU-Muster (Model–View–Update) auf Basis eines eigenen schlanken
MVU-Zyklus ohne externes Framework. Zielplattformen sind iOS und Windows; die Struktur
soll eine spätere Erweiterung um Android erlauben. Die App erhält eine Hauptnavigation
per Hamburgermenü mit den Einträgen „Startseite", „Quellen", „Favoriten", „Downloads",
„Profil" und „Einstellungen"; die zugehörigen Seiten werden zunächst als funktionslose
Grundgerüste angelegt. Zusätzlich gibt es eine Ladeanzeige für den App-Start. Die
Anbindung an die Server-API erfolgt über den neu implementierten plattformneutralen
clientseitigen API-Zugriff. Akzeptanzkriterien: Die App baut und startet unter Windows;
der iOS-Build ist über die CI-Umgebung nachweisbar; das Hamburgermenü enthält alle
genannten Einträge und führt zu den jeweiligen Seiten; die Struktur folgt MVU ohne
Übernahme des bisherigen MVVM-Codes und ohne externes MVU-Framework; die App kann die
API des lokal im IIS eingerichteten Webprojekts „VideoPlayer" grundsätzlich aufrufen
(z. B. Erreichbarkeitsprüfung).

**Abhängigkeiten:** 1

**Betroffene Bereiche:** App-Architektur, Navigation, API-Anbindung

### Schritt 3: Einstellungsverwaltung und lokale Datenhaltung mit Backup-Trennung

**Beschreibung:** Die neu entwickelte .NET-MAUI-App „VideoPlayer" (MVU-Architektur auf
Basis eines eigenen MVU-Zyklus, Zielplattformen iOS und Windows, Anbindung an die
Serveranwendung „VideoPlayer" über deren öffentliche API mittels neu implementiertem
clientseitigem Zugriff) benötigt eine persistente Einstellungsverwaltung und eine klar
strukturierte lokale Datenablage. Folgende benutzerbezogene Einstellungen sind zu
speichern und über die Einstellungsseite pflegbar: automatische Downloads an/aus, Anzahl
vorzuladender Episoden, Auto-Fortsetzung an/aus, Zeitfenster für Auto-Fortsetzung
(benutzerseitige Einstellung; die Auto-Fortsetzung greift später bei einer Restlaufzeit
unterhalb dieses Fensters), WLAN-only-Downloads an/aus, Zeitfenster „als angesehen" am
Videoende (benutzerseitige Einstellung; ein Video gilt als angesehen, wenn das Ende
abzüglich dieses Fensters erreicht ist), Speicherplatzkontingent für Downloads
(benutzerkonfigurierbar in Gigabyte, Standardwert 5 GB) sowie optionale automatische
Löschung gesehener Episoden. Geheime Daten (Zugangsdaten, Anmelde-Token) sind in der
sicheren Ablage der Plattform zu halten, nicht-geheime Einstellungen in der normalen
Einstellungsablage; eine Profil-Isolation über eine Umgebungsvariable ist vorzusehen.
Die lokale Datenablage (lokale Datenbank und Dateiablagen) ist so zu strukturieren,
dass backup-pflichtige Daten (Serverliste, zuletzt verbundener Server, Profildaten ohne
Geheimnisse, App-Einstellungen, unsynchronisierte Fortschritte) von nicht
backup-pflichtigen Daten (Offline-Videos, Vorschaubilder, Startseiten-Metadaten,
Episodenlisten, Download-Manifest, Temp-Cache sowie die lokale Kopie der serverseitig
geführten Favoriten) getrennt abgelegt werden. Geheimnisse gehören nicht ins
Cloud-Backup; nach einer Wiederherstellung ist daher ggf. eine erneute Anmeldung
erforderlich. Die Backup-Strategie wird primär für iOS umgesetzt (Ausschluss nicht
backup-pflichtiger Ablagen über den plattformüblichen Backup-Ausschluss bzw. eine
Cache-Ablage); auf Windows wird die Ablagetrennung übernommen und ein geeignetes
Roaming-Äquivalent geprüft und — sofern vorhanden — genutzt. Akzeptanzkriterien: Alle
genannten Einstellungen bleiben über App-Neustarts erhalten und sind über die
Einstellungsseite änderbar; das Speicherplatzkontingent ist in Gigabyte pflegbar und
mit 5 GB vorbelegt; die Backup-Klassifikation der Ablageorte ist umgesetzt und
nachweisbar (z. B. durch Tests oder Plattformattribute); Geheimnisse liegen weder in
der normalen Einstellungsablage noch gehen sie ins Cloud-Backup; unterschiedliche
Profile beeinflussen sich nicht gegenseitig.

**Abhängigkeiten:** 2

**Betroffene Bereiche:** Einstellungen, lokale Datenhaltung, Backup

### Schritt 4: Serververwaltung und Erststart-Einrichtung

**Beschreibung:** Die neu entwickelte .NET-MAUI-App „VideoPlayer" (MVU-Architektur auf
Basis eines eigenen MVU-Zyklus, Zielplattformen iOS und Windows, Anbindung an die
Serveranwendung „VideoPlayer" über deren öffentliche API mittels neu implementiertem
clientseitigem Zugriff, mit persistenter Einstellungsverwaltung) soll mehrere Server
verwalten können. Es ist eine Liste bekannter Server zu speichern; der zuletzt
verbundene Server wird persistiert und beim App-Start automatisch verwendet. Beim
Erststart — wenn noch kein Server hinterlegt ist — erscheint ein Einrichtungsdialog mit
zwei Wegen: manuelle Eingabe einer Serveradresse und automatische Erkennung von Servern
im lokalen Netzwerk über das bestehende Discovery-Protokoll (mDNS-Dienstsuche sowie
UDP-Broadcast auf Port 5001 mit bekanntem Abfrage-/Antwortschema). Die serverseitige
Beantwortung der Erkennung ist zu verifizieren; wird sie nicht beantwortet, ist ein
Issue im Server-Repository zu stellen statt einen Workaround in der App zu bauen.
Automatisch gefundene Server können aus einer Auswahl übernommen werden; eine
erfolgreich geprüfte Verbindung wird gespeichert. In den Einstellungen gibt es einen
bewusst unauffälligen Eintrag „Server wechseln", der die bekannten Server anzeigt und
das Hinzufügen neuer Server erlaubt. Akzeptanzkriterien: Erststart ohne hinterlegten
Server führt zum Einrichtungsdialog; automatisch gefundene Server werden zur Auswahl
angeboten; nach erfolgreicher Einrichtung startet die App künftig direkt mit dem
gespeicherten Server; „Server wechseln" listet bekannte Server, erlaubt das Hinzufügen
und den Wechsel ohne Verlust der übrigen Einstellungen.

**Abhängigkeiten:** 2, 3

**Betroffene Bereiche:** Serververwaltung, Erststart, Einstellungen

### Schritt 5: Verbindungs-Workflow und Anmeldung

**Beschreibung:** Die neu entwickelte .NET-MAUI-App „VideoPlayer" (MVU-Architektur auf
Basis eines eigenen MVU-Zyklus, Zielplattformen iOS und Windows, Anbindung an die
Serveranwendung „VideoPlayer" über deren öffentliche API mittels neu implementiertem
clientseitigem Zugriff, mit Verwaltung mehrerer Server und automatischer Nutzung des
zuletzt verbundenen Servers) erhält einen durchgängigen Verbindungs- und
Anmeldeworkflow. Beim App-Start zeigt die App eine Ladeanzeige und versucht
automatisch, den zuletzt verbundenen Server zu erreichen (Erreichbarkeitsprüfung). Bei
Erfolg erfolgt die Anmeldung mit den sicher gespeicherten Zugangsdaten; fehlen diese
oder schlägt die Anmeldung fehl, wird eine Anmeldemaske gezeigt. Ist kein Server
hinterlegt, wird die Servereinrichtung aufgerufen; ist der Server nicht erreichbar,
wechselt die App in den Offline-Zustand bzw. — falls keine Offline-Daten vorhanden
sind — zur Serverauswahl. Die Verbindungszustände (verbunden, Anmeldung erforderlich,
Servereinrichtung erforderlich, offline) sind der App durchgehend bekannt und steuern
die Navigation. Fehlerhafte Serverantworten werden vom clientseitigen API-Zugriff nach
der festgelegten Wiederholungsstrategie behandelt (drei Versuche mit exponentiellem
Backoff von 1 s, 2 s und 4 s; keine Wiederholung bei eindeutigen Clientfehlern außer
„nicht autorisiert", „Zeitüberschreitung der Anfrage" und „zu viele Anfragen").
Abgelaufene Anmelde-Token werden automatisch erneuert, indem mit den gespeicherten
Zugangsdaten eine stille Neuanmeldung erfolgt. Da Geheimnisse nicht ins Cloud-Backup
eingehen, verlangt der Workflow nach einer Wiederherstellung ggf. eine erneute
Anmeldung, während die Verbindung zum zuletzt genutzten Server automatisch
wiederhergestellt wird. Akzeptanzkriterien: Der beschriebene Start-Workflow funktioniert
in allen vier Zustandsausgängen (verbunden, Anmeldung, Servereinrichtung, offline);
fehlerhafte Serverantworten werden nach der definierten Strategie wiederholt statt
sofort zu scheitern; nach Ablauf des Tokens läuft die App ohne erneute manuelle
Anmeldung weiter; nach Wiederherstellung der App-Daten aus einem Backup verbindet sich
die App automatisch mit dem zuletzt genutzten Server und fordert nur bei fehlenden
Geheimnissen zur erneuten Anmeldung auf.

**Abhängigkeiten:** 4

**Betroffene Bereiche:** Verbindung, Authentifizierung, Fehlerbehandlung

### Schritt 6: Netzwerkstatus-Erkennung und Synchronisation

**Beschreibung:** Die neu entwickelte .NET-MAUI-App „VideoPlayer" (MVU-Architektur auf
Basis eines eigenen MVU-Zyklus, Zielplattformen iOS und Windows, Anbindung an die
Serveranwendung „VideoPlayer" über deren öffentliche API mittels neu implementiertem
clientseitigem Zugriff, mit automatischem Verbindungs- und Anmeldeworkflow) erkennt
Wechsel zwischen Online- und Offline-Zustand. Bei Rückkehr ins Netz wird die Verbindung
zum Server automatisch wiederhergestellt — einschließlich der Echtzeit-Verbindung für
Server-Ereignisse (Aktualisierung der Weiterschauen-Liste, Änderungen an Favoriten, neu
eingescannte Inhalte) — und eine Synchronisation ausgeführt. Diese umfasst einen
vollständigen Metadaten-Sync, das Nachsenden lokal gesammelter, noch nicht übermittelter
Fortschrittsmeldungen sowie die Aktualisierung der Weiterschauen-Liste. Für
unsynchronisierte Fortschrittsmeldungen ist eine persistente lokale Warteschlange
vorzusehen, die Meldungen über App-Neustarts hinweg aufbewahrt und bei Online-Rückkehr
in sinnvoller Reihenfolge an den Server nachsendet. Akzeptanzkriterien: Trennen und
Wiederherstellen der Netzwerkverbindung wird zuverlässig erkannt; nach Online-Rückkehr
laufen Wiederverbindung und Synchronisation automatisch ab; zuvor offline erfasste
Fortschritte erreichen den Server ohne Verlust; vom Server empfangene Ereignisse
aktualisieren die betroffenen Anzeigen ohne manuelle Aktualisierung.

**Abhängigkeiten:** 5

**Betroffene Bereiche:** Netzwerk, Synchronisation, Server-Ereignisse

### Schritt 7: Startseite mit gecachten Medienlisten

**Beschreibung:** Die neu entwickelte .NET-MAUI-App „VideoPlayer" (MVU-Architektur auf
Basis eines eigenen MVU-Zyklus, Zielplattformen iOS und Windows, Anbindung an die
Serveranwendung „VideoPlayer" über deren öffentliche API mittels neu implementiertem
clientseitigem Zugriff, mit automatischem Verbindungsworkflow und Synchronisation)
erhält eine Startseite mit den horizontalen Listen „Weiterschauen", „Favoriten" und
„Neu im Programm" (letztere über den Server-Endpunkt für zuletzt hinzugefügte Inhalte).
Alle Listen werden lokal gecacht, sodass die Startseite auch ohne sofortige
Serverantwort Inhalte anzeigt; sobald Serverdaten verfügbar sind, werden die Listen
aktualisiert. Vom Server empfangene Ereignisse (aktualisierte Weiterschauen-Liste,
geänderte Favoriten, neue Inhalte) aktualisieren die betroffenen Listen automatisch.
Aussehen und Bedienung richten sich an der bestehenden Webanwendung aus.
Akzeptanzkriterien: Alle drei Listen werden mit den korrekten Inhalten des Servers
angezeigt; bei erneutem Öffnen erscheinen zunächst die gecachten Inhalte;
Server-Ereignisse aktualisieren die Listen ohne manuelle Aktualisierung; die Auswahl
eines Eintrags führt zum jeweiligen Medium bzw. dessen Detailseite.

**Abhängigkeiten:** 6

**Betroffene Bereiche:** Startseite, Caching, Medienlisten

### Schritt 8: Quellen-Browsing und Medien-Detailseiten

**Beschreibung:** Die neu entwickelte .NET-MAUI-App „VideoPlayer" (MVU-Architektur auf
Basis eines eigenen MVU-Zyklus, Zielplattformen iOS und Windows, Anbindung an die
Serveranwendung „VideoPlayer" über deren öffentliche API mittels neu implementiertem
clientseitigem Zugriff, mit automatischem Verbindungs- und Anmeldeworkflow) erhält über
den Menüpunkt „Quellen" eine Übersicht der vom Server freigegebenen Medienbereiche. Pro
Quelle können die enthaltenen Medien durchblättert werden — mit Genre-Filterung,
Textsuche und seitenweisem Nachladen. Für Serien gibt es eine Detailseite mit Staffel-
und Episodenauswahl; für Filmkollektionen eine Detailseite mit den enthaltenen Filmen.
Medien können als Favorit markiert und die Markierung wieder entfernt werden; die
Favoriten werden primär serverseitig geführt, wobei eine lokale Kopie als nicht
backup-pflichtiger Cache für den Offlinebetrieb vorgehalten wird. Aussehen und
Bedienung richten sich an der bestehenden Webanwendung aus. Akzeptanzkriterien: Die
Quellenliste entspricht den Daten der Server-API; Genre-Filter, Suche und Paging
funktionieren; Detailseiten zeigen Staffeln/Episoden bzw. enthaltene Filme korrekt an;
Favoriten-Markierungen werden zum Server übertragen und sind nach Neustart der App
konsistent.

**Abhängigkeiten:** 5

**Betroffene Bereiche:** Quellen, Medienbrowsing, Detailseiten, Favoriten

### Schritt 9: Favoriten- und Profil-Seite

**Beschreibung:** Die neu entwickelte .NET-MAUI-App „VideoPlayer" (MVU-Architektur auf
Basis eines eigenen MVU-Zyklus, Zielplattformen iOS und Windows, Anbindung an die
Serveranwendung „VideoPlayer" über deren öffentliche API mittels neu implementiertem
clientseitigem Zugriff, mit automatischem Verbindungs- und Anmeldeworkflow) erhält zwei
weitere Menüseiten. Der Menüpunkt „Favoriten" führt zu einer eigenen Seite, die alle
favorisierten Medien anzeigt und das Entfernen von Favoriten erlaubt; die Favoriten
werden primär serverseitig geführt und zusätzlich als nicht backup-pflichtige lokale
Kopie vorgehalten, damit sie ohne Serververbindung verfügbar sind. Der Menüpunkt
„Profil" führt zu einer Profil-Seite, die den angemeldeten Benutzer und das aktive
lokale Profil anzeigt und eine Abmeldemöglichkeit bietet. Akzeptanzkriterien: Die
Favoriten-Seite zeigt alle Favoriten und erlaubt das Entfernen; die Profil-Seite zeigt
angemeldeten Benutzer und aktives lokales Profil und bietet eine Abmeldung; beide
Seiten orientieren sich in Aussehen und Bedienung an der bestehenden Webanwendung.

**Abhängigkeiten:** 5

**Betroffene Bereiche:** Favoriten, Profil

### Schritt 10: Videowiedergabe mit Picture-in-Picture und Fortschrittsverfolgung

**Beschreibung:** Die neu entwickelte .NET-MAUI-App „VideoPlayer" (MVU-Architektur auf
Basis eines eigenen MVU-Zyklus, Zielplattformen iOS und Windows, Anbindung an die
Serveranwendung „VideoPlayer" über deren öffentliche API mittels neu implementiertem
clientseitigem Zugriff, mit Startseiten- und Detailseiten zur Medienauswahl sowie einer
persistenten Warteschlange für unsynchronisierte Fortschrittsmeldungen) erhält die
Videowiedergabe. Diese erfolgt über die MediaElement-Komponente des .NET MAUI
CommunityToolkit mit verpflichtender Picture-in-Picture-Unterstützung auf iOS und
Windows; auf Windows ist PiP als kompaktes Overlay-/Mini-Player-Fenster ausgeprägt, und
die Machbarkeit der PiP-Umsetzung mit der MediaElement-Komponente ist frühzeitig in
diesem Schritt zu verifizieren. Abgespielt werden können Server-Streams und lokale
Dateien; beim Start wird eine vorhandene Fortsetzungsposition berücksichtigt. Während
der Wiedergabe wird der Fortschritt an den Server gemeldet; ist die App offline, werden
die Meldungen in der lokalen Warteschlange gesammelt und bei Online-Rückkehr
synchronisiert. Am Videoende gilt das Medium als „angesehen", sobald das Ende abzüglich
des konfigurierten benutzerseitigen Zeitfensters erreicht ist. Eine Auto-Fortsetzung
ist per benutzerseitigem Schalter aktivierbar und greift, wenn die Restlaufzeit des
Mediums das konfigurierte Zeitfenster unterschreitet. Ist eine Videoquelle nicht
verfügbar, erscheint eine Fehlermeldung und die App fällt auf die nächste Episode
zurück. Akzeptanzkriterien: Picture-in-Picture funktioniert auf beiden Zielplattformen
(auf Windows als kompaktes Overlay-/Mini-Player-Fenster); Fortschritte werden online
sofort und offline verzögert verlustfrei zum Server übertragen; „als angesehen" und
Auto-Fortsetzung respektieren die konfigurierten Zeitfenster und Schalter wie
beschrieben; eine nicht verfügbare Quelle führt zu Fehlermeldung und Rückfall auf die
nächste Episode.

**Abhängigkeiten:** 6, 7, 8

**Betroffene Bereiche:** Wiedergabe, Picture-in-Picture, Fortschrittsmeldung

### Schritt 11: Download-Verwaltung und automatische Downloads

**Beschreibung:** Die neu entwickelte .NET-MAUI-App „VideoPlayer" (MVU-Architektur auf
Basis eines eigenen MVU-Zyklus, Zielplattformen iOS und Windows, Anbindung an die
Serveranwendung „VideoPlayer" über deren öffentliche API mittels neu implementiertem
clientseitigem Zugriff, mit Videowiedergabe, Netzwerkstatus-Erkennung und
konfigurierbaren Download-Einstellungen) erhält eine vollwertige Download-Verwaltung
für den Offlinebetrieb. Medien können heruntergeladen werden; die Downloads-Seite
listet alle Downloads mit Status und Fortschritt und erlaubt deren Verwaltung und
Löschung. Die Verwaltung unterstützt parallele Downloads, die automatische
Wiederaufnahme abgebrochener Downloads (auch über App-Neustarts hinweg) und eine
definierte Fehlerbehandlung. Das benutzerkonfigurierbare Speicherplatzkontingent
(Angabe in Gigabyte, Standardwert 5 GB) begrenzt den belegten Speicherplatz; optional
können als gesehen markierte Episoden automatisch gelöscht werden. Automatische
Downloads (Vorladen von Episoden, insbesondere der Weiterschauen-Liste) sind per
Einstellung aktivierbar, mit konfigurierbarer Anzahl und optionaler Beschränkung auf
WLAN-Verbindungen. Zusätzlich wird ein Download-Manifest geführt (nicht
backup-pflichtig), aus dem der Download-Bestand rekonstruierbar ist. Heruntergeladene
Videos sind über die vorhandene Wiedergabe abspielbar. Akzeptanzkriterien: Mehrere
Downloads laufen parallel; abgebrochene Downloads werden automatisch fortgesetzt; das
Speicherplatzkontingent wird eingehalten; automatische Downloads respektieren Anzahl-
und WLAN-only-Einstellungen; die Auto-Löschung gesehener Episoden funktioniert bei
Aktivierung; die Downloads-Seite zeigt Status und Fortschritt aller Downloads.

**Abhängigkeiten:** 6, 10

**Betroffene Bereiche:** Downloads, Offline-Daten, Speicherverwaltung

### Schritt 12: Offlinebetrieb und Wiederherstellung

**Beschreibung:** Die neu entwickelte .NET-MAUI-App „VideoPlayer" (MVU-Architektur auf
Basis eines eigenen MVU-Zyklus, Zielplattformen iOS und Windows, Anbindung an die
Serveranwendung „VideoPlayer" über deren öffentliche API mittels neu implementiertem
clientseitigem Zugriff, mit gecachter Startseite, lokaler Favoriten-Verfügbarkeit,
Download-Verwaltung und Netzwerkstatus-Erkennung) erhält den vollständigen
Offlinebetrieb. Ohne Serververbindung wird die Startseite aus dem Cache befüllt — die
Liste „Weiterschauen" wird ausschließlich auf Basis der lokalen Downloads gebildet,
fehlende Episoden werden als Platzhalter dargestellt —, heruntergeladene Videos sind
abspielbar und Favoriten sind über die lokale Kopie verfügbar. Eingeschränkt sind:
keine neuen Inhalte, keine Quellenliste, kein Profilabruf. Nach der Wiederherstellung
der App aus einem Cloud-Backup verbindet sich die App automatisch mit dem zuletzt
genutzten Server; da Geheimnisse nicht gesichert werden, ist ggf. eine erneute
Anmeldung erforderlich; bei Nichterreichbarkeit wechselt die App in den Offline-Modus
(sofern Daten vorhanden) oder zur Serverauswahl; anschließend laufen vollständiger
Metadaten-Sync, Nachsenden unsynchronisierter Fortschritte und Aktualisierung der
Weiterschauen-Liste ab. Bei beschädigten lokalen Daten gilt ein definiertes Verhalten:
betroffene Caches werden verworfen und neu synchronisiert; eine beschädigte lokale
Datenbank wird neu initialisiert und der Nutzer wird informiert. Akzeptanzkriterien:
Alle genannten Offline-Fähigkeiten und -Einschränkungen greifen; der beschriebene
Wiederherstellungsablauf funktioniert inklusive erneuter Anmeldung bei fehlenden
Geheimnissen; beschädigte lokale Daten führen nicht zum Absturz, sondern zum
definierten Verhalten (Cache-Neuaufbau bzw. Datenbank-Neuinitialisierung mit
Nutzerinformation).

**Abhängigkeiten:** 7, 9, 11

**Betroffene Bereiche:** Offlinebetrieb, Wiederherstellung, Fehlerbehandlung

## Übersichtstabelle

| # | Titel | Abhängigkeiten | Betroffene Bereiche |
|---|-------|-----------------|----------------------|
| 1 | Clientseitiger Zugriff auf die Server-API (Neuimplementierung) | Keine | API-Anbindung, Authentifizierung, Fehlerbehandlung |
| 2 | App-Grundgerüst mit MVU-Struktur und Navigation | 1 | App-Architektur, Navigation, API-Anbindung |
| 3 | Einstellungsverwaltung und lokale Datenhaltung mit Backup-Trennung | 2 | Einstellungen, lokale Datenhaltung, Backup |
| 4 | Serververwaltung und Erststart-Einrichtung | 2, 3 | Serververwaltung, Erststart, Einstellungen |
| 5 | Verbindungs-Workflow und Anmeldung | 4 | Verbindung, Authentifizierung, Fehlerbehandlung |
| 6 | Netzwerkstatus-Erkennung und Synchronisation | 5 | Netzwerk, Synchronisation, Server-Ereignisse |
| 7 | Startseite mit gecachten Medienlisten | 6 | Startseite, Caching, Medienlisten |
| 8 | Quellen-Browsing und Medien-Detailseiten | 5 | Quellen, Medienbrowsing, Detailseiten, Favoriten |
| 9 | Favoriten- und Profil-Seite | 5 | Favoriten, Profil |
| 10 | Videowiedergabe mit Picture-in-Picture und Fortschrittsverfolgung | 6, 7, 8 | Wiedergabe, Picture-in-Picture, Fortschrittsmeldung |
| 11 | Download-Verwaltung und automatische Downloads | 6, 10 | Downloads, Offline-Daten, Speicherverwaltung |
| 12 | Offlinebetrieb und Wiederherstellung | 7, 9, 11 | Offlinebetrieb, Wiederherstellung, Fehlerbehandlung |

## Offene Punkte

Keine.
