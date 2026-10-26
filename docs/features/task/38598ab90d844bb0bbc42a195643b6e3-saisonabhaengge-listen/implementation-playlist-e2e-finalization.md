# Playlist-Verschieben: E2E-Abschlussprüfung

## Befund

`PlaylistManagementTests.MoveEntries_ReordersOnServer` scheiterte im vollständigen 72er-Lauf beim dritten Verschieben und in der isolierten Wiederholung schon beim zweiten Verschieben. Der zuletzt gelesene App-Zustand zeigte jeweils die erfolgreiche Reihenfolge des vorherigen Schritts und dessen Erfolgsmeldung; der nächste Verschiebeschritt erreichte den erwarteten Zustand nicht. Der erste Schritt und die Serverreihenfolge waren erfolgreich. Ein Server- oder Produktfehler ist damit nicht belegt.

Der Test öffnete nach dem Modell-Reload sofort das nächste `DisplayActionSheetAsync`. Der App-Code zeigt den Dialog asynchron; das Modell kann die neue Eintragsreihenfolge bereits enthalten, bevor das vorherige Aktionsmenü aus dem UIA-Baum verschwunden ist. Die wechselnde Fehlerposition passt zu dieser zeitlichen Überlappung. Diese Erklärung ist eine aus Code und Testverlauf abgeleitete Race-Hypothese, kein instrumentierter Nachweis des internen Popupzustands.

## Enger Testfix

Der UI-Treiber bietet `WaitForNameAbsent`: Er fragt im App-Fenster und unter den UIA-Elementen desselben Prozesses ab, bis der ausgewählte Aktionsmenüeintrag nicht mehr vorhanden ist. Nur `ChooseEntryActionAsync` verwendet diese Abfrage nach dem Klick, bevor der Test zum nächsten Schritt geht. Assertions, Produktcode und allgemeine Zeitlimits blieben unverändert.

## Prüfung

- Vor dem Fix: vollständiger Lauf 70/72, Playlistfall fehlgeschlagen; isolierter Nachlauf ebenfalls fehlgeschlagen, aber bei einem anderen Verschiebeschritt.
- Nach dem Fix: isolierter `MoveEntries_ReordersOnServer`-Lauf **1 bestanden, 0 fehlgeschlagen**, TRX unter `.work/test-results/playlist-move-popup-wait/playlist-move-popup-wait.trx`.
- Zweiter isolierter `MoveEntries_ReordersOnServer`-Lauf: **1 bestanden, 0 fehlgeschlagen**, TRX unter `.work/test-results/playlist-move-popup-wait-repeat/playlist-move-popup-wait-repeat.trx`.
- Nachbarszenarien mit demselben Menühelper: `RemoveEntry_RemovesFromServer` **1/1 bestanden**; `RemoveEntryWithConflict_ConfirmationRetriesWithFlag` und `RemoveEntryWithConflict_CancelKeepsEntry` **2/2 bestanden**. TRX unter `.work/test-results/playlist-remove-neighbors/` und `.work/test-results/playlist-remove-conflict-neighbors/`.

Die vollständige E2E-Suite wurde in diesem Schritt nicht wiederholt. Die sechs saisonalen Fälle waren zuvor im 72er-Lauf grün; ihre Fixtures wurden durch diesen Fix nicht geändert.
