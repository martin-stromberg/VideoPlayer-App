# Bestandsaufnahme: saisonabhängige Genre-Listen

## Ergebnis

Die mobile Startseite ist auf drei fest verdrahtete Listen und deren
Cache-Synchronisation ausgelegt: Weiterschauen, Favoriten und Neu im Programm.
Ein Vertrag für saisonale Genre-Listen ist im mobilen Repository nicht
vorhanden. Allgemeine Genre-APIs für Quellen-Browsing und Playlist-Genres
decken weder saisonale Listen noch deren Sichtbarkeitszeitraum ab.

Das maßgebliche Server/Web-Repository liegt lokal unter
`D:/Repositories/softwareschmiede/1f22729c-5f43-4f69-94c5-aee41f073cac`.
Branch: `task/issue-277-1f22729c5f434f6994c5aee41f073cac-tv-sender-platzhalter-clips-gr`.
Die Arbeitskopie war zum Lesezeitpunkt sauber. Die aktuelle Branchbezeichnung
und `AGENTS.md` zeigen, dass diese Arbeitskopie einem anderen Thema gehört.
Die spätere Umsetzung muss deshalb in einem separaten, isolierten Klon/Checkout
innerhalb des freigegebenen Workspace und auf einem eigenen Feature-Branch
erfolgen. Der externe Ursprungscheckout und seine Git-Metadaten dürfen nicht
verändert werden. Die Bestandsaufnahme wurde nur lesend durchgeführt.

Die Webansicht berechnet saisonale Genres über `GenreService`, lädt dann
Movies, MovieCollections und TVShows aus für den Benutzer erlaubten Quellen,
gruppiert Sammlungen und sortiert Einträge. Die Umsetzung liest derzeit direkt
über den `ApplicationDbContext`; ein saisonaler HTTP-Endpunkt ist nicht
vorhanden. Die Nutzerantworten bestimmen die gewünschte Richtung: API zuerst,
danach nutzen sowohl Mobile als auch Web dieselbe API; der Server bleibt für
die Zeitlogik zuständig.

## Geprüfte Bereiche

- [API- und Serververträge](inventory/api-vertraege.md): vorhandene mobile
  API-Modelle und Endpunkte, Serverrepo/-branch und Websemantik.
- [Mobile Startseite und Datenfluss](inventory/mobile-startseite.md):
  Startseiten-XAML, MVU-Zustand, Cache-Service und Synchronisation.
- [Tests und E2E-Betrieb](inventory/tests-und-e2e.md): relevante
  Unit-/Contract-Tests, Mock-Server und ausführbarer UI-Testablauf.

## Auswirkungen auf die Planung

Die Anforderungen sind geklärt und der Plan setzt nun einen konkreten
authentifizierten GET-Vertrag samt Payload, Fehler-/Leerregeln, Sortierung,
Zeitsemantik und Clientverhalten fest. Die sichere Serverstrategie und genaue
mobile Windows-E2E-Flüsse sind im [Plan](plan.md) dokumentiert.

Es wurden keine Tests ausgeführt; dies ist eine Code- und Workflow-Bestandsaufnahme.
