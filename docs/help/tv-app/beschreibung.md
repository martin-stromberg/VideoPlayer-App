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
des Start-Workflows, Verbindungs-Zustandsseiten (Server einrichten,
Anmelden — jeweils mit „Erneut prüfen"-Aktion) oder den Tab-Rahmen.

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
- Eingabe-Dialoge der Verbindungsphase (Serverwahl, Pairing-Code,
  Anmeldeformular) sowie sämtliche Inhaltsseiten folgen in späteren
  Entwicklungsschritten — die Seiten zeigen derzeit Status- und
  Hinweistexte.
- Eine Veröffentlichung (App Store / TestFlight) ist noch nicht
  eingerichtet; die dafür nötigen Signatur-Artefakte liegen nicht vor.
