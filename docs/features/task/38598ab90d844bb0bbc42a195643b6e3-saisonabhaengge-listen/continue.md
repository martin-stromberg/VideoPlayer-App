# Offene Aufgaben

Erstellt am: 2026-10-25
Abbruchgrund: Maximale Iterationsanzahl erreicht (3); fremder Systemdialog blockiert Tastaturfokus.

## Offene Planelemente

Keine. Saisonale Server-/Web-/Mobilefunktion vollständig umgesetzt und unabhängig geprüft.

## Code-Review-Befunde

Keine.

## Usability-Befunde

Keine.

## Fehlgeschlagene Tests

- [x] `SourceSearchTests.SubmitViaEnter_FiltersFromFirstPage` am 2026-10-26 unverändert erneut ausgeführt: bestanden.
- [x] `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn` am 2026-10-26 unverändert erneut ausgeführt: bestanden.
- [x] Vollständige guarded Windows-E2E-Suite nach der Testinfrastrukturänderung ausgeführt: 69/72, 0 übersprungen; alle sechs saisonalen Tests bestanden. Drei Fehlerfälle wurden gezielt wiederholt: zwei bestanden isoliert, der Suchtest bestand nach einem erfassten App-Crash auch in zwei Wiederholungen. Der Vollsuite-Fehlerstand bleibt 69/72. Details in `test-results.md`.
- [x] Native WinRT-Absturzüberwachung in der guarded Suite geprüft: `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn` erfasste App-PID 45732 mit Exit `0xC0000005`; PID-Prozessdaten und stderr bewahren den Finalizerstack. Zwei isolierte Wiederholungen bestanden, die Ursache ist aber nicht erklärt oder behoben. Keine Testabschwächung oder Produktkorrektur. Details in `test-results.md`.

## Noch offene Abschlusskriterien (2026-10-26)

- [ ] Vollständige Windows-E2E-Suite mit 72/72, Exitcode 0 und ohne unerwarteten App-Abbruch erfolgreich ausführen. Letzter bewachter Gesamtlauf am 2026-10-26: 70/72, 0 übersprungen; Playlist-Löschen und LogProtocol bestanden anschließend jeweils isoliert. Dies ersetzt keinen grünen Gesamtlauf.
- [ ] Nativen Shutdown-Abbruch der MAUI-App (`0xC0000005`, zuletzt PID 57880) anhand eines passenden nativen Crashdumps eingrenzen und einen belegbaren Fix prüfen. Der vorhandene Dump eines anderen Laufs zeigt `C000027B`; sein Zusammenhang mit dem Access Violation-Abbruch ist nicht belegt. Details in `native-dump-investigation.md` und `native-dump-analysis.md`.
## Hinweise zur Fortsetzung

Produktcode und Testtreiber wurden in Iteration 3 nicht verändert; erfolglose Fokusversuche wurden vollständig zurückgenommen. Bestehende Nachweise: 1396 mobile Unit-Tests bestanden, Windows-Build erfolgreich; Server Debug/Release und vollständige Suite 2358 bestanden/2 bestehende Skips, vier echte Web-E2E bestanden und unabhängige Mutationstests erfolgreich. Details in `test-results.md` und `implementation-followup-3.md`.
## Automatische Fortsetzungsprüfung

Geprüft am: 2026-10-25 20:50:05 +01:00

Einstiegspunkt war Schritt 6 aufgrund dieser vorhandenen Datei. Branch und sauberer Arbeitsstand wurden geprüft; Dokumentation und geprüfter Produktstand sind vorhanden. Die Read-only-Prüfung Get-Process -Name PickerHost sowie Get-Process -Id 11252 bestätigt, dass derselbe dokumentierte PickerHost-Prozess mit PID 11252 weiterhin existiert. Die vorliegende Fokusdiagnose bleibt damit ungeklärt; eine erneute native Fokusmessung oder sichere Freigabe des App-Tastaturfokus wurde nicht nachgewiesen.

Die Fortsetzung wird vor weiteren Reviews/Tests gemäß Implementierungsabbruchregel beendet: Es gibt keine geänderte externe Voraussetzung und keinen begründeten Produktfix. Der fremde Dialog wird nicht bedient oder geschlossen. Keine Tests werden ohne veränderte Grundlage erneut gestartet; keine Produkt-/Testdateien werden verändert. Alle drei offenen Nacharbeiten bleiben unverändert. Vor einem neuen Testlauf muss eine freie interaktive Windows-Testsitzung bereitstehen. Dieser Lauf ist bereits die automatische Fortsetzung; es wird keine weitere automatische Fortsetzung ausgelöst.

## Gezielte Nachprüfung am 2026-10-26

Auf Nutzerauftrag wurden die beiden zuvor fehlgeschlagenen Such-E2E ohne Code- oder Teständerung erneut ausgeführt: **2 bestanden, 0 Fehler, 0 Skips**, Gesamtdauer 39,1 Sekunden, Prozess-Exitcode 0. Der zuvor gemeldete WinRT-Finalizerfehler trat in diesem Lauf nicht auf; eine vollständige Suite wurde nicht wiederholt. Kein PickerHost-Prozess war vor dem Lauf vorhanden. Die anschließende Computer-Use-Fensterliste zeigte keinen PickerHost-/Firewalldialog; eine Firewallabfrage ist weiterhin nicht nachgewiesen. Die historische Fokusbeobachtung allein beweist die damalige Fehlerursache nicht.

Maschinenlesbares Ergebnis lokal unter `.work/test-results/search-recheck-20261026/search-recheck.trx`; dauerhafte Zusammenfassung in `test-results.md`. Die beiden konkreten Suchfehler sind damit erfolgreich nachgeprüft. Die vollständige Suite sowie die frühere WinRT-Prozessmeldung bleiben als gesonderte Nacharbeit erhalten.

## Letzter Abschlusslauf (2026-10-26)
- [x] Einen letzten vollständigen guarded Windows-E2E-Lauf mit PID-genauem ProcDump für Workspace-App-PIDs ausgeführt und Console/TRX/Dump lokal gespeichert. Ergebnis **70/72**, 0 Skips; beide Fehler je einmal isoliert wiederholt (einer erneut fehlgeschlagen, einer bestanden). Die Bedingung 72/72 bleibt offen; nativer Dump PID 27688 meldet `C000027B`, nicht `0xC0000005`. Details in `test-results.md`.
## Offene Punkte nach der letzten Fortsetzung

- [ ] Vollständige Windows-E2E-Suite mit 72/72 und Exitcode 0 nachweisen. Der letzte Lauf nach dem geprüften Playlist-Testfix endete mit 70/72; Playlist-Löschen und LogProtocol bestanden nur in isolierten Wiederholungen. Alle sechs saisonalen E2E-Flüsse sind weiterhin grün.
- [ ] Sporadischen MAUI-Abbruch `0xC0000005` beim Fixture-Shutdown eingrenzen und belegt beheben. Er trat zuletzt im LogProtocol-Fluss (App-PID 57880) auf. Der lokal gesicherte Dump eines anderen Laufs zeigt `C000027B`/`E_UNEXPECTED`; ein Zusammenhang ist nicht belegt. Kein Runtime-Fix ohne konkreten nativen Nachweis.

Diese Punkte wurden in der maximalen Lifecycle-Prüfschleife nicht abgeschlossen. Der Featureordner bleibt erhalten und Schritt 13 in `todo.md` offen. Die übrigen Anforderungs- und saisonalen E2E-Prüfungen sind dokumentiert.
