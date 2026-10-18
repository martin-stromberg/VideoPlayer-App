# Bestandsaufnahme: Altbestand VideoWebPlayer.Maui / VideoWebPlayer.Client entfernen

Analysiert wurde der Altbestand `VideoWebPlayer.Maui` / `VideoWebPlayer.Client` sowie alle Referenzen darauf aus Solution, verbleibenden Projekten, Tests, CI und Dokumentation — bezogen auf Issue #60.

## Zusammenfassung

- `VideoWebPlayer.Maui` (117 getrackte Dateien, ~47.300 Zeilen) und `VideoWebPlayer.Client` (12 Dateien, ~1.300 Zeilen) sind eigenständige Projekte in `VideoPlayer.App.sln`; `VideoWebPlayer.Maui` referenziert `VideoWebPlayer.Client` per `ProjectReference`.
- **Kein Produktivcode der neuen App** referenziert den Altbestand (`VideoPlayer.Maui`, `VideoPlayer.Mvu`, `VideoPlayer.ApiClient`, `VideoPlayer.Storage`, `msTools.DeveloperApi*` sind sauber).
- Eingehende Referenzen existieren nur aus `VideoWebPlayer.Maui.Tests` (Projektreferenz + 3 betroffene Testdateien), den drei GitHub-Workflows und der Dokumentation.
- `VideoWebPlayer.Maui.Tests` ist trotz des Namens das Testprojekt der **neuen** App — es referenziert `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`, `VideoPlayer.Storage`, `msTools.DeveloperApi` und bleibt erhalten.
- Die Release-Workflows veröffentlichen aktuell Artefakte des Altbestands (APK/MSIX/iOS-`.app`) — deren Entfernung verändert den Release-Output (offener Punkt).
- `VideoPlayer.E2E.Tests` hat keinerlei Altbestand-Bezug.

## Details

- [Altbestand: Struktur und Inhalt](inventory/altbestand.md)
- [Eingehende Referenzen](inventory/referenzen.md)
- [Betroffene Tests](inventory/tests.md)
