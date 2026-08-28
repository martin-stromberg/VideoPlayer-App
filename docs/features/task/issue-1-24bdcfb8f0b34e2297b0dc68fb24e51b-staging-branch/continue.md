# Offene Aufgaben

Erstellt am: 2026-08-28
Abbruchgrund: Lint-Verstösse im bestehenden Quellcode verhindern eine vollständig grüne Testprüfung.

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Fehlgeschlagene Tests

- [ ] `dotnet format --verify-no-changes` bestehende Whitespace- und Formatierungsverstösse im C#-Quellcode korrigieren, damit der Linter-Schritt in `PR Checks` grün wird.

## Weiterer manueller Schritt

- [ ] Branch-Protection-Regeln für `main` und `staging` in der GitHub-UI anlegen.
