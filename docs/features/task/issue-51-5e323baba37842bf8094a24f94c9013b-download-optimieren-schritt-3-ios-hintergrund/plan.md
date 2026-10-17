# Umsetzungsplan: iOS-Hintergrund-Downloads

## Designentscheidungen

| Bereich | Ansatz | Begründung |
|---------|--------|------------|
| Transfer-Abstraktion | `IDownloadBackend.TransferAsync(url, token, targetPath, wifiOnly, progress, ct)` → Task; `HttpDownloadBackend` = bisheriger Stream | iOS delegiert ans System; Manifest/Quota bleiben im Service |
| iOS-Backend | `IosBackgroundDownloadBackend` mit `NSUrlSession` Hintergrund-Config, Session-Id `videoplayer.downloads`, Task-Description = Medien-Key; Datei wird vom Delegate an den Zielpfad verschoben | System führt Transfer prozessunabhängig |
| App-Neustart | `AppDelegate.HandleEventsForBackgroundUrlSession` reicht Completion-Handler ans Backend; fertige Downloads aktualisieren Manifest via Callback | iOS startet App für Session-Events neu |
| Parallelität | Backend wird je aktivem Auftrag aufgerufen — max. 2 bleibt App-seitig | Stakeholder-Entscheidung |

## Programmabläufe

1. `TransferAsync` → Backend (iOS: NSUrlSession-Download-Task, `AllowsCellularAccess = !wifiOnly`, discretionary-artiges Verhalten nicht nötig).
2. Delegate `DidFinishDownload` → Datei an `entry.FilePath` verschieben, Fortschritt/Abschluss an Service melden.
3. `DidCompleteWithError` → `network`-Fehler; `CancelAsync`/`DeleteAsync` → `task.Cancel()` + Datei entfernen.
4. `HandleEventsForBackgroundUrlSession` → Session an Backend, Completion-Handler aufrufen sobald alle Tasks fertig.
5. Beim Start: `ReconcileAsync` bereinigt Reste; Einträge `downloading` ohne laufenden Backend-Task → `ResumeInterruptedAsync` neu einplanen.

## Neue/geänderte Elemente

- Neu: `IDownloadBackend`, `DownloadBackendResult`, `HttpDownloadBackend` (Mvu); `IosBackgroundDownloadBackend` (Platforms/iOS).
- `DownloadService`: optionaler Backend-Parameter; `TransferAsync` delegiert; WLAN-Flag an Backend; `BuildStreamUrlAsync` bleibt.
- `AppDelegate`: `HandleEventsForBackgroundUrlSession`.
- `MauiProgram`: `#if IOS` Registrierung.

## Tests

- `DownloadsContractTests`/`IosBackgroundContractTests`: iOS-Datei enthält `NSUrlSession.CreateBackgroundSession`, `HandleEventsForBackgroundUrlSession`, Registrierung in `MauiProgram`, `UIBackgroundModes` unverändert (kein fetch nötig — URLSession braucht keinen Background-Mode-Eintrag).
- `DownloadServiceBackendTests` (Fake-Backend): Erfolg → completed+Datei; Fehler → failed+`network`; Abbruch → cancelled; Quota greift vor Backend-Aufruf.
- E2E: iOS nicht ausführbar — Nachweis über Unit-/Contract-Tests (Begründung wie Schritt 1).

## Offene Punkte

Keine.
