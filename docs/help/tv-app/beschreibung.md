← [Zurück zur Übersicht](index.md)

# Apple-TV-App (tvOS) — Beschreibung

## Zweck

Die Apple-TV-App ist die eigenständige tvOS-Ausgabe der Heimkino-Anwendung.
Sie soll persönliche Medienbibliotheken eines selbst gehosteten
VideoPlayer-Servers auf dem Fernseher verfügbar machen — als Lean-back-Player,
der sich per Siri Remote bedienen lässt.

## Funktionsweise

Die App läuft auf demselben plattformneutralen MVU-Kern wie die
Hauptanwendung (`VideoPlayer.Mvu`): ein `MvuHost` treibt den
Modell-Update-Kreislauf mit serialisiertem Dispatch; ein tvOS-seitiger
`TvosAppEffectHandler` führt die Effekte des Start- und
Verbindungs-Workflows am App-Rand aus (Ablage-Initialisierung, Einstellungen,
Serververwaltung, Erreichbarkeitsprüfung, Pairing-/Bootstrap-Austausch,
Anmeldung, Netzwerk-Monitoring, Server-Ereignisse, Synchronisation,
Cache-First-Startseiten-Listen, Navigation), den lesenden
Playlist-Zugriff (Übersicht, Kopfdaten, paginierte Einträge,
Wiedergabekontext) sowie die manuellen Downloads (Einreihen,
Abbrechen, Wiederholen, Löschen, Wiederaufnahme,
Ablauf-/Abgleichslauf). Nicht für tvOS vorgesehene
Effekte (Auto-Download-Vorlauf, Inhalts-/Playlist-Verwaltung,
Protokollversand) laufen protokolliert ins Leere — es gibt keinerlei
Schreibzugriff auf verwaltete Inhalte.

Die Oberfläche ist ein `UITabBarController`-Seitenrahmen mit den
Menübereichen Startseite, Favoriten, Playlists, Downloads, Profil und
Einstellungen — vollständig über die tvOS-Focus-Engine per Siri Remote
bedienbar. Der Navigator bildet den Modellzustand ab: Ladeanzeige während
des Start-Workflows, die Verbindungs-Workflow-Seiten
(`ServerSetupViewController`, `LoginViewController`) oder den Tab-Rahmen.

Die Serververbindung ist vollständig fernbedienungstauglich umgesetzt:
automatische Servererkennung im lokalen Netz (mDNS + UDP) mit
Auswahlliste, Liste bekannter Server inklusive Serverwechsel, manuelle
Adresseingabe, Geräte-Kopplung per Einmal-Code
(`api/pairing/exchange`) oder Ticket-/Kurzcode
(`api/pairing/bootstrap`), Anmeldung mit Benutzername/Passwort sowie
automatische Wiederanmeldung mit gespeicherten Zugangsdaten aus dem
Schlüsselbund. Alle Texteingaben nutzen `UITextField` — die
tvOS-Systemtastatur unterstützt Diktat per Siri Remote und die
Continuity-Tastatur vom iPhone; ein QR-Code-Scan existiert auf tvOS
nicht (keine Kamera). Fehlerzustände (ungültiger/abgelaufener Code,
Versuchssperre, nicht erreichbarer Server, abgelehnte Anmeldung,
verworfenes Geräte-Token) erscheinen als Inline-Texte und erlauben die
Wiederholung. „Server wechseln" ist über die Einstellungsseite,
„Abmelden" über die Profilseite erreichbar; bei bestehender Verbindung
führt die Menü-Taste der Siri Remote aus der Einrichtung zurück ins
Menü.

