# Implementierung: App-begrenzte Desktop-Suche

`UiDriver.ClickByName` und `WaitForNameAbsent` prüfen weiterhin zuerst Nachfahren des Hauptfensters. Der Fallback sucht jetzt nur in direkten Desktop-Kindern mit der App-PID. Pro Kind prüft er dessen eigenen Namen und danach seine Nachfahren. Damit wird die vorherige Suche durch alle fremden Desktop-Fensterbäume vermieden. Retry-Intervalle und Timeouts blieben unverändert.

## Gezielte Prüfung am 2026-10-26

`dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --no-restore --filter "FullyQualifiedName~SetAndResetGenres_UpdatesServer|FullyQualifiedName~LongPressMovie_Cancel_KeepsEntry_NoMutationRequests"` bestand mit **2/2**, 0 Fehlern und Exitcode 0 (18,5 Sekunden). Die TRX-Datei liegt lokal unter `.work/test-results/bounded-desktop-targeted-20261026/bounded-desktop-targeted.trx`.

Der Erfolg belegt die beiden Benutzerflüsse unter dieser Änderung, aber nicht, dass der Desktop-Fallback in jedem Klick tatsächlich benutzt wurde oder dass UIA-Aufrufe nie blockieren. Ein vollständiger Windows-E2E-Lauf bleibt als Abschlussprüfung offen.

## Vollständige Nachprüfung

Der anschließend ausgeführte vollständige Lauf bestand 70/72 Tests (0 Skips, Exitcode 1). Beide gezielt geprüften Flüsse, alle Playlist-Management- und Continue-Watching-Kontextmenütests sowie alle sechs saisonalen Tests bestanden. Zwei andere Tests scheiterten durch native MAUI-Shutdown-Abbrüche `0xC0000005` im WinRT-Finalizer. Diese bleiben als eigenes Abschlussproblem offen; Details stehen in `test-results.md`.
