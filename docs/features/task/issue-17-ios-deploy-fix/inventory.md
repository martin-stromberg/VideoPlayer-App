# Bestandsaufnahme: iOS-Deployment-Skript reparieren (MSB1006)

Analysiert wurde `scripts/iOS-Deployment.ps1` (~1125 Zeilen) bezogen auf den
MSB1006-Fehler beim Release-Build mit App-Store-Connect-Upload, sowie das
Testprojekt `VideoWebPlayer.Maui.Tests` fuer den abzusichernden Vertrag.
Die Analyse baut auf der Projekt-Inventur
`docs/projects/project/issue-17-testbarkeit-und-onboarding/inventory/ios-deployment.md`
auf und ergaenzt sie um eine programmatische MSB1006-Reproduktion.

## Zusammenfassung

- Alle drei MSBuild-Aufrufe (`Invoke-Build`, `Invoke-Run`,
  `Invoke-DeviceViaSsh`) speisen Eigenschaften ueber eine Response-Datei
  ein (`Add-PropertyLine` → `-p:Name="Wert"`, `New-ResponseFile` →
  `[System.IO.File]::WriteAllLines`, Aufruf `dotnet … @rsp`).
- `Add-PropertyLine` prueft den Wert nicht: kein Trimmen, keine Pruefung
  auf Zeichen, die den MSBuild-Response-Datei-Parser brechen.
- **MSB1006 programmatisch reproduziert** (dotnet SDK 10.0.401, minimales
  Test-csproj): Der Fehler tritt auf, wenn ein `-p:`-Argument ohne
  gueltigen Eigenschaftsnamen beim Parser ankommt — konkret erzeugbar
  ueber ein `"` im Wert (je nach Position) oder einen Zeilenumbruch,
  dessen Folgezeile als eigenes `-p:`-Argument gelesen wird. Details in
  `inventory/msb1006-repro.md`.
- Secrets (`ServerPassword`, `VideoPlayerApiToken`) werden in der
  Konsolenanzeige bereits maskiert (`Add-MaskedDisplay`); beim
  Build-Fehlschlag werden die Response-Zeilen aber gar nicht gezeigt
  (kein maskierter Fehlschlag-Output). `Invoke-DeviceViaSsh` hat noch
  nicht einmal eine Display-Liste.
- Test-Ausgangszustand: **883/883 erfolgreich** (Nachweis:
  `inventory/tests.md`).

## Details

- [Skript-Analyse und Aufrufkette](inventory/script-analysis.md)
- [MSB1006-Reproduktion](inventory/msb1006-repro.md)
- [Test-Ausgangszustand](inventory/tests.md)
