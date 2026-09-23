# Bestandsaufnahme: Videowiedergabe mit Picture-in-Picture und Fortschrittsverfolgung (Entwicklungsschritt 10)

Analysiert wurden der MVU-Kern (`VideoPlayer.Mvu`), der App-Rand
(`VideoPlayer.Maui`), die Ablage (`VideoPlayer.Storage`), der
API-Client (`VideoPlayer.ApiClient`) sowie der Altbestand
(`VideoWebPlayer.Maui`/`VideoWebPlayer.Client`) bezogen auf die
Anforderung „Videowiedergabe mit Picture-in-Picture und
Fortschrittsverfolgung".

## Zusammenfassung

- **Bereits vorhanden:** `ProgressReportService` (online sofort,
  offline über die persistente Warteschlange `pending_progress`),
  Drain der Warteschlange bei Online-Rückkehr über
  `ServerSyncService.FlushPendingProgressAsync` (läuft im
  `RunFullSyncEffect` der Wiederverbindungs-Kette), die drei
  benötigten Einstellungen (`AutoResumeEnabled`,
  `AutoResumeRemainingSeconds`, `WatchedEndWindowSeconds` in
  `AppSettings`/`AppSettingsService`), der Serienkontext der
  Detailseite (`MediaDetailsView.Seasons` mit sortierten Episoden),
  die Weiterschauen-Liste mit `PositionSeconds` (Server-Endpunkt
  `api/continue-watching` + Cache), die Stream-URL-Bildung im
  Altbestand (`{server}/api/items/{typ}/{id}/stream?access_token=…`)
  und ein Altbestand-Player auf `CommunityToolkit.Maui.MediaElement`
  10.0.0 (`MediaBannerPlayer` mit 5-s-Fortschrittstakt).
- **Noch fehlend:** Player-Seite + Route `player`, MediaElement-
  Referenz in `VideoPlayer.Maui` (`UseMauiCommunityToolkitMediaElement`
  fehlt in `MauiProgram`), Quellenabstraktion (Stream vs. lokale
  Datei), Fortsetzungspositions-Auflösung, „angesehen"-Schwelle,
  Auto-Fortsetzung, Fehler-Rückfall auf die nächste Episode,
  PiP-Umsetzung (iOS: `AVPlayerViewController`-PiP über die
  Transportsteuerung + `UIBackgroundModes=audio` in `Info.plist`;
  Windows: `AppWindow`-`CompactOverlay`-Presenter als Mini-Player).

Test-Ausgangszustand: 469 Tests, alle erfolgreich (Details in
[inventory/tests.md](inventory/tests.md)).

## Details

- [MVU-Kern und Player-Kontext](inventory/mvu.md)
- [Services und Ablage](inventory/services.md)
- [UI und Navigation](inventory/ui-navigation.md)
- [MediaElement und PiP-Machbarkeit](inventory/pip-machbarkeit.md)
- [Altbestand-Referenz](inventory/legacy.md)
- [Tests](inventory/tests.md)
