# Projektplan-Gegenprüfung

## Ergebnis

**Status:** Projektplan vollständig

## Abgleich Anforderung ↔ Entwicklungsschritte

| Anforderungspunkt | Abgedeckt durch Schritt(e) | Status |
|--------------------|-----------------------------|--------|
| Neuentwicklung ohne Übernahme der bisherigen App-Implementierung; ausschließlich öffentliche Server-API | Vorgehensentscheidung „Bestandscode"/„Serverbezug"; Schritte 1, 2 (explizit „kein Code des bisherigen Clients/keine Übernahme des MVVM-Codes") | Abgedeckt |
| API-Client (Neuimplementierung): Endpunkte Anmeldung, Erreichbarkeit, Quellen, zuletzt hinzugefügte Inhalte, Weiterschauen inkl. Fortschritt/Ausblenden/Überspringen, Favoriten (Abruf/Markieren/Entfernen), Mediennachschlagen (Filme, Serien, Staffeln, Episoden, Filmkollektionen, Genres, Metadaten), Bildabrufe; DTO-Felder aus Bestand als Referenz | Schritt 1 (Endpunktliste mit „darunter" = nicht erschöpfend; Metadatenbezug zusätzlich über Metadaten-Sync in Schritt 6) | Abgedeckt |
| Client-Gate per API-Schlüssel (`X-API-Key`), konfigurierbar über MSBuild-Property bzw. Umgebungsvariable (`MauiClientApiToken` / `VIDEOWEBPLAYER_MAUI_API_TOKEN`) | Schritt 1 („Client-Gate per API-Schlüssel, konfigurierbar über Build-Eigenschaft bzw. Umgebungsvariable") | Abgedeckt |
| Tokenbasierte Anmeldung inkl. automatischer Erneuerung bei „nicht autorisiert" (stille Neuanmeldung/Token-Refresh) | Schritte 1, 5 | Abgedeckt |
| Definierte Retry-Strategie für fehlerhafte Serverantworten (3 Versuche, exponentieller Backoff 1 s/2 s/4 s, Ausnahmen 401/408/429) | Vorgehensentscheidung „Fehlerbehandlung"; Schritte 1, 5 | Abgedeckt |
| Benutzerbezogene Einstellungen (8 Stück): automatische Downloads, Anzahl vorzuladender Episoden, Auto-Fortsetzung an/aus, Zeitfenster Auto-Fortsetzung, WLAN-only, Zeitfenster „als angesehen", Speicherplatzkontingent (GB, Standard 5 GB), Auto-Löschung gesehener Episoden — persistiert, pflegbar, im Cloud-Backup | Schritt 3 (alle acht Einstellungen explizit genannt inkl. Semantik und Standardwert) | Abgedeckt |
| Ablagetrennung: Geheimnisse (Zugangsdaten, Token) in sicherer Ablage, nicht-geheime Einstellungen normal; Profil-Isolation über Umgebungsvariable (`VIDEOWEBPLAYER_PROFILE`) | Schritt 3 | Abgedeckt |
| Lokale Datenhaltung: lokale Datenbank für Downloads/Caches, persistente Outbox für unsynchronisierte Fortschritte, Download-Manifest | Schritte 3 (Datenbank/Ablagestruktur), 6 (persistente Warteschlange über Neustarts), 11 (Download-Manifest, nicht backup-pflichtig) | Abgedeckt |
| Backup-Klassifikation: backup-pflichtig (Serverliste, letzter Server, Profil ohne Geheimnisse, Einstellungen, unsynchronisierte Fortschritte) vs. nicht backup-pflichtig (Offline-Videos, Vorschaubilder, Startseiten-Metadaten, Episodenlisten, Download-Manifest, Temp-Cache, lokale Favoriten-Kopie); iOS-Ausschluss, Windows-Roaming-Äquivalent prüfen; Geheimnisse nicht im Backup | Vorgehensentscheidung „Backup"; Schritt 3 | Abgedeckt |
| Multi-Server-Verwaltung: Liste bekannter Server, Persistenz des zuletzt verbundenen Servers, automatische Verwendung beim Start | Schritt 4 | Abgedeckt |
| Erststart-Einrichtung: Dialog mit manueller Eingabe und automatischer Erkennung (mDNS-Dienstsuche, UDP-Broadcast Port 5001 mit bekanntem Schema), Auswahl gefundener Server, Speicherung erfolgreicher Verbindung; serverseitige Antwort verifizieren, sonst Server-Issue | Vorgehensentscheidung „Servererkennung"; Schritt 4 | Abgedeckt |
| „Server wechseln" in den Einstellungen: bewusst unauffällig, bekannte Server anzeigen, neue hinzufügen | Schritt 4 | Abgedeckt |
| Verbindungs-Workflow/Start: Ladeanzeige → automatischer Verbindungsversuch zum letzten Server → Login mit gespeicherten Credentials bzw. Anmeldemaske; Zustände verbunden/Anmeldung erforderlich/Servereinrichtung erforderlich/offline steuern Navigation; Offline-Modus oder Serverauswahl bei Nichterreichbarkeit | Schritte 2 (Ladeanzeige), 5 (vier Zustandsausgänge, Anmeldemaske) | Abgedeckt |
| Netzwerkstatus-Erkennung (Online/Offline-Wechsel) | Schritt 6 | Abgedeckt |
| Server-Ereignisse (Echtzeit-Verbindung): Weiterschauen-Aktualisierung, Favoriten-Änderungen, neu eingescannte Inhalte; Reconnect-Behandlung | Schritt 6 (Ereignis-Mechanismus inkl. Reconnect), Anzeige-Aktualisierung in Schritt 7 | Abgedeckt |
| Synchronisation bei Online-Rückkehr: automatischer Reconnect, vollständiger Metadaten-Sync, Nachsenden unsynchronisierter Fortschritte, Aktualisierung der Weiterschauen-Liste | Schritt 6 | Abgedeckt |
| Hamburgermenü mit Einträgen Startseite, Quellen, Favoriten, Downloads, Profil, Einstellungen; zugehörige Seiten als Gerüste | Schritt 2 | Abgedeckt |
| Startseite: horizontale Listen „Weiterschauen", „Favoriten", „Neu im Programm" (Endpunkt zuletzt hinzugefügte Inhalte), alle lokal gecacht, Aktualisierung durch Server-Ereignisse | Schritt 7 | Abgedeckt |
| Quellenübersicht (freigegebene Medienbereiche) mit Browsing; Detailseiten für Serien (Staffeln/Episoden) und Filmkollektionen | Schritt 8 | Abgedeckt |
| Favoriten: markieren/entfernen, primär serverseitig geführt, lokale Kopie als nicht backup-pflichtiger Offline-Cache; eigene Favoriten-Seite | Vorgehensentscheidung „Favoriten"; Schritte 8, 9 | Abgedeckt |
| Profil-Seite: angemeldeter Benutzer, aktives lokales Profil, Abmeldung | Schritt 9 (löst offene Frage „Profil" sinnvoll auf) | Abgedeckt |
| Wiedergabe: `CommunityToolkit.Maui.MediaElement` mit verpflichtendem PiP auf iOS und Windows; Windows-PiP als kompaktes Overlay-/Mini-Player-Fenster, Machbarkeit frühzeitig verifizieren; Server-Streams und lokale Dateien; Fortsetzungsposition | Vorgehensentscheidung „Picture-in-Picture"; Schritt 10 | Abgedeckt |
| Fortschrittsverfolgung: Meldung an Server, offline Sammeln in lokaler Warteschlange, Nachsynchronisation bei Online-Rückkehr | Schritte 10, 6 | Abgedeckt |
| „Als angesehen" am Videoende mit konfigurierbarem Zeitfenster; Auto-Fortsetzung mit an/aus-Schalter und konfigurierbarem Zeitfenster | Schritte 10, 3 (Einstellungen) | Abgedeckt |
| Nicht verfügbare Videoquelle → Fehlermeldung + Rückfall auf nächste Episode | Schritt 10 | Abgedeckt |
| Download-Verwaltung: parallele Downloads, automatische Wiederaufnahme (auch über Neustarts), Fehlerbehandlung, Speicherplatzkontingent, optionale Auto-Löschung gesehener Episoden, Downloads-Seite mit Status/Fortschritt | Schritt 11 | Abgedeckt |
| Automatische Downloads (Vorladen von Episoden, insb. Weiterschauen-Liste): aktivierbar, Anzahl und WLAN-only konfigurierbar | Schritt 11 (Einstellungen aus Schritt 3, Netzwerkstatus aus Schritt 6, Weiterschauen-Liste aus Schritt 7 — transitiv über Schritt 10) | Abgedeckt |
| Offlinebetrieb: Startseite aus Cache, „Weiterschauen" nur aus lokalen Downloads, fehlende Episoden als Platzhalter, Downloads abspielbar, Favoriten lokal; Einschränkungen (keine neuen Inhalte, keine Quellenliste, kein Profilabruf) | Schritt 12 | Abgedeckt |
| Wiederherstellung aus Cloud-Backup: automatischer Reconnect zum letzten Server, ggf. erneute Anmeldung (Geheimnisse nicht gesichert), Offline-Modus/Serverauswahl bei Nichterreichbarkeit, danach Metadaten-Sync + Fortschritts-Nachsenden + Weiterschauen-Aktualisierung | Schritt 12 (nutzt Workflow aus Schritt 5 und Synchronisation aus Schritt 6) | Abgedeckt |
| Fehlerszenarien: Server nicht erreichbar → Offline-Modus; fehlerhafte Antworten → Retry; abgebrochene Downloads → Wiederaufnahme; nicht verfügbare Quelle → Fehlermeldung + nächste Episode; beschädigte lokale Daten → definiertes Verhalten (Cache verwerfen/neu synchronisieren, DB neu initialisieren, Nutzerinformation) | Schritte 5, 1, 11, 10, 12 | Abgedeckt |
| MVU-Architektur über eigenen schlanken MVU-Zyklus, kein externes Framework | Vorgehensentscheidung „Architektur"; Schritt 2 (in allen weiteren Schrittbeschreibungen als Kontext wiederholt) | Abgedeckt |
| Zielplattformen iOS und Windows, Android-erweiterbar (nicht Teil der ersten Stufe); iOS-Nachweis über CI (macOS) | Vorgehensentscheidung „Testbarkeit"; Schritt 2 | Abgedeckt |
| UI/Bedienung an bestehender Webanwendung ausgerichtet; kein Abwarten eines Google-Stitch-Entwurfs | Vorgehensentscheidung „Design"; explizit in Schritten 7, 8, 9 („Aussehen und Bedienung richten sich an der bestehenden Webanwendung aus") | Abgedeckt |
| Serverbezug: nur öffentliche API; API-Lücken → Issues im Server-Repository statt Workarounds; keine Server-Änderungen in diesem Repo | Vorgehensentscheidung „Serverbezug"; Schritt 4 (Discovery-Fall) | Abgedeckt |
| Testbarkeit gegen die API des lokal im IIS eingerichteten Webprojekts „VideoPlayer" | Schritte 1, 2 (explizit in den Akzeptanzkriterien) | Abgedeckt |
| Cache-Dienste: Startseiten-Listen-Cache, Vorschaubild-Cache, Episodenlisten-Cache | Schritte 7 (Listen-Cache), 3 (Ablagestruktur für Vorschaubilder/Episodenlisten als nicht backup-pflichtig), 12 (Cache-Neuaufbau bei Beschädigung) | Abgedeckt |
| Tests (`VideoWebPlayer.Maui.Tests`): automatisierte Tests für neue Bereiche (Backup, Reconnect/Wiederherstellung, Offline-Sync, Download-Verwaltung, Serververwaltung/Erststart) | Vorgehensentscheidung „Testbarkeit" („jeder Entwicklungsschritt liefert automatisierte Tests"); Akzeptanzkriterien der Schritte 3–12 fordern Nachweisbarkeit | Abgedeckt |
| Nicht-Ziele: keine Server-Änderungen, keine API-Workarounds, keine Wiederverwendung der alten iOS-App, Android nur Erweiterbarkeit, keine Funktionen über den Umfang hinaus | Vorgehensentscheidungen „Bestandscode", „Serverbezug"; Schritt 2 (Android-erweiterbar); kein Schritt enthält Umfang außerhalb der Anforderung | Abgedeckt |

## Abhängigkeitsprüfung

| Prüfpunkt | Befund |
|-----------|--------|
| Zyklen in Abhängigkeiten | Keine gefunden. Alle Abhängigkeiten verweisen ausschließlich auf kleinere Schrittnummern (1: –, 2: 1, 3: 2, 4: 2+3, 5: 4, 6: 5, 7: 6, 8: 5, 9: 5, 10: 6+7+8, 11: 6+10, 12: 7+9+11); topologische Reihenfolge existiert. |
| Verweise auf nicht existierende Schritte | Keine; alle Referenzen liegen im Bereich 1–12. |
| Reihenfolge konsistent zu inhaltlichen Abhängigkeiten | Ja. Inhaltliche Abhängigkeiten ohne expliziten Vermerk sind transitiv auflösbar: Schritt 8/9 (Favoriten-Cache, Datenhaltung aus Schritt 3) via 5→4→3; Schritt 11 (Weiterschauen-Liste für automatische Downloads aus Schritt 7, Download-Einstellungen aus Schritt 3) via 10→7 bzw. 6→5→4→3; Schritt 12 (Verbindungsworkflow, Synchronisation, Datenhaltung) via 7→6→5→4→3 bzw. 11→6. Kein Schritt steht vor einem Schritt, von dem er inhaltlich abhängt. |

## Fehlende oder unvollständige Punkte

—

## Hinweise

- **Initialer Metadaten-Sync beim Start:** Die Anforderung beschreibt im Start-Workflow nach erfolgreichem Login einen Metadaten-Sync. Schritt 5 endet mit dem Zustand „verbunden"; die Synchronisation ist in Schritt 6 verortet (dort formal an „Online-Rückkehr" gekoppelt). Bei der Abarbeitung von Schritt 6 ist sicherzustellen, dass die Synchronisation auch beim initialen Verbindungsaufbau nach der Anmeldung ausgelöst wird — Schritt 12 setzt dies im Wiederherstellungsablauf bereits voraus.
- **Download-Ereignisse:** Die Anforderung nennt in der Referenzliste auch Download-bezogene Server-Ereignisse (`DownloadCompletedEvent`, `DownloadDeletedEvent`). Schritt 6 führt den Ereignis-Mechanismus mit drei Beispiel-Ereignissen ein; bei der Umsetzung der Downloads-Seite (Schritt 11) ist zu prüfen, ob Download-Ereignisse über denselben Mechanismus eingehen bzw. ob sie serverseitig überhaupt gesendet werden (ggf. Server-Issue).
- **Ausblenden/Überspringen in „Weiterschauen":** Die Endpunkte `hide`/`skip` sind in Schritt 1 abgedeckt; eine Bedienfunktion dafür in der Oberfläche ist in keinem UI-Schritt explizit benannt. Bei der Umsetzung der Startseite (Schritt 7) ist anhand der Webanwendung als Gestaltungsreferenz zu entscheiden, ob diese Aktionen in der UI angeboten werden.
- **Vorschaubild-/Episodenlisten-Cache:** Diese Cache-Bereiche sind über die Ablagetrennung (Schritt 3) und den Listen-Cache (Schritt 7) implizit abgedeckt; bei der Umsetzung von Schritt 7 bzw. 12 sollten sie als eigene Cache-Kategorien explizit mitgedacht werden (insb. gecachte Vorschaubilder für die Offline-Startseite).
- **UI-Ausrichtung an der Webanwendung:** Explizit genannt in den Schritten 7, 8 und 9; für die übrigen Oberflächen-Schritte (2, 4, 5, 10, 11, 12) gilt die Vorgehensentscheidung „Design" global — bei der Abarbeitung ist sie als verbindliche Gestaltungsreferenz mitzuführen.
