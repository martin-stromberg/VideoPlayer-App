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

- [ ] `SourceSearchTests.SubmitViaEnter_FiltersFromFirstPage` erneut in einer freien interaktiven Windows-Session ausführen. Native Such-TextBox enthält den Text, der Tastaturfokus liegt jedoch bei einem fremden `PickerHost`-Systemdialog (PID 11252, Klasse `Shell_SystemDialog`, AutomationId `Popup Window`); globale Return-Taste erreicht die App nicht. Fremden Dialog nicht eigenständig schließen oder bedienen.
- [ ] `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn` nach Beseitigung derselben Fokusblockade erneut ausführen; anschließend vollständige Windows-E2E-Suite erfolgreich abschließen. Letzter vollständiger Lauf: 70/72 bestanden, alle sechs saisonalen Szenarien bestanden.
- [ ] Im letzten gezielten Lauf zusätzlich gemeldeten WinRT-Finalizer-Prozessfehler `0xC0000005` in freier interaktiver Session prüfen; Ergebnis war 7/9 (einschließlich sechs bestandener saisonaler Tests). Keine Produktursache belegt; keine Testabschächung oder spekulative Korrektur vornehmen.

## Hinweise zur Fortsetzung

Produktcode und Testtreiber wurden in Iteration 3 nicht verändert; erfolglose Fokusversuche wurden vollständig zurückgenommen. Bestehende Nachweise: 1396 mobile Unit-Tests bestanden, Windows-Build erfolgreich; Server Debug/Release und vollständige Suite 2358 bestanden/2 bestehende Skips, vier echte Web-E2E bestanden und unabhängige Mutationstests erfolgreich. Details in `test-results.md` und `implementation-followup-3.md`.
