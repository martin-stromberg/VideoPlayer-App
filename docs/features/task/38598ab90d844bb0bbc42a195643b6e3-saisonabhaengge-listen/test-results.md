# Abschlussprüfungen – Saisonabhängige Genre-Listen

**Aktueller Status (2026-10-26): letzter vollständiger Windows-E2E-Lauf 70/72, 0 Skips, Exitcode 1; alle sechs saisonalen Flüsse bestanden. Zwei bestehende Fehlerfälle bestanden jeweils isoliert, der native App-Abbruch trat erneut auf.**

## Neuester vollständiger Windows-E2E-Lauf nach Playlist-Synchronisierung

Nach dem unabhängig geprüften `WaitForNameAbsent`-Testhelper wurde die gesamte 72er-Suite einmal mit dem bestehenden App-Exit-Guard ausgeführt: **70 bestanden, 2 fehlgeschlagen, 0 übersprungen, Exitcode 1**, Dauer 12m43s. Alle sechs `SeasonalGenresTests` bestanden. Der zuvor reproduzierbar fehlgeschlagene Verschiebetest bestand nach der Korrektur zwei isolierte Läufe und im Gesamtlauf; drei benachbarte Entfernen-Szenarien bestanden ebenfalls.

- `PlaylistManagementTests.EditAndDeletePlaylist_ViaManageMenu_UpdatesServerAndNavigatesBack`: UIA-Timeout beim Menüpunkt „Löschen“ im Gesamtlauf; eine isolierte Wiederholung bestand 1/1 mit App-Exit 0.
- `LogProtocolTests.LoggingEnabled_LogoutAndPipSubFlows_WriteDiagnosticEntries`: MAUI-App PID 57880 beendete sich mit `0xC0000005`; stderr enthält „The specified network name is no longer available.“ Eine isolierte Wiederholung bestand 1/1 mit App-Exit 0. Die native Ursache bleibt ungeklärt.

Console/TRX und beide isolierten TRX-Dateien liegen ignoriert unter `.work/test-results/final-post-playlist-20261026/`; Prozessdaten unter `.work/e2e-process-logs/20261026-140654595-57880/`. Ein älterer 789-MB-Dump zu `C000027B` zeigte eine WinRT-Stowed-Exception mit `E_UNEXPECTED` im UI-Messaging-Pfad nach Beginn des Fixture-Shutdowns; der Zusammenhang mit `0xC0000005` ist nicht belegt. Zwölf PID-genau überwachte Suchtest-Wiederholungen plus ein gemeinsamer Lauf (6/6) reproduzierten `0xC0000005` nicht. Details in `native-dump-investigation.md` und `native-dump-analysis.md`. Aus den bestandenen Einzelwiederholungen folgt kein grüner 72er-Gesamtlauf.
## Vorheriger guarded Windows-E2E-Vollrun am 2026-10-26

Die angeforderte vollständige 72er-Suite lief mit PID-genauer ProcDump-Überwachung aller eigenen `VideoPlayer.Maui.exe`-Prozesse. Ergebnis laut Console und TRX: **70 bestanden, 2 fehlgeschlagen, 0 übersprungen, 72 gesamt**, Laufzeit 9:20, TRX-Outcome `Failed`. Alle sechs `SeasonalGenresTests` bestanden. Der Monitor erkannte 77 Workspace-App-PIDs und erstellte einen vollständigen Dump.

Fehler und je eine isolierte Wiederholung:

- `PlaylistManagementTests.MoveEntries_ReordersOnServer`: Timeout beim Verschieben von Film A; isolierte Wiederholung **1/1 fehlgeschlagen**, Exitcode 1.
- `ContinueWatchingContextMenuTests.LongPressMovie_ChooseSkip_CallsSkipEndpoint_EntryDisappears`: `Assert.NotNull()` für den Continue-Watching-Eintrag; isolierte Wiederholung **1/1 bestanden**, Exitcode 0.

