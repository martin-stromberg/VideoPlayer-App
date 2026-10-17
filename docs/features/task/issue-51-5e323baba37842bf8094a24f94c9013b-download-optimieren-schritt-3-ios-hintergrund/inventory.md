# Bestandsaufnahme: iOS-Hintergrund-Downloads

- `DownloadService.TransferAsync` nutzt `HttpClient`-Stream → `FileStream`
  prozessintern; keine Plattform-Abstraktion außer `httpClientFactory`.
- iOS `Info.plist`: nur `UIBackgroundModes = audio`; `AppDelegate` minimal.
- `ReconcileAsync` (Schritt 1) und `ResumeInterruptedAsync` liefern den
  Abgleich für den Neustart-/Rückkehr-Fall.
- Test-Stand: 1356 grün (Schritt-2-Stand); iOS-Code hier nicht bau-/lauffähig
  → Nachweis über Contract-Tests + plattformneutral testbare Logik.
