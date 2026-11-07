# Code-Review — Schritt 9: tvOS-Downloads

Status: **Keine Befunde**

Abweichung: Selbst-Review durch den Hauptagenten (keine Unteragenten verfügbar).

## Geprüfte Punkte

- **tvOS-Kompilierfallen:**
  - `UIApplicationDelegate.HandleEventsForBackgroundUrlSession` ist Basistyp-Member → `override` statt `[Export]` (in `AppDelegate.cs` korrigiert).
  - `UIProgressView.Progress` ist `float`, nicht `nfloat` → Cast korrigiert (`DownloadTableCell.cs`).
  - Kein `const nfloat` (`static readonly` verwendet), kein `SeparatorStyle`, kein `RegisterClassForCell`, keine `IReadOnlyList.IndexOf`-Nutzung, `UIAlertAction`-Handler-Parameter heißt `action` (nicht `_`).
  - Named-Args-Case in `DownloadEnqueueRequest`-Aufruf stimmt mit Parameternamen überein.
- **Parität zur iOS-Vorlage:** `TvosBackgroundDownloadBackend` ist ein wortgetreuer Port (Session-Kennung, `TaskDescription`-Key-Format, `pending`-Verwaltung, Temp→Ziel-`File.Copy`, `CompletionSink`-Semantik); identischer Session-Identifier wie iOS ist zulässig (getrennte Bundles/Geräte).
- **Offline-Sicherheit der Aktivierung:** `AppActivatedMessage` → bei `Connected` `ResumeDownloadsEffect` (enthält Abgleich + `ResumeInterruptedAsync`), sonst `ReconcileDownloadsEffect` — verhindert `no_connection`-Fehlschläge verfrühter Wiederaufnahmen.
- **Kein Auto-Download:** `RunAutoDownloadsEffect`/`AutoDownloadService` bleiben auf tvOS unverdrahtet (Contract-Test abgesichert); `ReconcileAsync` erhält keine Kandidatenmenge.
- **Kein „nach Abspielen löschen":** `ExpireWatchedDownloadEffect`/`AutoDeletePlayedDownloads` werden von tvOS-UI nicht angeboten (Einstellung kommt erst in Schritt 10; `ExpireWatchedDownloadAsync` im Handler ist bereits für den Wiedergabe-Schutz-Pfad vorhanden und bleibt unverändert).
- **Rückmeldungswege:** `ItemChanged`/`ItemRemoved` → `DownloadItemChangedMessage`/`DownloadDeletedMessage` (bestand); `DownloadsLoadedMessage` nach Sammelaktionen; `DownloadEnqueueCompletedMessage` mit Notice.
- **Speicher-/Reconcile-Pfad:** `ReconcileAsync` deckt Teildateien, Waisen (`OrphanedBytes` in Kopfzeile) und `missing_file`-Korrektur ab.

## Befunde

Keine.
