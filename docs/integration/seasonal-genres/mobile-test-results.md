# Mobile-Testergebnisse: Saisonale Genre-Listen

Stand: 2026-10-26. Frühere Mobile-Unit-, Build- und Servernachweise stehen im
Feature-Testbericht und im Server-Testbericht.

## Aktueller Nachweis nach Playlist-Testfix

Der letzte vollständige Windows-E2E-Lauf bestand mit **70/72, 0 Skips, Exitcode 1**; alle sechs saisonalen Flüsse waren grün. Der zuvor reproduzierbare Playlist-Verschiebefall bestand nach einer überprüften ActionSheet-Synchronisierung. Der Playlist-Löschfall scheiterte im Gesamtlauf an einem UIA-Timeout und bestand isoliert. Im LogProtocol-Fluss beendete sich die MAUI-App mit `0xC0000005` (PID 57880); isoliert bestand auch dieser Fall. Der volle Lauf bleibt rot. Artefakte: `.work/test-results/final-post-playlist-20261026/`. Dumpanalyse und unerledigte Diagnose stehen im erhaltenen Feature-Verzeichnis.
## Vorheriger guarded Windows-E2E-Vollrun am 2026-10-26

Die vollständige Suite lief mit PID-genauer ProcDump-Überwachung aller eigenen App-Prozesse: **70 bestanden, 2 fehlgeschlagen, 0 übersprungen, 72 gesamt**, laut TRX `Failed` (9:20). Alle sechs saisonalen E2E-Flüsse bestanden. 77 Workspace-App-PIDs wurden überwacht.

- `PlaylistManagementTests.MoveEntries_ReordersOnServer`: Timeout; einmal isoliert wiederholt und erneut fehlgeschlagen (Exit 1).
- `ContinueWatchingContextMenuTests.LongPressMovie_ChooseSkip_CallsSkipEndpoint_EntryDisappears`: `Assert.NotNull()`; einmal isoliert wiederholt und bestanden (Exit 0).

Die Wiederholungen ändern den Vollsuite-Stand nicht. Keine Produkt-/Teständerung, Abschwächung oder Skips; kein weiterer Vollrun.

ProcDump erstellte für PID 27688 einen vollständigen Dump (789 MB) nach `Unhandled: C000027B`; die Prozessdaten notieren später einen erzwungenen App-Shutdown. In diesem Lauf trat kein `0xC0000005` auf. Der Dump und sämtliche Logs bleiben lokal unter `.work/test-results/full-final-20261026/`; es wurde nichts hochgeladen. Der dotnet-Prozess-Exitcode aus dem Monitor war 0, obwohl TRX/Console den Testlauf als fehlgeschlagen ausweisen; Ergebnisstatus 70/2.

## Guarded Windows-E2E-Suite

Befehl:

```text
dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --no-restore --logger "console;verbosity=normal" --logger "trx;LogFileName=final-guarded-20261026.trx" --results-directory .work/test-results/final-guarded-20261026
```

Ergebnis: **69 bestanden, 3 fehlgeschlagen, 0 übersprungen, 72 gesamt**; 8:03;
Exitcode 1. Alle sechs saisonalen E2E-Flüsse bestanden. Der zuvor fehlgeschlagene
Playlist-Genre-Editor-Fall bestand diesmal.

Die drei Fehler wurden einzeln wiederholt:

- `ContinueWatchingContextMenuTests.LongPressMovie_ChooseSkip_CallsSkipEndpoint_EntryDisappears`
  scheiterte im Vollsuite-Lauf mit `FlaUI.Core.Exceptions.NoClickablePointException`;
  isoliert 1/1 bestanden.
- `PlaylistProgressReportTests.Playback_ProgressReport_WithoutPlaylist_HasNullPlaylistId`
  scheiterte mit `Assert.NotNull()`; isoliert 1/1 bestanden.
- `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn` endete mit einem
  bestätigten App-Absturz: PID 45732, Exit `0xC0000005`, Profil
  `e2e-submittedtextremains-df9b7ff6bfda49d2a9c98c9c28086e99`. Im Prozess-JSON
  stehen `killAttempted=false`, `forcedKill=false`, `exitedBeforeShutdown=false`
  und `outputDrained=true`. `stderr.log` enthält den nativen WinRT-Finalizerstack
  über `Marshal.Release` und `GC.RunFinalizers`; `stdout.log` ist leer. Zwei
  anschließende isolierte Wiederholungen bestanden jeweils 1/1 ohne Crash.
  Ursache ungeklärt.

Die Wiederholungen ändern das Vollsuite-Ergebnis nicht. Es gab keinen Produktfix,
keine Testabschwächung und keine Skips. Für einen sauberen 72/72-Lauf und die
Erklärung des App-Crashs liegt daher noch kein Nachweis vor.

Console-Log, TRX, Exitcodes und Wiederholungen liegen lokal unter
`.work/test-results/final-guarded-20261026/`. Prozessdaten der App liegen unter
`.work/e2e-process-logs/20261026-062851082-45732/`; diese temporären Artefakte
sind nicht Teil dieser dauerhaften Dokumentation.
