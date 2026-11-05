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
Cache-First-Startseiten-Listen, Navigation). Nicht für tvOS vorgesehene
Effekte (Downloads, Wiedergabe/PiP, Inhalts-/Playlist-Verwaltung,
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
- Die Inhaltsseiten (Startseiten-Listen, Browsing, Detailansichten,
  Wiedergabe, Playlist- und Download-Ansichten, weitere
  Einstellungsbereiche) folgen in späteren Entwicklungsschritten —
  betroffene Menüseiten zeigen derzeit Platzhaltertexte.
- Eine Veröffentlichung (App Store / TestFlight) ist noch nicht
  eingerichtet; die dafür nötigen Signatur-Artefakte liegen nicht vor.