Die Wiederholungen ändern das Vollsuite-Ergebnis nicht. Keine Tests wurden übersprungen oder abgeschwächt, kein Produkt-/Testcode wurde geändert. Es gab keine weitere Vollsuite-Wiederholung.

ProcDump schrieb für Workspace-App-PID 27688 (`e2e-selectentrystartspla-df49504e6dbc451eac155d6df5479f4e`) einen vollständigen 789-MB-Dump. Sein Protokoll meldet `Unhandled: C000027B`; die E2E-Prozessdaten verzeichnen anschließend einen versuchten und erzwungenen App-Shutdown. Ein `0xC0000005` trat in diesem Vollrun nicht auf. Der Dump klärt die Ursache nicht; die native Auswertung bleibt offen. Das vom `Start-Process`-Monitor gemeldete dotnet-Prozess-Exitcodefeld war 0, obwohl Console und TRX den Lauf als fehlgeschlagen ausweisen; maßgeblich für den Teststatus sind die 70/2-Resultate im TRX.

Artefakte liegen lokal und ignoriert unter `.work/test-results/full-final-20261026/`: `console.log`, `full-final-20261026.trx`, `exit-code.txt`, `monitor-status.txt`, je-PID-Protokolle und `dumps-27688/`. Die beiden isolierten Wiederholungen liegen in `recheck/`. Es wurden keine Dumps hochgeladen.

## Guarded Windows-E2E-Lauf am 2026-10-26

Nach unabhängiger Prüfung der E2E-Crash-/Cleanup-Änderungen wurde die volle 72er-Suite erneut mit `--no-restore`, normaler Console-Ausgabe und TRX-Logger ausgeführt:

```text
dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --no-restore --logger "console;verbosity=normal" --logger "trx;LogFileName=final-guarded-20261026.trx" --results-directory .work/test-results/final-guarded-20261026
```

Ergebnis: **69 bestanden, 3 fehlgeschlagen, 0 übersprungen, 72 gesamt**, 8:03, Prozess-Exitcode **1**. Alle sechs `SeasonalGenresTests` bestanden. `PlaylistManagementTests.SetAndResetGenres_UpdatesServer`, das im vorigen Lauf fehlgeschlagen war, bestand diesmal.

Fehler und gezielte Wiederholungen:

- `ContinueWatchingContextMenuTests.LongPressMovie_ChooseSkip_CallsSkipEndpoint_EntryDisappears`: `FlaUI.Core.Exceptions.NoClickablePointException` bei `UiDriver.ClickByName` (Testzeile 74). Isolierte Wiederholung: **1/1 bestanden**, Exit 0.
- `PlaylistProgressReportTests.Playback_ProgressReport_WithoutPlaylist_HasNullPlaylistId`: `Assert.NotNull()` fehlgeschlagen (Zeile 159). Isolierte Wiederholung: **1/1 bestanden**, Exit 0.
- `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn`: guarded Test meldete unerwarteten MAUI-App-Prozess **PID 45732**, Exit `-1073741819` (`0xC0000005`), Profil `e2e-submittedtextremains-df9b7ff6bfda49d2a9c98c9c28086e99`. Prozessdaten: `killAttempted=false`, `forcedKill=false`, `exitedBeforeShutdown=false`, Shutdown begann `2026-10-26T06:28:50.708399Z`, Ausgabe vollständig gelesen. `stderr.log` enthält `Fatal error. 0xC0000005` im Stack `Marshal.Release` → `WinRT.IObjectReference.DisposeTrackerSource` → `Dispose` → `Finalize` → `GC.RunFinalizers`; `stdout.log` ist leer. Das ist ein bestätigter App-Prozessabsturz während des Testablaufs, keine erfolgreiche Testbereinigung. Zwei anschließende isolierte Wiederholungen bestanden jeweils (**1/1**, Exit 0) und zeigten keinen erneuten Crash. Die Ursache ist damit nicht behoben oder erklärt.