Die Startseite ist eine Lean-back-Ansicht mit vertikal gestapelten,
horizontalen Karussell-Reihen (`UICollectionView` mit
Fokus-Hervorhebung): „Weiterschauen" mit Fortschrittsbalken (und
Platzhalter-Kennzeichnung nicht abspielbarer Offline-Einträge),
„Favoriten", „Neu im Programm" sowie die saisonalen Genre-Sektionen
des Servers (mit Ablauf-Timer, der beim Erreichen der
Ablaufgrenze nachlädt). Die Listen folgen dem Cache-First-Prinzip der
App: gespeicherte Daten erscheinen sofort, Aktualisierung läuft über
Synchronisation im Hintergrund, Server-Ereignisse (SignalR) und bei
jeder Rückkehr in den Vordergrund. Poster werden über
`api/pictures/{id}` vom Server nachgeladen und dreistufig
zwischengespeichert (Arbeitsspeicher, Datei im Cache-Verzeichnis,
Serverabruf); fehlende Bilder erhalten einen Titel-Platzhalter. Die
Auswahl eines Eintrags öffnet die Detailansicht des Mediums. Im
Offline-Zustand zeigt die Startseite die aus lokalen Daten gespeisten
Bereiche samt Hinweis; leere Listen blenden ihre Sektion aus.

Das Browsing folgt demselben plattformneutralen Quellenbrowser wie die
Hauptanwendung (`AppModel.SourcesOverview`, `AppModel.SourceBrowser`):
die Startseite trägt eine „Quellen"-Reihe der freigegebenen
Medienbereiche (`SourceItemCell`-Karten mit Server-Icon aus
`api/sourceicons/{id}`); deren Auswahl öffnet die modale Browsing-Seite
(`SourceBrowserViewController`). Sie zeigt den Quellennamen als Titel,
ein Suchfeld (tvOS-Systemtastatur mit Diktat bzw.
Continuity-Tastatur — der eingegebene Text bleibt sichtbar), die
Genre-Filterleiste inklusive „Alle" (ausgeblendet, wenn die Quelle
keine Genres liefert) und ein vertikal scrollendes Poster-Raster, das
bei Annäherung an das Listenende weitere Seiten nachlädt
(`SourceItemsNextPageRequestedMessage`). Lade-, Leer- und
Fehlerzustände sind fernbedienungstauglich sichtbar; Ladefehler zeigen
eine „Erneut versuchen"-Aktion. Die Auswahl eines Eintrags führt — wie
auf der Startseite — auf die Detailansicht; die Menü-Taste schließt die
Seite zurück in den Menürahmen. Das Browsing ist rein lesend; kein
Schreibzugriff auf Inhalte wird verdrahtet.

Die Detailansicht (`MediaDetailsViewController`) ist das tvOS-Pendant
der Detailseiten der Hauptanwendung und wird modal gestapelt —
Detail → Detail wie im Navigationsstack (Episode aus einer Serie,
Film aus einer Sammlung). Sie nutzt denselben plattformneutralen
Detailseiten-Dienst (`MediaDetailsService`): online kommen die Daten
vom Server, ohne Verbindung aus dem gespeicherten Detail-Snapshot
(Download-Manifest, gecachte Serienstruktur, Favoriten-Kopie) statt
einer Fehlerseite. Der Kopfbereich zeigt Bildmaterial (Banner bzw.
Episoden-Still), Titel, Genre-/Jahres-Metadaten und Handlung sowie die
Kennzeichnungen „Gesehen", „Auf diesem Gerät" (lokal vorhandene
Download-Datei) und „Offline-Stand". Abgedeckt werden alle Medientypen:
Film, Filmsammlung (mit enthaltenen Filmen als Karussell), Serie,
Staffel und Episode — im Serienkontext steuern fokussierbare
Staffel-Chips und eine Episodenreihe die Auswahl; Episoden tragen je
einen Gesehen-Status, den lokale-Datei-Hinweis und bei
Offline-Platzhaltern die Markierung „nicht heruntergeladen". Als
Aktionen stehen „Abspielen", „Herunterladen" (manueller Download des
Mediums inklusive Serienkontext bei Episoden) und das
Umschalten der Favoriten-Markierung über `api/favorites/toggle` zur
Verfügung; die Favoriten-Änderung erneuert zugleich die lokale
Favoriten-Kopie. Eine Bearbeitung von Inhalten oder Playlists gibt es
nicht.

