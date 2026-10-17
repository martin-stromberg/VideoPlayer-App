# Anforderung: Video-Downloads unter iOS im Hintergrund abschließen

Entwicklungsschritt 3 des Projektplans „Download optimieren" (Issue #51).

## Fachliche Zusammenfassung

Auf iOS wird die Dateiübertragung an eine systemseitige Hintergrund-Übertragung
(iOS-Background-URL-Session) delegiert; die App-seitige Auftragsverwaltung
(Manifest, Warteschlange, Kontingent, WLAN-Bedingung, max. 2 parallel) bleibt
führend. Übrige Plattformen bleiben beim bisherigen prozessinternen Transfer.

## Akzeptanzkriterien

1. Laufender Download wird nach Wechsel in den Hintergrund fortgeführt, Datei
   vollständig abgelegt, Eintrag „fertig".
2. Wartende Downloads rücken nach / starten beim nächsten Vordergrund- oder
   Start-Ereignis.
3. Abbruch und Löschen wirken auch auf systemseitige Transfers.
4. Kontingent, WLAN-Bedingung, max. 2 parallel gelten unverändert.
5. Nach App-Neustart stimmt der Listen-Zustand mit dem Ergebnis der
   Hintergrund-Transfers überein.

## Betroffene Komponenten

- `VideoPlayer.Mvu`: neue Abstraktion `IDownloadBackend` (Plattform-Transfer),
  `DownloadService` delegiert den Transfer, WLAN-Flag wird durchgereicht.
- `VideoPlayer.Maui/Platforms/iOS`: `IosBackgroundDownloadBackend`
  (NSUrlSession-Hintergrundsession + Download-Delegate), `AppDelegate`
  (`HandleEventsForBackgroundUrlSession`).
- `VideoPlayer.Maui/MauiProgram.cs`: Registrierung der iOS-Implementierung.
- Tests: Contract-Tests (iOS-Verdrahtung), Service-Tests mit Fake-Backend.
