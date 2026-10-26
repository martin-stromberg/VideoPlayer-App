# Abschlussprüfungen – Saisonabhängige Genre-Listen

**Aktueller Status (2026-10-26): Beide Such-E2E bei gezielter Wiederholung bestanden; vollständige Windows-E2E-Nachprüfung weiterhin offen.**

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
