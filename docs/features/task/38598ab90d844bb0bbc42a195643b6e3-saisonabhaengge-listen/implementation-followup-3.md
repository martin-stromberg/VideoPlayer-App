# Implementierung – Nachprüfung Iteration 3

## Ergebnis

Keine Produktänderung und keine dauerhafte Teständerung. Die beiden offenen Such-E2E bleiben wegen des nativen Tastaturfokus der Testumgebung offen. Keine Assertions wurden geändert, keine Tests übersprungen, kein direkter Aufruf des Suchhandlers als Ersatz für Return eingeführt. Kein Commit.

## Untersuchte Ursache

`SourceSearchTests.SubmitViaEnter_FiltersFromFirstPage` und `SubmittedText_RemainsVisibleAfterReturn` setzen den Suchtext bereits über die vorhandene Developer-API in das echte MAUI-Suchfeld. Der unverändert erfolgreiche Test `UnsubmittedInput_SurvivesModelTicks` prüft den sichtbaren Feldinhalt und das Ausbleiben einer unbestätigten Suche.

Die gezielte UIA-Diagnose ergab für die native TextBox der SearchBar:

```text
input=Edit TextBox id=TextBox focus=False value=Komöd
focused=Window Shell_SystemDialog id=Popup Window process=11252
```

Der Prozess `11252` wurde über `Get-Process -Id 11252` als `PickerHost` identifiziert. Damit enthält das echte Suchfeld den korrekten Text, besitzt aber nicht den Tastaturfokus. Die globale Return-Eingabe wird unter diesen Bedingungen nicht an die App zugestellt. Zusätzlich bildet UIA die SearchBar als `Group` mit innerem `Edit` ab. Die Versuche, das Appfenster zu aktivieren und das innere Edit per UIA bzw. echtem Klick zu fokussieren, änderten den fremden Systemdialog-Fokus nicht und behoben die Timeouts nicht.

Der fremde Systemdialog wurde weder geschlossen noch bedient. Alle erfolglosen Änderungen am `UiDriver.PressEnter` und die temporären Diagnoseausgaben wurden vollständig zurückgenommen. Eine endgültige Aussage zur nativen Return-Verarbeitung des Produkts ist erst in einer Windows-Testsitzung mit verfügbarem App-Tastaturfokus möglich.

## Ausgeführte Tests

- Lauf aller drei `SourceSearchTests` nach erster Fokusdiagnose: **1 bestanden, 2 fehlgeschlagen**; Such-Submit blieb leer.
- Einzelne diagnostische Wiederholungen von `SubmitViaEnter_FiltersFromFirstPage` bestätigten den Timeout; die finale native Fokusdiagnose identifizierte den oben genannten fremden `PickerHost`.
- Gemeinsamer Lauf `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --filter "FullyQualifiedName~SourceSearchTests|FullyQualifiedName~SeasonalGenresTests"`: **7 bestanden, 2 fehlgeschlagen, 0 übersprungen, 9 gesamt**, Dauer 2m03s.
- In diesem gemeinsamen Lauf: **alle sechs saisonalen Windows-E2E bestanden**, dazu `UnsubmittedInput_SurvivesModelTicks`; nur die zwei Return-Suchfälle scheiterten erneut.
- Der gemeinsame Lauf gab zusätzlich einen nativen WinRT-Finalizerfehler `0xC0000005` aus; alle neun Testfälle erhielten dennoch ein Ergebnis. Kein saisonaler Test wurde dadurch als fehlgeschlagen oder übersprungen gemeldet.

## Offene Nacharbeit

- Die beiden Return-Such-E2E in einer Windows-Sitzung ohne fremden Systemdialog als Fokusinhaber erneut ausführen; bei Bedarf danach die vollständige Windows-E2E-Suite abschließen.
- Der bereits dokumentierte vollständige Lauf aus Iteration 2 bleibt **70/72 bestanden**, ohne Skips. Iteration 3 liefert keinen vollständig grünen Gesamtnachweis und behauptet keinen solchen.