Die drei Einzelfall-Wiederholungen bestehen nur zur Reproduzierbarkeit; sie ändern das Vollsuite-Ergebnis **69/72** nicht. Kein Produktfix, keine Abschwächung und kein Skip. Die Crashursache bleibt ungeklärt.

Artefakte: `.work/test-results/final-guarded-20261026/` enthält vollständige Console/TRX/Exitcodes und die gefilterten Wiederholungen. PID-bezogene Daten des Absturzes: `.work/e2e-process-logs/20261026-062851082-45732/{process.json,stderr.log,stdout.log}`. Eine cleanup-feste Zusammenfassung liegt in `docs/integration/seasonal-genres/mobile-test-results.md`.

## Vollständige Windows-E2E-Nachprüfung am 2026-10-26

Ausgeführt wurde die angeforderte vollständige Suite mit `--no-restore`, normaler Console-Ausgabe und TRX-Logger:

```text
dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --no-restore --logger "console;verbosity=normal" --logger "trx;LogFileName=final-20261026.trx" --results-directory .work/test-results/final-20261026
```

Ergebnis: **71 bestanden, 1 fehlgeschlagen, 0 übersprungen, 72 gesamt**, Laufzeit 9:09, Prozess-Exitcode **1**. Die sechs `SeasonalGenresTests` und beide zuvor fehlgeschlagenen `SourceSearchTests` bestanden.

Einziger Testfehler: `PlaylistManagementTests.SetAndResetGenres_UpdatesServer` nach Timeout beim erneuten Öffnen des Genre-Editors. Letzter Modellzustand: Seite `Playlists`, Playlist-Genretext `Komödie, Drama`, Änderungsmitteilung „Die Genres wurden aktualisiert.“, `playlistGenreEditorOpen=false`, Optionen leer. Stack: `DevApiClient.WaitForModelAsync` Zeile 138, Test Zeile 493.

Der fehlgeschlagene Playlistfall wurde einmal isoliert wiederholt: **1 bestanden, 0 fehlgeschlagen, 0 übersprungen**, 13,5 s, Exitcode 0. Damit ist der Suite-Fehler nicht reproduziert; die ursprüngliche Abweichung bleibt dokumentiert, es wurde kein Fix vorgenommen.

Der native WinRT-Finalizerfehler `0xC0000005` trat im Vollsuite-Log erneut auf (`Marshal.Release` → `WinRT.IObjectReference.DisposeTrackerSource` → `Dispose` → `Finalize` / `GC.RunFinalizers`). Die Ausgabe enthält keine Prozess-ID und erlaubt keine belastbare Zuordnung zu Testhost oder MAUI-App. Der Testhost setzte die Suite fort und schloss sie mit den oben genannten Testergebnissen ab.

Artefakte: `.work/test-results/final-20261026/console.log`, `final-20261026.trx`, `exit-code.txt`; isolierte Wiederholung unter `.work/test-results/final-20261026/recheck/`. Eine kompakte, cleanup-feste Zusammenfassung liegt in `docs/integration/seasonal-genres/mobile-test-results.md`.

## Mobile UnitSuite und Windows-Build

- Mobile UnitSuite: **1396 bestanden, 0 Fehler, 0 übersprungen** (50 s). Seit diesem erfolgreichen Lauf gab es keine dauerhafte Mobile-Produktänderung; deshalb keine Wiederholung.
- Windows-MAUI-Build: erfolgreich, **0 Fehler**; vorhandene Warnungen NU1903 (`Microsoft.AspNetCore.SignalR.Common` 10.0.10) und CS0618 (`Page.DisplayAlert`).

## Windows-E2E

