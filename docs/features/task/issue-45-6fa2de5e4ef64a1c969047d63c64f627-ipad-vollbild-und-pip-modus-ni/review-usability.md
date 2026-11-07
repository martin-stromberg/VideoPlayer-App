# Usability-Review

## Ergebnis

**Status:** Keine Befunde

Alle geforderten Interaktionen sind über sichtbare, verständlich
beschriftete Bedienelemente erreichbar. Das Kopfband
(`OverlayChrome`) ist nun am Elternlayout eingabedurchlässig
(`InputTransparent="True"`) und enthält ausschließlich nicht-
interaktive Anzeigeelemente — keine Bedienfunktion geht verloren.
Unter iOS werden Kopfband und Playlist-Navigation um den Versatz
`IosNativeControlsClearance` unter die native Top-Leiste des
`AVPlayerViewController` geschoben, sodass die eingeblendeten
Vollbild-/PiP-Schaltflächen weder visuell überdeckt noch in ihrer
Eingabe abgefangen werden. Die Playlist-Navigation
(„Zurück"/„Weiter") bleibt ein eigenes, nicht eingabedurchlässiges
Element und damit bedienbar. Der Windows-Pfad ist unverändert.

Verifikationshinweis (kein Usability-Befund): Die Höhe des iOS-
Versatzes (96 Punkte) ist im Code ausdrücklich als Schätzwert
markiert und gemäß Anforderung in der manuellen TestFlight-
Geräteprüfung auf dem iPad zu bestätigen — eine statische Prüfung
kann die tatsächliche Position der nativen Leiste nicht verifizieren.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Vollbild-Schaltfläche der nativen iOS-Transportsteuerung auf dem
  iPad antippen → unauffällig (Kopfband eingabedurchlässig und unter
  die native Leiste verschoben)
- PiP-Schaltfläche der nativen iOS-Transportsteuerung auf dem iPad
  antippen → unauffällig (gleicher Bereich, gleiche Korrektur)
- Titel, Playlist-Kontext und „Angesehen"-Badge in der Vollansicht
  ablesen → unauffällig (bleiben sichtbar, nur unter iOS um den
  Versatz tiefer positioniert; im Mini-Player weiterhin ausgeblendet)
- Playlist-Navigation „Zurück"/„Weiter" antippen → unauffällig
  (eigenes `PlayerRoot`-Element außerhalb des eingabedurchlässigen
  Bands, kein `InputTransparent`, unter iOS mitverschoben und damit
  weiterhin frei von der nativen Leiste)
- Mini-Player-Schalter und Transportsteuerung unter Windows
  bedienen → unauffällig (Pfad unverändert, `PipButton` bleibt
  Windows-only)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `VideoPlayer.Maui/Pages/PlayerPage.xaml`
- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`
- `VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`