Der Player (`TvosPlayerViewController`) ist ein reiner
Ereignis-Übersetzer in den MVU-Zyklus: er wendet die aufgelöste
Quelle des `PlayerState` an, springt die Fortsetzungsposition bei
Wiedergabebereitschaft an und meldet Takt, Ende und Fehler als
Messages — die Schwellen- und Fortsetzungslogik verbleibt im
plattformneutralen `AppUpdate`. Playlist-Navigation liegt als
eigene Einträge in der Transport-Bar; Titel, Playlist-Position und
Gesehen-Hinweis zeigt ein eigener Tab im Pull-down-Infobereich
(`CustomInfoViewControllers`, tvOS 15), der zugleich die
systemeigene Spurwahl enthält.

Der Playlist-Zugriff ist rein lesend und nutzt denselben
plattformneutralen Dienst (`PlaylistService`) wie die Hauptanwendung.
Die Menüseite „Playlists" (`PlaylistsPageViewController`) zeigt die
eigenen und die öffentlichen Playlists des angemeldeten Benutzers in
einer gruppierten Liste — je Sektion „Meine Playlists" bzw.
„Öffentliche Playlists", je Zeile Cover (`api/playlists/{id}/cover`),
Name, Sortiermodus und Anzeige-Genres. Playlists sind ein reines
Online-Feature: ohne Serververbindung erscheint eine verständliche
Fehleranzeige mit Wiederholmöglichkeit statt eines veralteten Stands.
Die Auswahl öffnet die modale Detailseite
(`PlaylistDetailsViewController`): Kopfbereich mit Cover, Name,
Zusammenfassung (Eintragszahl, Sortiermodus), Genres und Beschreibung
sowie der Aktion „Abspielen", die den serverseitigen Wiedergabestart
ab dem ersten abspielbaren Eintrag auslöst. Darunter scrollt das
Poster-Raster der Einträge in serverseitiger Reihenfolge — Titel,
Poster über `api/pictures/{id}` und der übergeordnete Kontext (z. B.
„Serie · Episode N") — und lädt bei Annäherung ans Listenende
seitenweise nach (`api/playlists/{id}/entries/paged`). Die Auswahl
eines Eintrags entscheidet das Update: zugängliche Filme und Episoden
starten die Wiedergabe ab genau diesem Eintrag, alle übrigen Einträge
(Serien, Staffeln, Sammlungen, nicht zugängliche Medien) öffnen die
Medien-Detailansicht. Die Wiedergabe läuft über den Playlist-Kontext
des Players — Positionsanzeige im Infobereich, Übergang zum nächsten
bzw. vorherigen Eintrag per Transport-Leiste und Auto-Fortsetzung über
den advance-Endpunkt. Jegliche Playlist-Verwaltung (Anlegen,
Umbenennen, Sortieren, Eintragsverwaltung, Genre-Editor,
Öffentlich-Schalter) ist nicht verdrahtet und in der Oberfläche nicht
erreichbar.

Die Downloads nutzen denselben plattformneutralen `DownloadService`
mit Manifest wie die Hauptanwendung — ohne Auto-Download (kein
Vorlauf, keine „Cache"-Herkunft). Der eigentliche Transfer läuft
suspensionstauglich über eine `NSUrlSession`-Hintergrund-Session
(`TvosBackgroundDownloadBackend` hinter `IDownloadBackend`), weil
tvOS-Apps wochenlang suspendiert bleiben können: laufende Downloads
setzen sich bei suspendierter App fort; startet das System die App
für abgeschlossene Hintergrund-Transfers neu, meldet der
Completion-Sink deren Ergebnis dem Manifest
(`CompleteExternalTransferAsync`), und verwaist gebliebene Aufträge
werden über den Unterbrechungspfad (`ResumeInterruptedAsync`) neu
eingeplant. Der Anstoß erfolgt je Eintrag über die Detailansicht;
die Menüseite „Downloads" (`DownloadsPageViewController`) zeigt die
Manifest-Einträge mit Vorschaubild, Zustand, Fortschrittsbalken,
Größen- und Ablaufdatum-Angabe sowie Fehlerhinweis — die
Zeilenauswahl öffnet ein Aktionsmenü mit „Abbrechen" (aktive
Downloads), „Erneut versuchen" (fehlgeschlagene/abgebrochene) und
„Löschen"; im Kopf stehen die Kontingent-Belegung samt Hinweis auf
nicht zugeordnete Restdaten und die Sammelaktion „Fehlerhafte
löschen". Jeder Download trägt ein Ablaufdatum gemäß der
eingestellten Aufbewahrungszeit; die Quota-Obergrenze wird bereits
vor der Einplanung geprüft und bei Überschreitung verständlich
gemeldet. Der Pflicht-Abgleich (Teildateien entfernen, „fertig" ohne
Datei auf den Fehler für fehlende Dateien korrigieren — tvOS räumt
App-Daten unter Speicherdruck —, Waisen erkennen, Abgelaufenes
entfernen) läuft beim App-Start, bei Online-Rückkehr, beim Aufruf
der Download-Seite und zusätzlich über die plattformneutrale
`AppActivatedMessage` bei jedem Vordergrund-Eintritt aus dem
Suspend-Zustand. Heruntergeladene Dateien stehen der Wiedergabe als
bevorzugte Quelle zur Verfügung; ein automatisches Löschen nach dem
Abspielen wird auf tvOS nicht angeboten.

Plattform-Anschlüsse: `NSUserDefaults` für nicht-geheime Einstellungen,
Schlüsselbund (`SecKeyChain`, Generic Password) für Zugangsdaten und
Geräte-Tokens, `Library/Caches`-Verzeichnisse als Ablagewurzeln und
`NWPathMonitor` für den Netzwerkstatus.

## Einschränkungen

- tvOS besitzt kein garantiert dauerhaftes Dateisystem: lokale Daten
  (Datenbanken, Serverliste) liegen in `Library/Caches` und können vom
  System geräumt werden — eine Räumung erzwingt die Neu-Einrichtung.
  Geheimnisse (Schlüsselbund) und Einstellungen (`NSUserDefaults`)
  sind davon nicht betroffen.
- Die Wiedergabe läuft im nativen tvOS-Vollbildplayer
  (`AVPlayerViewController` mit systemeigenem Transport-UI):
  Server-Stream oder lokale Datei ab der Fortsetzungsposition,
  5-Sekunden-Fortschrittstakt an den Server bzw. die
  Offline-Warteschlange, „angesehen" erst nach bestätigtem
  Endfenstereintritt, Auto-Fortsetzung über Serien- und
  Playlist-Kontext (advance/next/previous-Endpunkte) gemäß
  Einstellung, Fehler-Rückfall auf den nächsten Eintrag sowie
  Wiedergabe-Schutz für Downloads (Ablauf-Verlängerung während des
  Abspielens). Die Ton-/Untertitel-Auswahl steht über den
  System-Infobereich zur Verfügung, sofern der Stream Spurgruppen
  anbietet — sonst entfällt sie.
- Downloads laufen suspensionstauglich über die
  `NSUrlSession`-Hintergrund-Session; Dateien liegen wie alle
  lokalen Daten in `Library/Caches` und können vom System geräumt
  werden — der Abgleich meldet solche Einträge dann als
  fehlgeschlagen („Datei nicht mehr vorhanden") und erlaubt die
  Neu-Einplanung. Es gibt keinen Auto-Download-Vorlauf und kein
  automatisches Löschen nach dem Abspielen auf tvOS. Weitere
  Einstellungsbereiche folgen in späteren Entwicklungsschritten —
  betroffene Menüseiten zeigen derzeit Platzhaltertexte. Playlists
  sind auf tvOS ausschließlich lesend verfügbar; jegliche
  Playlist-Verwaltung bleibt der Hauptanwendung vorbehalten.
- Eine Veröffentlichung (App Store / TestFlight) ist noch nicht
  eingerichtet; die dafür nötigen Signatur-Artefakte liegen nicht vor.
