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
- [ ] Vollständige Windows-E2E-Suite nach der erfolgreichen gezielten Nachprüfung erneut abschließen. Historischer vollständiger Lauf: 70/72; die beiden damaligen Fehlerfälle sind jetzt separat bestanden.
- [ ] Im letzten gezielten Lauf zusätzlich gemeldeten WinRT-Finalizer-Prozessfehler `0xC0000005` in freier interaktiver Session prüfen; Ergebnis war 7/9 (einschließlich sechs bestandener saisonaler Tests). Keine Produktursache belegt; keine Testabschächung oder spekulative Korrektur vornehmen.

## Hinweise zur Fortsetzung

Produktcode und Testtreiber wurden in Iteration 3 nicht verändert; erfolglose Fokusversuche wurden vollständig zurückgenommen. Bestehende Nachweise: 1396 mobile Unit-Tests bestanden, Windows-Build erfolgreich; Server Debug/Release und vollständige Suite 2358 bestanden/2 bestehende Skips, vier echte Web-E2E bestanden und unabhängige Mutationstests erfolgreich. Details in `test-results.md` und `implementation-followup-3.md`.
## Automatische Fortsetzungsprüfung

Geprüft am: 2026-10-25 20:50:05 +01:00

Einstiegspunkt war Schritt 6 aufgrund dieser vorhandenen Datei. Branch und sauberer Arbeitsstand wurden geprüft; Dokumentation und geprüfter Produktstand sind vorhanden. Die Read-only-Prüfung Get-Process -Name PickerHost sowie Get-Process -Id 11252 bestätigt, dass derselbe dokumentierte PickerHost-Prozess mit PID 11252 weiterhin existiert. Die vorliegende Fokusdiagnose bleibt damit ungeklärt; eine erneute native Fokusmessung oder sichere Freigabe des App-Tastaturfokus wurde nicht nachgewiesen.

Die Fortsetzung wird vor weiteren Reviews/Tests gemäß Implementierungsabbruchregel beendet: Es gibt keine geänderte externe Voraussetzung und keinen begründeten Produktfix. Der fremde Dialog wird nicht bedient oder geschlossen. Keine Tests werden ohne veränderte Grundlage erneut gestartet; keine Produkt-/Testdateien werden verändert. Alle drei offenen Nacharbeiten bleiben unverändert. Vor einem neuen Testlauf muss eine freie interaktive Windows-Testsitzung bereitstehen. Dieser Lauf ist bereits die automatische Fortsetzung; es wird keine weitere automatische Fortsetzung ausgelöst.

## Gezielte Nachprüfung am 2026-10-26

Auf Nutzerauftrag wurden die beiden zuvor fehlgeschlagenen Such-E2E ohne Code- oder Teständerung erneut ausgeführt: **2 bestanden, 0 Fehler, 0 Skips**, Gesamtdauer 39,1 Sekunden, Prozess-Exitcode 0. Der zuvor gemeldete WinRT-Finalizerfehler trat in diesem Lauf nicht auf; eine vollständige Suite wurde nicht wiederholt. Kein PickerHost-Prozess war vor dem Lauf vorhanden. Die anschließende Computer-Use-Fensterliste zeigte keinen PickerHost-/Firewalldialog; eine Firewallabfrage ist weiterhin nicht nachgewiesen. Die historische Fokusbeobachtung allein beweist die damalige Fehlerursache nicht.

Maschinenlesbares Ergebnis lokal unter `.work/test-results/search-recheck-20261026/search-recheck.trx`; dauerhafte Zusammenfassung in `test-results.md`. Die beiden konkreten Suchfehler sind damit erfolgreich nachgeprüft. Die vollständige Suite sowie die frühere WinRT-Prozessmeldung bleiben als gesonderte Nacharbeit erhalten.
