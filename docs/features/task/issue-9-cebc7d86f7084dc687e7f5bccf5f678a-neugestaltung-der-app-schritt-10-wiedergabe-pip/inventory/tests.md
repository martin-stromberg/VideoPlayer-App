# Bestandsaufnahme: Tests

## Ausgangszustand

- **Datum/Zeit:** 2026-09-22, ~13:15 (lokal, Windows)
- **Branch:** `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-10-wiedergabe-pip`
- **Commit:** `15f2d7db43c58c5e37de4de4b57a8f12ee9cc83e`
- **Uncommittete Änderungen:** `.gitignore` (fremde Änderung, wird
  nicht committed) sowie die neuen Feature-Artefakte unter
  `docs/features/task/…-schritt-10-wiedergabe-pip/`
- **SDK:** .NET 10.0.401, MAUI-Workloads installiert
- **Befehl:** `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --nologo`
- **Ergebnis:** Exit-Code 0 — **469 erfolgreich, 0 fehlgeschlagen,
  0 übersprungen** (Dauer ~3 s)
- **Nachweis:** [test-results/baseline-dotnet-test.log](test-results/baseline-dotnet-test.log)
  (Warnungen im Log: CS0108/CS8618 des Altbestands, CS1570 in
  `KnownServersService` — bekannte, nicht auf diesen Schritt
  zurückführbare Warnungen)

## Vorhandene Abdeckung (relevant für diesen Schritt)

- `AppUpdateTests_*` — MVU-Übergänge aller bisherigen Seiten
- `ProgressReportServiceTests` — Online-Meldung, Offline-Einreihung,
  permanente/überholbare Fehler
- `PendingProgressSyncServiceTests`, `ServerSyncServiceTests` —
  Warteschlange und Drain
- `MediaDetailsServiceTests`, `AppUpdateTests_MediaDetails` —
  Serienkontext der Detailseite
- `EnumCoverageTests` — textuelle Enum-Wert-Abdeckung (Hook-Vorgabe:
  jeder neue Enum-Wert muss in einer Testdatei vorkommen)
- `VideoPlayerMauiContractTests` — XAML-/Projekt-Verträge über
  Quelltext (das Testprojekt kann keine MAUI-Targets referenzieren)

## Testlücken für diese Anforderung

- Kein Player-State, keine Player-Messages/Effekte, keine Player-Seite —
  sämtliche Wiedergabe-Übergänge (Quellauflösung, Fortschrittstakt,
  „angesehen"-Schwelle, Auto-Fortsetzung, Fehler-Rückfall, PiP) sind
  neu zu testen.
- `PlaybackSourceService` (Stream-URL + Token, Fortsetzungsposition,
  lokale Datei) ist neu und testbar (plattformneutral in
  `VideoPlayer.Mvu`).
- Neuer Enum `PlaybackSourceKind` braucht textuelle Abdeckung.
- E2E-/Gerätetests existieren im Projekt nicht (keine Infrastruktur);
  der Benutzerfluss wird über MVU-Übergangstests (Dispatch-Kette inkl.
  Effekt-Handler-Aufrufe) und XAML-/Route-Contract-Tests abgesichert —
  im Plan entsprechend verankert.