- Vollständige Suite aus Iteration 2: **70 bestanden, 2 fehlgeschlagen, 0 übersprungen, 72 gesamt** (9 m 15 s). Alle sechs saisonalen Flüsse bestanden.
- Nachprüfung Iteration 3: `SourceSearchTests` plus `SeasonalGenresTests`: **7 bestanden, 2 fehlgeschlagen, 0 übersprungen, 9 gesamt** (2 m 03 s). Alle sechs saisonalen E2E und `UnsubmittedInput_SurvivesModelTicks` bestanden.

Offen bleiben `SourceSearchTests.SubmitViaEnter_FiltersFromFirstPage` und `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn`. Die native SearchBar-TextBox enthielt bei der UIA-Diagnose bereits `Komöd`, hatte aber keinen Tastaturfokus. Stattdessen hielt ein fremder `Shell_SystemDialog`-Popup den Fokus; dessen Prozess wurde als `PickerHost` (PID 11252) identifiziert. Die globale Return-Eingabe erreichte daher die App nicht. Versuche, Appfenster und inneres Edit per UIA oder echtem Klick zu fokussieren, änderten den Fokus nicht. Der fremde Systemdialog wurde nicht geschlossen oder bedient. Die Diagnoseänderungen am `UiDriver` wurden vollständig zurückgenommen; keine Assertions wurden abgeschwächt, keine Tests übersprungen und kein Produkt- oder Testfix blieb bestehen.

Der gemeinsame Iteration-3-Lauf meldete zusätzlich einen nativen WinRT-Finalizerfehler `0xC0000005`; alle neun Testfälle erhielten dennoch ein Ergebnis. Dieser Prozessabbruch ist für eine spätere Windows-Testsession nacharbeitsbedürftig. Der native Return-Pfad kann erst mit tatsächlichem App-Tastaturfokus abschließend beurteilt werden.

Eine weitere Wiederholung erfolgte in diesem Abschlussbericht-Schritt nicht: Es gab keine dauerhafte Codeänderung und keinen technischen Grund, der die bereits belegte Fokusblockade verändert hätte. Erneut auszuführen sind die beiden Suchfälle in einer Windows-Testsitzung ohne fremden Systemdialog als Fokusinhaber; danach gegebenenfalls die Vollsuite.

## Servernachweise

Keine Server-Suites erneut ausgeführt. Der dauerhafte [Server-Testbericht](../../../integration/seasonal-genres/server-test-results.md) und die [unabhängige Abnahme](../../../integration/seasonal-genres/acceptance-schritt-1.md) dokumentieren Debug-/Release-Build, die vollständige Suite (**2358 bestanden, 2 bestehende Skips**) sowie unabhängige gezielte Saisonprüfungen (**12/12**, darunter vier reale Web-E2E-Flüsse und Mutationstests).

## Gezielte Nachprüfung am 2026-10-26

Unveränderter Produkt- und Teststand, gefilterter Lauf der beiden zuvor fehlgeschlagenen Suchtests:

- `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn`: bestanden (32 s).
- `SourceSearchTests.SubmitViaEnter_FiltersFromFirstPage`: bestanden (4 s).
- Gesamt: **2 bestanden, 0 Fehler, 0 Skips**, 39,1 s, Exitcode 0. Keine WinRT-Finalizerfehlermeldung in diesem Lauf.

Befehl: `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --no-restore --filter "FullyQualifiedName~SourceSearchTests.SubmitViaEnter_FiltersFromFirstPage|FullyQualifiedName~SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn"` mit TRX-Logger. Lokales Ergebnis: `.work/test-results/search-recheck-20261026/search-recheck.trx`.

Vor dem Lauf lief kein PickerHost-Prozess; die anschließende Fensterliste zeigte keinen entsprechenden Dialog. Eine Firewallabfrage ist nicht nachgewiesen. Die ältere Fokusbeobachtung wird als historische Diagnose erhalten, nicht als abschließend bewiesene Ursache. Die vollständige 72er-Suite wurde auf diesen gezielten Nutzerauftrag hin nicht wiederholt; aus den zwei bestandenen Wiederholungen wird kein neuer vollständiger 72/72-Lauf abgeleitet.
