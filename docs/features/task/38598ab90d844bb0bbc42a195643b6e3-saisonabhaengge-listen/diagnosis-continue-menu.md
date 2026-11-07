# Diagnose: sporadischer Timeout im Continue-Watching-Kontextmenü

## Beobachtung

Der vollständige Windows-E2E-Lauf vom 2026-10-26 meldete bei `LongPressMovie_Cancel_KeepsEntry_NoMutationRequests` nach 18,17 Sekunden einen Timeout in `WaitForName(app.Ui, "Ausblenden")` (`ContinueWatchingContextMenuTests.cs:104/150`). Die anschließende gezielte Wiederholung bestand. Die zugehörige App-PID 51092 beendete sich beim Test-Fixture-Shutdown regulär mit Exitcode 0; `stderr.log` und `stdout.log` sind leer. Ein App-Absturz ist für diesen Fehler nicht belegt.

## Codebefund

`WaitForName` ruft ausschließlich `UiDriver.FindByName` auf. Diese Methode sucht nur unterhalb des App-Hauptfensters. Dagegen berücksichtigt `UiDriver.ClickByName` bei ActionSheets zusätzlich prozesszugehörige Popups auf Desktop-Ebene. Die Testbeschreibung und der Kommentar zu `ClickByName` weisen darauf hin, dass `DisplayActionSheet`-Schaltflächen außerhalb des Hauptfensterbaums liegen können. Der Timeout beweist deshalb nicht, dass das Menü nicht erschienen ist: Auch ein vorhandenes Popup außerhalb dieses Teilbaums wäre für `WaitForName` unsichtbar.

`HomePage.OnContinueWatchingItemLongPressed` ruft `DisplayActionSheetAsync` mit „Ausblenden“, „Überspringen“ und „Abbrechen“ auf. Der Dev-API-Long-Press löst das Carousel-Ereignis aus; die HTTP-Antwort wartet nicht auf die Auswahl im asynchronen ActionSheet. Ein kurz verzögertes Erscheinen des Dialogs ist daher möglich. Ob im fehlgeschlagenen Lauf eine Verzögerung oder ein Popup außerhalb des Fensterbaums vorlag, lässt sich ohne UIA-Baumaufnahme nicht entscheiden.

## Eng begrenzte Korrekturidee

Für den Cancel-Test kann die rein vorbereitende `WaitForName("Ausblenden")`-Abfrage entfallen und `ClickByName("Abbrechen", ..., initialWait: 300 ms)` direkt nach dem Long-Press erfolgen. Das nutzt die bereits vorhandene Popup-Suche mit Retry und prüft weiterhin, dass keine Mutation stattfand. Die Verfügbarkeit beider Aktionen wird im Hide-Test separat geprüft. Ein gezielter Test und danach die volle Suite wären nötig. Der Desktop-Fallback von `ClickByName` besitzt selbst keinen harten Timeout für eine einzelne UIA-Abfrage; die vom Nutzer beobachtete Hängegefahr bleibt damit bestehen.

Eine robustere Alternative wäre, den Popup-Fund prozessbezogen und mit einem einzelnen UIA-Aufruf mit eigenem Timeout zu instrumentieren und beim Fehler den relevanten Baum zu protokollieren. Dafür fehlen derzeit Laufzeitbelege und ein kleiner, nachgewiesener API-Pfad. Kein Produktcode wurde geändert.
