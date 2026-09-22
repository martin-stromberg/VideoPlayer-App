# Offene Aufgaben

Erstellt am: 2026-09-22
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine (review.md: `Vollständig umgesetzt`).

## Code-Review-Befunde

Keine (review-code.md Runde 3: `Keine Befunde`).

## Usability-Befunde

- [ ] Fehlgeschlagener (Neu-)Ladevorgang löscht alle vorhandenen Quellen-Einträge — mittel. `UpdateSourcesLoaded` (AppUpdate.cs) übernimmt `message.Sources` unverändert; `LoadSourcesAsync` (ShellAppEffectHandler.cs:428-444) dispatcht bei Ausnahme `sources = []` → transienter Fehler räumt alle Menüeinträge ab. Empfehlung: bei `Error != null` den bisherigen Bestand behalten (`Sources = message.Error is null ? message.Sources : model.SourcesOverview.Sources`), sodass die entworfene Darstellung „Retry-Zeile + weiterhin antippbare Einträge" eintritt.
- [ ] Erneuter Tap auf die bereits geöffnete Quelle schiebt eine identische Seite — gering. `NavigateToSourceOverviewAsync` (ShellAppEffectHandler.cs:409-426) pusht `sourceoverview` ohne Dedup. Empfehlung: Push unterlassen bzw. nur neu laden, wenn die oberste Route bereits `sourceoverview?id={SourceId}` ist.

## Fehlgeschlagene Tests

Keine (test-results.md: `Keine Fehler`, 644/644).
