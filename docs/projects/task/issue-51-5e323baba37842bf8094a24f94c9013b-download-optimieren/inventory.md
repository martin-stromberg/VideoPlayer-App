# Bestandsaufnahme: Download optimieren (Issue #51)

Analysiert wurde die Download-Funktion der aktiven App `VideoPlayer.Maui`
(MVU-Architektur, plattformneutrale Logik in `VideoPlayer.Mvu`, Ablage in
`VideoPlayer.Storage`) bezogen auf die Anforderung: Hintergrund-Downloads unter
iOS, Erkennung/Bereinigung verwaister Download-Daten sowie mehr Kontrolle und
Transparenz über belegten Speicher.

## Zusammenfassung

- Downloads laufen als prozessinterne `HttpClient`-Streams in
  `VideoPlayer.Mvu/DownloadService.cs` (`TransferAsync`). Es gibt **keine**
  iOS-Hintergrund-Übertragung; die `Info.plist` kennt nur
  `UIBackgroundModes = audio`. Wechselt die App in den Hintergrund, suspendiert
  iOS den Prozess und die Sockets — der Download bricht ab.
- Die Speicherbelegung (`DownloadService.GetUsedBytes`) summiert **alle**
  Dateien im Video-Verzeichnis — inklusive Dateien ohne Manifest-Eintrag.
  Das erklärt das Fehlerbild „13,1 GB belegt bei einem einzigen Listeneintrag":
  verwaiste Dateien werden mitgezählt, aber nicht angezeigt.
- Verwaiste Dateien entstehen u. a. bei Prozessabbruch während des Transfers
  (Teildatei ohne sauberen `TryDeleteFile`-Pfad) und bei Verlust der
  Cache-Datenbank `videoplayer-cache.db3` (Manifest weg, Dateien bleiben).
  Es existiert **kein** Abgleich Manifest ↔ Dateisystem und keine Bereinigung.
- Der Anwender kann einzelne Downloads löschen (`DeleteAsync`), nicht aber
  alle auf einmal; eine Größenanzeige pro Download existiert nicht
  (`DownloadListItem` trägt kein `SizeBytes`).
- Wiederaufnahme ist vorhanden (`ResumeInterruptedAsync` beim Verbindungsaufbau
  und nach App-Neustart), startet aber mangels Range-Support am Server
  immer ab Byte 0.
- Test-Ausgangszustand: **1335 Unit-/Verhaltenstests bestanden**, 0 Fehler,
  0 übersprungen — Nachweis in [Tests](inventory/tests.md).
- E2E-Suite (`VideoPlayer.E2E.Tests`, FlaUI, Windows) wurde im Baseline-Lauf
  nicht ausgeführt — sie benötigt einen laufenden Windows-Desktop mit der
  gebauten App und einem Mock-Server; für dieses iOS-zentrierte Thema sind
  Downloads-E2E-Szenarien außerdem nicht vorhanden (kein `DownloadsTests.cs`).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Plattform/iOS](inventory/platforms.md)
- [Tests](inventory/tests.md)
