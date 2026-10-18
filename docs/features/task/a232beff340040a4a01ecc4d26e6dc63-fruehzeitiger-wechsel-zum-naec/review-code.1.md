# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### PlayerPage.xaml.cs (PlayerPage)

- **Fehlerbehandlung** — `appliedMediaId` wird in `UpdateUi` (Z. 260) optimistisch gesetzt, bevor die Quelle am Element erfolgreich angewendet wurde (tatsächliches Setzen erst Z. 287, Fehlschlag-Pfade Z. 281/293). Die Begründung im Kommentar („auf den Fehlschlag-Pfaden … läuft der Takt dann ohnehin nicht — keine Wiedergabe → kein ReportPosition") trifft nicht zu: Schlägt `MediaSource.FromUri`/die Zuweisung fehl, während das Element das alte Medium weiter abspielt, läuft der Takt weiter (`CurrentState == Playing`) und meldet Positionswerte des Altmediums bereits unter der neuen `MediaId`. Da `player.MediaId` im Modell ebenfalls schon die neue Identität trägt, kann der neue Identitäts-Guard in `UpdatePlaybackProgress` diese Alt-Takte nicht mehr verwerfen — auf Fehlschlag-Pfaden ohne Folge-Episode (Fehlerzustand, MediaId unverändert, `IsResolving` greift nicht) kann so die Streak des Altmediums-Endfensters die neue Episode fälschlich als „angesehen" markieren samt `DeleteWatchedDownloadEffect`.

  Empfehlung: `appliedMediaId` erst nach erfolgreichem `VideoElement.Source = …` zuweisen (bei `location is null` weiterhin 0; im Fehlschlag-Fall bleibt die alte Id stehen und der Guard verwirft die Alt-Takte). Alternativ zumindest den Kommentar korrigieren — die tatsächliche Absicherung auf Fehlschlag-Pfaden leistet `IsResolving`, nicht „kein Takt".

### AppUpdate.cs (AppUpdate) / PlayerPage.xaml.cs (PlayerPage)

- **Doppelter Code** — Die Endfenster-Berechnung `TimeSpan.FromSeconds(Math.Max(Settings.WatchedEndWindowSeconds, Settings.AutoResumeRemainingSeconds))` ist nahezu identisch an zwei Stellen implementiert: `AppUpdate.UpdatePlaybackProgress` (Z. 3852–3854) und `PlayerPage.ReportPosition` (Z. 186–188). Divergiert die Fensterdefinition an einer Stelle, wertet das Diagnose-Logging ein anderes Fenster aus als der MVU-Kern.

  Empfehlung: Die Ableitung an einer gemeinsamen Stelle kapseln (z. B. berechnete Property `EndWindow` auf `AppSettings` oder eine kleine statische Hilfsmethode im MVU-Kern) und an beiden Stellen verwenden.

### AppUpdate.cs (AppUpdate)

- **God-Methode** — `UpdatePlaybackProgress` (Z. 3811–3932, ~120 Zeilen) behandelt mittlerweile sechs konzeptuell getrennte Schritte hintereinander: Aktivitäts-/Identitäts-Guard, State-Abbildung, Fortschritts-Throttling, Streak-Auswertung, „angesehen"-Schwelle samt Download-Löschung und zwei Advance-Verzweigungen. Die Änderung hat die Methode um ~40 Zeilen verlängert.

  Empfehlung: Den neuen Endfenster-Block (Streak-Auswertung plus die drei schwellenbasierten Auslöser, Z. 3849–3927) in eine eigene private Methode auslagern — analog zur bestehenden Auslagerung `IsProgressTickPlausible` —, sodass die Methode wieder den Takt-Grundablauf abbildet.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `VideoPlayer.Mvu/AppUpdate.cs`
- `VideoPlayer.Mvu/AppMessage.cs`
- `VideoPlayer.Mvu/PlayerState.cs`
- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`
- `VideoPlayer.Maui.Tests/AppUpdateTests_Player.cs`
- `VideoPlayer.Maui.Tests/AppUpdateTests_PlaylistPlayback.cs`
- `VideoPlayer.Maui.Tests/AppUpdateTests_Downloads.cs`
- `VideoPlayer.Maui.Tests/PlayerContractTests.cs`
